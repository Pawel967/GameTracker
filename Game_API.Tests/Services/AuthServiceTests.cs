using AutoMapper;
using Game_API.Configuration;
using Game_API.Data;
using Game_API.Dtos.Auth;
using Game_API.Models.Auth;
using Game_API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game_API.Tests.Services
{
    public class AuthServiceTests
    {
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<AuthService>> _mockLogger;
        private readonly Mock<IOptions<JwtSettings>> _mockJwtSettings;
        private readonly DbContextOptions<AppDbContext> _dbContextOptions;
        private readonly JwtSettings _jwtSettings;

        public AuthServiceTests()
        {
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<AuthService>>();
            _jwtSettings = new JwtSettings
            {
                Secret = "your-256-bit-secret-key-here-for-testing",
                ExpirationInMinutes = 60,
                Issuer = "test-issuer",
                Audience = "test-audience"
            };
            _mockJwtSettings = new Mock<IOptions<JwtSettings>>();
            _mockJwtSettings.Setup(x => x.Value).Returns(_jwtSettings);

            _dbContextOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
        }

        private AppDbContext CreateContext()
        {
            var context = new AppDbContext(_dbContextOptions);
            context.Database.EnsureCreated();
            return context;
        }

        #region Login Tests

        [Fact]
        public async Task LoginAsync_ValidCredentials_ReturnsLoginResponse()
        {
            // Arrange
            using var context = CreateContext();
            var service = new AuthService(context, _mockJwtSettings.Object, _mockMapper.Object, _mockLogger.Object);

            // Create user with properly hashed password
            var loginDto = new LoginDto
            {
                Username = "testuser",
                Password = "TestPass123!"
            };

            var user = new User
            {
                Username = loginDto.Username,
                Email = "test@example.com",
                PasswordHash = Convert.ToBase64String(
                    System.Security.Cryptography.SHA256.Create()
                        .ComputeHash(System.Text.Encoding.UTF8.GetBytes(loginDto.Password))),
                Roles = new List<Role> { new Role { Name = "User" } }
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();

            var userDto = new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Roles = new List<string> { "User" }
            };
            _mockMapper.Setup(m => m.Map<UserDto>(It.IsAny<User>())).Returns(userDto);

            // Act
            var result = await service.LoginAsync(loginDto);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Token);
            Assert.Equal(userDto, result.User);
        }

        [Fact]
        public async Task LoginAsync_InvalidUsername_ThrowsException()
        {
            // Arrange
            using var context = CreateContext();
            var service = new AuthService(context, _mockJwtSettings.Object, _mockMapper.Object, _mockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => service.LoginAsync(new LoginDto
            {
                Username = "nonexistent",
                Password = "TestPass123!"
            }));
        }

        [Fact]
        public async Task LoginAsync_InvalidPassword_ThrowsException()
        {
            // Arrange
            using var context = CreateContext();
            var user = new User
            {
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("hashedTestPass123!")),
                Roles = new List<Role> { new Role { Name = "User" } }
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var service = new AuthService(context, _mockJwtSettings.Object, _mockMapper.Object, _mockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => service.LoginAsync(new LoginDto
            {
                Username = "testuser",
                Password = "WrongPassword123!"
            }));
        }

        #endregion

        #region Register Tests

        [Fact]
        public async Task RegisterAsync_ValidData_ReturnsUserDto()
        {
            // Arrange
            using var context = CreateContext();
            var role = new Role { Name = Role.RoleNames.User };
            context.Roles.Add(role);
            await context.SaveChangesAsync();

            var registerDto = new RegisterDto
            {
                Username = "newuser",
                Email = "new@example.com",
                Password = "NewPass123!"
            };

            var expectedUserDto = new UserDto
            {
                Username = registerDto.Username,
                Email = registerDto.Email,
                Roles = new List<string> { Role.RoleNames.User }
            };

            _mockMapper.Setup(m => m.Map<UserDto>(It.IsAny<User>())).Returns(expectedUserDto);

            var service = new AuthService(context, _mockJwtSettings.Object, _mockMapper.Object, _mockLogger.Object);

            // Act
            var result = await service.RegisterAsync(registerDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedUserDto.Username, result.Username);
            Assert.Equal(expectedUserDto.Email, result.Email);
        }

        [Fact]
        public async Task RegisterAsync_InvalidEmail_ThrowsArgumentException()
        {
            // Arrange
            using var context = CreateContext();
            var service = new AuthService(context, _mockJwtSettings.Object, _mockMapper.Object, _mockLogger.Object);

            var registerDto = new RegisterDto
            {
                Username = "newuser",
                Email = "invalid-email",
                Password = "ValidPass123!"
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => service.RegisterAsync(registerDto));
        }

        [Fact]
        public async Task RegisterAsync_WeakPassword_ThrowsArgumentException()
        {
            // Arrange
            using var context = CreateContext();
            var service = new AuthService(context, _mockJwtSettings.Object, _mockMapper.Object, _mockLogger.Object);

            var registerDto = new RegisterDto
            {
                Username = "newuser",
                Email = "valid@example.com",
                Password = "weak"
            };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => service.RegisterAsync(registerDto));
        }

        [Fact]
        public async Task RegisterAsync_DuplicateUsername_ThrowsException()
        {
            // Arrange
            using var context = CreateContext();
            var existingUser = new User
            {
                Username = "existinguser",
                Email = "existing@example.com",
                PasswordHash = "hash"
            };
            context.Users.Add(existingUser);
            await context.SaveChangesAsync();

            var service = new AuthService(context, _mockJwtSettings.Object, _mockMapper.Object, _mockLogger.Object);

            var registerDto = new RegisterDto
            {
                Username = "existinguser",
                Email = "new@example.com",
                Password = "ValidPass123!"
            };

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => service.RegisterAsync(registerDto));
        }

        #endregion

        #region UpdateUser Tests

        [Fact]
        public async Task UpdateCurrentUserAsync_ValidUpdate_ReturnsUpdatedUser()
        {
            // Arrange
            using var context = CreateContext();
            var user = new User
            {
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "hash"
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var updateDto = new UpdateUserDto
            {
                Username = "newusername",
                Email = "newemail@example.com"
            };

            var expectedUserDto = new UserDto
            {
                Username = updateDto.Username,
                Email = updateDto.Email
            };

            _mockMapper.Setup(m => m.Map<UserDto>(It.IsAny<User>())).Returns(expectedUserDto);

            var service = new AuthService(context, _mockJwtSettings.Object, _mockMapper.Object, _mockLogger.Object);

            // Act
            var result = await service.UpdateCurrentUserAsync(user.Id, updateDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedUserDto.Username, result.Username);
            Assert.Equal(expectedUserDto.Email, result.Email);
        }

        [Fact]
        public async Task UpdateCurrentUserAsync_UserNotFound_ThrowsException()
        {
            // Arrange
            using var context = CreateContext();
            var service = new AuthService(context, _mockJwtSettings.Object, _mockMapper.Object, _mockLogger.Object);

            var updateDto = new UpdateUserDto
            {
                Username = "newusername"
            };

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() =>
                service.UpdateCurrentUserAsync(Guid.NewGuid(), updateDto));
        }

        #endregion

        #region GetCurrentUser Tests

        [Fact]
        public async Task GetCurrentUserAsync_ExistingUser_ReturnsUserDto()
        {
            // Arrange
            using var context = CreateContext();
            var user = new User
            {
                Username = "testuser",
                Email = "test@example.com",
                PasswordHash = "hash"
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var expectedUserDto = new UserDto
            {
                Username = user.Username,
                Email = user.Email
            };

            _mockMapper.Setup(m => m.Map<UserDto>(It.IsAny<User>())).Returns(expectedUserDto);

            var service = new AuthService(context, _mockJwtSettings.Object, _mockMapper.Object, _mockLogger.Object);

            // Act
            var result = await service.GetCurrentUserAsync(user.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedUserDto.Username, result.Username);
            Assert.Equal(expectedUserDto.Email, result.Email);
        }

        [Fact]
        public async Task GetCurrentUserAsync_NonExistingUser_ThrowsException()
        {
            // Arrange
            using var context = CreateContext();
            var service = new AuthService(context, _mockJwtSettings.Object, _mockMapper.Object, _mockLogger.Object);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() =>
                service.GetCurrentUserAsync(Guid.NewGuid()));
        }

        #endregion
    }
}
