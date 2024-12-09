using FluentAssertions;
using Game_API.Controllers;
using Game_API.Dtos.Auth;
using Game_API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Game_API.Tests.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<IAuthService> _mockAuthService;
        private readonly Mock<ILogger<AuthController>> _mockLogger;
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            _mockAuthService = new Mock<IAuthService>();
            _mockLogger = new Mock<ILogger<AuthController>>();
            _controller = new AuthController(_mockAuthService.Object, _mockLogger.Object);
        }

        #region Register Tests

        [Fact]
        public async Task Register_ValidInput_ReturnsOkResult()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Username = "testuser",
                Email = "test@example.com",
                Password = "Test123!"
            };

            var expectedUser = new UserDto
            {
                Id = Guid.NewGuid(),
                Username = registerDto.Username,
                Email = registerDto.Email,
                Roles = new List<string> { "User" }
            };

            _mockAuthService.Setup(x => x.RegisterAsync(registerDto))
                .ReturnsAsync(expectedUser);

            // Act
            var result = await _controller.Register(registerDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedUser = Assert.IsType<UserDto>(okResult.Value);
            Assert.Equal(expectedUser.Username, returnedUser.Username);
            Assert.Equal(expectedUser.Email, returnedUser.Email);
        }

        [Fact]
        public async Task Register_InvalidInput_ReturnsBadRequest()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Username = "test",
                Email = "invalid-email",
                Password = "weak"
            };

            _mockAuthService.Setup(x => x.RegisterAsync(registerDto))
                .ThrowsAsync(new ArgumentException("Invalid input"));

            // Act
            var result = await _controller.Register(registerDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Invalid input", badRequestResult.Value);
        }

        [Fact]
        public async Task Register_ServiceError_ReturnsInternalServerError()
        {
            // Arrange
            var registerDto = new RegisterDto
            {
                Username = "testuser",
                Email = "test@example.com",
                Password = "Test123!"
            };

            _mockAuthService.Setup(x => x.RegisterAsync(registerDto))
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _controller.Register(registerDto);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Equal("An error occurred while processing your request.", statusCodeResult.Value);
        }

        #endregion

        #region Login Tests

        [Fact]
        public async Task Login_ValidCredentials_ReturnsOkWithToken()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Username = "testuser",
                Password = "Test123!"
            };

            var expectedResponse = new LoginResponseDto
            {
                Token = "test-token",
                User = new UserDto
                {
                    Id = Guid.NewGuid(),
                    Username = loginDto.Username,
                    Email = "test@example.com",
                    Roles = new List<string> { "User" }
                }
            };

            _mockAuthService.Setup(x => x.LoginAsync(loginDto))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<LoginResponseDto>(okResult.Value);
            Assert.Equal(expectedResponse.Token, response.Token);
            Assert.Equal(expectedResponse.User.Username, response.User.Username);
        }

        [Fact]
        public async Task Login_InvalidCredentials_ReturnsBadRequest()
        {
            // Arrange
            var loginDto = new LoginDto
            {
                Username = "testuser",
                Password = "wrongpassword"
            };

            _mockAuthService.Setup(x => x.LoginAsync(loginDto))
                .ThrowsAsync(new Exception("Invalid credentials"));

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Invalid credentials", badRequestResult.Value);
        }

        #endregion

        #region GetCurrentUser Tests

        [Fact]
        public async Task GetCurrentUser_AuthenticatedUser_ReturnsOkResult()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupControllerUser(userId);

            var expectedUser = new UserDto
            {
                Id = userId,
                Username = "testuser",
                Email = "test@example.com",
                Roles = new List<string> { "User" }
            };

            _mockAuthService.Setup(x => x.GetCurrentUserAsync(userId))
                .ReturnsAsync(expectedUser);

            // Act
            var result = await _controller.GetCurrentUser();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedUser = Assert.IsType<UserDto>(okResult.Value);
            Assert.Equal(expectedUser.Id, returnedUser.Id);
            Assert.Equal(expectedUser.Username, returnedUser.Username);
        }

        [Fact]
        public async Task GetCurrentUser_NoUserId_ReturnsUnauthorized()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act
            var result = await _controller.GetCurrentUser();

            // Assert
            Assert.IsType<UnauthorizedResult>(result.Result);
        }

        [Fact]
        public async Task GetCurrentUser_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupControllerUser(userId);
            _mockAuthService.Setup(x => x.GetCurrentUserAsync(userId))
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _controller.GetCurrentUser();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Equal("An error occurred while retrieving user data.", statusCodeResult.Value);
        }

        [Fact]
        public async Task GetCurrentUser_InvalidUserIdFormat_ReturnsInternalServerError()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "invalid-guid")
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = principal
                }
            };

            // Act
            var result = await _controller.GetCurrentUser();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Equal("An error occurred while retrieving user data.", statusCodeResult.Value);
        }

        #endregion

        #region UpdateCurrentUser Tests

        [Fact]
        public async Task UpdateCurrentUser_ValidUpdate_ReturnsOkResult()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupControllerUser(userId);

            var updateDto = new UpdateUserDto
            {
                Username = "newusername",
                Email = "newemail@example.com"
            };

            var expectedUser = new UserDto
            {
                Id = userId,
                Username = updateDto.Username,
                Email = updateDto.Email,
                Roles = new List<string> { "User" }
            };

            _mockAuthService.Setup(x => x.UpdateCurrentUserAsync(userId, updateDto))
                .ReturnsAsync(expectedUser);

            // Act
            var result = await _controller.UpdateCurrentUser(updateDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedUser = Assert.IsType<UserDto>(okResult.Value);
            Assert.Equal(expectedUser.Username, returnedUser.Username);
            Assert.Equal(expectedUser.Email, returnedUser.Email);
        }

        [Fact]
        public async Task UpdateCurrentUser_InvalidUpdate_ReturnsBadRequest()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupControllerUser(userId);

            var updateDto = new UpdateUserDto
            {
                Email = "invalid-email"
            };

            _mockAuthService.Setup(x => x.UpdateCurrentUserAsync(userId, updateDto))
                .ThrowsAsync(new ArgumentException("Invalid email format"));

            // Act
            var result = await _controller.UpdateCurrentUser(updateDto);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Invalid email format", badRequestResult.Value);
        }

        [Fact]
        public async Task UpdateCurrentUser_ServiceError_ReturnsInternalServerError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            SetupControllerUser(userId);

            var updateDto = new UpdateUserDto
            {
                Username = "newusername"
            };

            _mockAuthService.Setup(x => x.UpdateCurrentUserAsync(userId, updateDto))
                .ThrowsAsync(new Exception("Service error"));

            // Act
            var result = await _controller.UpdateCurrentUser(updateDto);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Equal("An error occurred while processing your request.", statusCodeResult.Value);
        }

        #endregion

        #region Helper Methods

        private void SetupControllerUser(Guid userId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = principal
                }
            };
        }

        #endregion
    }
}
