using Game_API.Controllers;
using Game_API.Dtos.Auth;
using Game_API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game_API.Tests.Controllers
{
    public class AdminControllerTests
    {
        private readonly Mock<IUserManagementService> _mockUserManagementService;
        private readonly Mock<ILogger<AdminController>> _mockLogger;
        private readonly AdminController _controller;

        public AdminControllerTests()
        {
            _mockUserManagementService = new Mock<IUserManagementService>();
            _mockLogger = new Mock<ILogger<AdminController>>();
            _controller = new AdminController(_mockUserManagementService.Object, _mockLogger.Object);
        }

        #region GetAllUsers Tests

        [Fact]
        public async Task GetAllUsers_ReturnsOkResult()
        {
            // Arrange
            var users = new List<UserDto>
            {
                new UserDto { Id = Guid.NewGuid(), Username = "user1", Email = "user1@example.com", Roles = new List<string> { "User" } },
                new UserDto { Id = Guid.NewGuid(), Username = "user2", Email = "user2@example.com", Roles = new List<string> { "Admin" } }
            };

            _mockUserManagementService.Setup(x => x.GetAllUsersAsync())
                .ReturnsAsync(users);

            // Act
            var result = await _controller.GetAllUsers();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedUsers = Assert.IsAssignableFrom<IEnumerable<UserDto>>(okResult.Value);
            Assert.Equal(2, returnedUsers.Count());
        }

        [Fact]
        public async Task GetAllUsers_ServiceError_ReturnsInternalServerError()
        {
            // Arrange
            _mockUserManagementService.Setup(x => x.GetAllUsersAsync())
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetAllUsers();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Equal("An error occurred while retrieving users", statusCodeResult.Value);
        }

        #endregion

        #region GetUser Tests

        [Fact]
        public async Task GetUser_ExistingUser_ReturnsOkResult()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new UserDto
            {
                Id = userId,
                Username = "testuser",
                Email = "test@example.com",
                Roles = new List<string> { "User" }
            };

            _mockUserManagementService.Setup(x => x.GetUserByIdAsync(userId))
                .ReturnsAsync(user);

            // Act
            var result = await _controller.GetUser(userId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedUser = Assert.IsType<UserDto>(okResult.Value);
            Assert.Equal(userId, returnedUser.Id);
        }

        [Fact]
        public async Task GetUser_ServiceError_ReturnsInternalServerError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _mockUserManagementService.Setup(x => x.GetUserByIdAsync(userId))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetUser(userId);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Equal("An error occurred while retrieving the user", statusCodeResult.Value);
        }

        #endregion

        #region DeleteUser Tests

        [Fact]
        public async Task DeleteUser_ExistingUser_ReturnsOkResult()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _mockUserManagementService.Setup(x => x.DeleteUserAsync(userId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteUser(userId);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task DeleteUser_NonExistingUser_ReturnsNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _mockUserManagementService.Setup(x => x.DeleteUserAsync(userId))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteUser(userId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("User not found", notFoundResult.Value);
        }

        [Fact]
        public async Task DeleteUser_ServiceError_ReturnsInternalServerError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _mockUserManagementService.Setup(x => x.DeleteUserAsync(userId))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.DeleteUser(userId);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Equal("An error occurred while deleting the user", statusCodeResult.Value);
        }

        #endregion

        #region AssignRole Tests

        [Fact]
        public async Task AssignRole_ValidInput_ReturnsOkResult()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var roleName = "Admin";
            _mockUserManagementService.Setup(x => x.AssignRoleAsync(userId, roleName))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.AssignRole(userId, roleName);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task AssignRole_InvalidUserOrRole_ReturnsNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var roleName = "InvalidRole";
            _mockUserManagementService.Setup(x => x.AssignRoleAsync(userId, roleName))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.AssignRole(userId, roleName);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("User or role not found", notFoundResult.Value);
        }

        [Fact]
        public async Task AssignRole_ServiceError_ReturnsInternalServerError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var roleName = "Admin";
            _mockUserManagementService.Setup(x => x.AssignRoleAsync(userId, roleName))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.AssignRole(userId, roleName);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Equal("An error occurred while assigning the role", statusCodeResult.Value);
        }

        #endregion

        #region GetAllRoles Tests

        [Fact]
        public async Task GetAllRoles_ReturnsOkResult()
        {
            // Arrange
            var roles = new List<string> { "Admin", "User", "Moderator" };
            _mockUserManagementService.Setup(x => x.GetAllRolesAsync())
                .ReturnsAsync(roles);

            // Act
            var result = await _controller.GetAllRoles();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedRoles = Assert.IsAssignableFrom<IEnumerable<string>>(okResult.Value);
            Assert.Equal(3, returnedRoles.Count());
            Assert.Contains("Admin", returnedRoles);
            Assert.Contains("User", returnedRoles);
            Assert.Contains("Moderator", returnedRoles);
        }

        [Fact]
        public async Task GetAllRoles_ServiceError_ReturnsInternalServerError()
        {
            // Arrange
            _mockUserManagementService.Setup(x => x.GetAllRolesAsync())
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.GetAllRoles();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Equal("An error occurred while retrieving roles", statusCodeResult.Value);
        }

        #endregion
    }
}
