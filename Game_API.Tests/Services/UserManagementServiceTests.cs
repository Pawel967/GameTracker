using AutoMapper;
using Game_API.Data;
using Game_API.Dtos.Auth;
using Game_API.Models.Auth;
using Game_API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game_API.Tests.Services
{
    public class UserManagementServiceTests
    {
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<UserManagementService>> _mockLogger;
        private readonly DbContextOptions<AppDbContext> _dbContextOptions;

        public UserManagementServiceTests()
        {
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<UserManagementService>>();
            _dbContextOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
        }

        #region GetUserByIdAsync Tests

        [Fact]
        public async Task GetUserByIdAsync_UserNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var service = CreateService(context);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.GetUserByIdAsync(Guid.NewGuid()));
        }

        [Fact]
        public async Task GetUserByIdAsync_UserExists_ReturnsUserDto()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                Email = "test@example.com"
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var expectedDto = new UserDto
            {
                Id = userId,
                Username = "testuser",
                Email = "test@example.com"
            };
            _mockMapper.Setup(m => m.Map<UserDto>(It.IsAny<User>()))
                .Returns(expectedDto);

            var service = CreateService(context);

            // Act
            var result = await service.GetUserByIdAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedDto.Username, result.Username);
            Assert.Equal(expectedDto.Email, result.Email);
        }

        #endregion

        #region DeleteUserAsync Tests

        [Fact]
        public async Task DeleteUserAsync_UserNotFound_ReturnsFalse()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var service = CreateService(context);

            // Act
            var result = await service.DeleteUserAsync(Guid.NewGuid());

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task DeleteUserAsync_UserExists_DeletesAndReturnsTrue()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var userId = Guid.NewGuid();
            var user = new User { Id = userId };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            // Act
            var result = await service.DeleteUserAsync(userId);

            // Assert
            Assert.True(result);
            Assert.Null(await context.Users.FindAsync(userId));
        }

        #endregion

        #region AdminUpdateUserAsync Tests

        [Fact]
        public async Task AdminUpdateUserAsync_UserNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var service = CreateService(context);
            var updateDto = new UpdateUserDto { Username = "newname" };

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.AdminUpdateUserAsync(Guid.NewGuid(), updateDto));
        }

        [Fact]
        public async Task AdminUpdateUserAsync_DuplicateUsername_ThrowsInvalidOperationException()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var existingUser = new User { Id = Guid.NewGuid(), Username = "existing" };
            var targetUser = new User { Id = Guid.NewGuid(), Username = "target" };
            context.Users.AddRange(existingUser, targetUser);
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var updateDto = new UpdateUserDto { Username = "existing" };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.AdminUpdateUserAsync(targetUser.Id, updateDto));
        }

        [Fact]
        public async Task AdminUpdateUserAsync_InvalidEmail_ThrowsArgumentException()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var userId = Guid.NewGuid();
            var user = new User { Id = userId };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var updateDto = new UpdateUserDto { Email = "invalid-email" };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.AdminUpdateUserAsync(userId, updateDto));
        }

        [Fact]
        public async Task AdminUpdateUserAsync_WeakPassword_ThrowsArgumentException()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var userId = Guid.NewGuid();
            var user = new User { Id = userId };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var updateDto = new UpdateUserDto { Password = "weak" };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.AdminUpdateUserAsync(userId, updateDto));
        }

        [Fact]
        public async Task AdminUpdateUserAsync_ValidUpdate_UpdatesUser()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                Username = "oldname",
                Email = "old@example.com"
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var expectedDto = new UserDto
            {
                Id = userId,
                Username = "newname",
                Email = "new@example.com"
            };

            _mockMapper.Setup(m => m.Map<UserDto>(It.IsAny<User>()))
                .Returns(expectedDto);

            var service = CreateService(context);
            var updateDto = new UpdateUserDto
            {
                Username = "newname",
                Email = "new@example.com",
                Password = "StrongP@ss123"
            };

            // Act
            var result = await service.AdminUpdateUserAsync(userId, updateDto);

            // Assert
            Assert.Equal(expectedDto.Username, result.Username);
            Assert.Equal(expectedDto.Email, result.Email);

            var updatedUser = await context.Users.FindAsync(userId);
            Assert.NotNull(updatedUser);
            Assert.Equal("newname", updatedUser.Username);
            Assert.Equal("new@example.com", updatedUser.Email);
            Assert.NotNull(updatedUser.PasswordHash);
        }

        #endregion

        #region AssignRoleAsync Tests

        [Fact]
        public async Task AssignRoleAsync_UserNotFound_ReturnsFalse()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var service = CreateService(context);

            // Act
            var result = await service.AssignRoleAsync(Guid.NewGuid(), "Admin");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task AssignRoleAsync_RoleNotFound_ReturnsFalse()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var userId = Guid.NewGuid();
            var user = new User { Id = userId };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            // Act
            var result = await service.AssignRoleAsync(userId, "NonexistentRole");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task AssignRoleAsync_Success_AssignsRole()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var userId = Guid.NewGuid();
            var user = new User { Id = userId };
            var role = new Role { Id = Guid.NewGuid(), Name = "Admin" };

            context.Users.Add(user);
            context.Roles.Add(role);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            // Act
            var result = await service.AssignRoleAsync(userId, "Admin");

            // Assert
            Assert.True(result);
            var updatedUser = await context.Users
                .Include(u => u.Roles)
                .FirstAsync(u => u.Id == userId);
            Assert.Single(updatedUser.Roles);
            Assert.Equal("Admin", updatedUser.Roles.First().Name);
        }

        #endregion

        #region GetAllUsersAsync Tests

        [Fact]
        public async Task GetAllUsersAsync_ReturnsAllUsers()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var users = new List<User>
            {
                new() { Id = Guid.NewGuid(), Username = "user1" },
                new() { Id = Guid.NewGuid(), Username = "user2" }
            };
            context.Users.AddRange(users);
            await context.SaveChangesAsync();

            var expectedDtos = users.Select(u => new UserDto { Id = u.Id, Username = u.Username }).ToList();
            _mockMapper.Setup(m => m.Map<IEnumerable<UserDto>>(It.IsAny<List<User>>()))
                .Returns(expectedDtos);

            var service = CreateService(context);

            // Act
            var result = await service.GetAllUsersAsync();

            // Assert
            Assert.Equal(2, result.Count());
            Assert.Contains(result, u => u.Username == "user1");
            Assert.Contains(result, u => u.Username == "user2");
        }

        #endregion

        #region GetAllRolesAsync Tests

        [Fact]
        public async Task GetAllRolesAsync_ReturnsAllRoles()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var roles = new List<Role>
            {
                new() { Id = Guid.NewGuid(), Name = "Admin" },
                new() { Id = Guid.NewGuid(), Name = "User" }
            };
            context.Roles.AddRange(roles);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            // Act
            var result = await service.GetAllRolesAsync();

            // Assert
            Assert.Equal(2, result.Count());
            Assert.Contains("Admin", result);
            Assert.Contains("User", result);
        }

        #endregion

        private UserManagementService CreateService(AppDbContext context)
        {
            return new UserManagementService(
                context,
                _mockMapper.Object,
                _mockLogger.Object);
        }
    }
}
