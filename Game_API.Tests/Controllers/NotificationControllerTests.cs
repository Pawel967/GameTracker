using Game_API.Controllers;
using Game_API.Dtos.Notification;
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
    public class NotificationControllerTests
    {
        private readonly Mock<INotificationService> _mockNotificationService;
        private readonly Mock<ILogger<NotificationController>> _mockLogger;
        private readonly NotificationController _controller;
        private readonly Guid _testUserId;

        public NotificationControllerTests()
        {
            _mockNotificationService = new Mock<INotificationService>();
            _mockLogger = new Mock<ILogger<NotificationController>>();
            _controller = new NotificationController(_mockNotificationService.Object, _mockLogger.Object);
            _testUserId = Guid.NewGuid();
        }

        #region Helper Methods

        private void SetupAuthenticatedUser(Guid userId, string role = "")
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };

            if (!string.IsNullOrEmpty(role))
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        private List<NotificationDto> CreateSampleNotifications()
        {
            return new List<NotificationDto>
            {
                new NotificationDto
                {
                    Id = Guid.NewGuid(),
                    Message = "Test Notification 1",
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                },
                new NotificationDto
                {
                    Id = Guid.NewGuid(),
                    Message = "Test Notification 2",
                    IsRead = true,
                    CreatedAt = DateTime.UtcNow.AddHours(-1)
                }
            };
        }

        #endregion

        #region GetMyNotifications Tests

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task GetMyNotifications_ReturnsOkResult(bool includeRead)
        {
            // Arrange
            SetupAuthenticatedUser(_testUserId);
            var expectedNotifications = CreateSampleNotifications();

            _mockNotificationService.Setup(x => x.GetUserNotificationsAsync(_testUserId, includeRead))
                .ReturnsAsync(expectedNotifications);

            // Act
            var result = await _controller.GetMyNotifications(includeRead);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var notifications = Assert.IsAssignableFrom<IEnumerable<NotificationDto>>(okResult.Value);
            Assert.Equal(2, notifications.Count());
        }

        [Fact]
        public async Task GetMyNotifications_ServiceException_ReturnsInternalServerError()
        {
            // Arrange
            SetupAuthenticatedUser(_testUserId);
            _mockNotificationService.Setup(x => x.GetUserNotificationsAsync(_testUserId, false))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.GetMyNotifications();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Equal("An error occurred while retrieving notifications", statusCodeResult.Value);
        }

        #endregion

        #region GetUnreadCount Tests

        [Fact]
        public async Task GetUnreadCount_ReturnsOkResult()
        {
            // Arrange
            SetupAuthenticatedUser(_testUserId);
            int expectedCount = 5;

            _mockNotificationService.Setup(x => x.GetUnreadCountAsync(_testUserId))
                .ReturnsAsync(expectedCount);

            // Act
            var result = await _controller.GetUnreadCount();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var count = Assert.IsType<int>(okResult.Value);
            Assert.Equal(expectedCount, count);
        }

        #endregion

        #region MarkAsRead Tests

        [Fact]
        public async Task MarkAsRead_ReturnsOkResult()
        {
            // Arrange
            SetupAuthenticatedUser(_testUserId);
            var notificationId = Guid.NewGuid();

            _mockNotificationService.Setup(x => x.MarkAsReadAsync(_testUserId, notificationId))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.MarkAsRead(notificationId);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        #endregion

        #region MarkAllAsRead Tests

        [Fact]
        public async Task MarkAllAsRead_ReturnsOkResult()
        {
            // Arrange
            SetupAuthenticatedUser(_testUserId);

            _mockNotificationService.Setup(x => x.MarkAllAsReadAsync(_testUserId))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.MarkAllAsRead();

            // Assert
            Assert.IsType<OkResult>(result);
        }

        #endregion

        #region SendNotification Tests

        [Fact]
        public async Task SendNotification_AsAdmin_ReturnsOkResult()
        {
            // Arrange
            SetupAuthenticatedUser(_testUserId, "Admin");
            var dto = new CreateNotificationDto
            {
                Message = "Test notification",
                UserIds = new List<Guid> { Guid.NewGuid() }
            };

            _mockNotificationService.Setup(x => x.CreateNotificationAsync(dto.Message, dto.UserIds))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.SendNotification(dto);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task SendNotification_GlobalNotification_ReturnsOkResult()
        {
            // Arrange
            SetupAuthenticatedUser(_testUserId, "Admin");
            var dto = new CreateNotificationDto
            {
                Message = "Global notification",
                UserIds = null
            };

            _mockNotificationService.Setup(x => x.CreateNotificationAsync(dto.Message, null))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.SendNotification(dto);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        #endregion

        #region DeleteNotification Tests

        [Fact]
        public async Task DeleteNotification_ExistingNotification_ReturnsOkResult()
        {
            // Arrange
            SetupAuthenticatedUser(_testUserId);
            var notificationId = Guid.NewGuid();

            _mockNotificationService.Setup(x => x.DeleteNotificationAsync(_testUserId, notificationId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteNotification(notificationId);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task DeleteNotification_NonExistingNotification_ReturnsNotFound()
        {
            // Arrange
            SetupAuthenticatedUser(_testUserId);
            var notificationId = Guid.NewGuid();

            _mockNotificationService.Setup(x => x.DeleteNotificationAsync(_testUserId, notificationId))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteNotification(notificationId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Notification not found", notFoundResult.Value);
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task GetUserId_NoAuthenticationClaim_ReturnsInternalServerError()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act
            var result = await _controller.GetMyNotifications();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Equal("An error occurred while retrieving notifications", statusCodeResult.Value);
        }

        [Fact]
        public async Task GetUserId_InvalidGuid_ReturnsInternalServerError()
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
                HttpContext = new DefaultHttpContext { User = principal }
            };

            // Act
            var result = await _controller.GetMyNotifications();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Equal("An error occurred while retrieving notifications", statusCodeResult.Value);
        }

        [Fact]
        public async Task AnyEndpoint_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            SetupAuthenticatedUser(_testUserId);
            _mockNotificationService.Setup(x => x.GetUserNotificationsAsync(_testUserId, false))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.GetMyNotifications();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Equal("An error occurred while retrieving notifications", statusCodeResult.Value);
        }

        #endregion
    }
}
