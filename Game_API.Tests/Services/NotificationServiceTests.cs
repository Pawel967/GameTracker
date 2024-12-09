using AutoMapper;
using Game_API.Data;
using Game_API.Dtos.Notification;
using Game_API.Models.Auth;
using Game_API.Models.Notification;
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
    public class NotificationServiceTests
    {
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<NotificationService>> _mockLogger;
        private readonly DbContextOptions<AppDbContext> _dbContextOptions;

        public NotificationServiceTests()
        {
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<NotificationService>>();
            _dbContextOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
        }

        #region CreateFollowNotificationAsync Tests

        [Fact]
        public async Task CreateFollowNotificationAsync_FollowerNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var service = CreateService(context);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.CreateFollowNotificationAsync(Guid.NewGuid(), Guid.NewGuid()));
        }

        [Fact]
        public async Task CreateFollowNotificationAsync_Success_CreatesNotification()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var followerId = Guid.NewGuid();
            var followedId = Guid.NewGuid();
            var follower = new User
            {
                Id = followerId,
                Username = "follower"
            };
            context.Users.Add(follower);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            // Act
            await service.CreateFollowNotificationAsync(followerId, followedId);

            // Assert
            var notification = await context.Notifications.FirstOrDefaultAsync();
            Assert.NotNull(notification);
            Assert.Equal(followedId, notification.UserId);
            Assert.Equal(followerId, notification.TriggerUserId);
            Assert.Contains("follower started following you", notification.Message);
            Assert.False(notification.IsRead);
        }

        #endregion

        #region CreateNotificationAsync Tests

        [Fact]
        public async Task CreateNotificationAsync_NoUserIds_NotifiesAllUsers()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var users = new List<User>
            {
                new User { Id = Guid.NewGuid() },
                new User { Id = Guid.NewGuid() }
            };
            context.Users.AddRange(users);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            // Act
            await service.CreateNotificationAsync("Test message");

            // Assert
            var notifications = await context.Notifications.ToListAsync();
            Assert.Equal(2, notifications.Count);
            Assert.All(notifications, n =>
            {
                Assert.Equal("Test message", n.Message);
                Assert.False(n.IsRead);
            });
        }

        [Fact]
        public async Task CreateNotificationAsync_SpecificUserIds_NotifiesOnlySpecifiedUsers()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var user1 = new User { Id = Guid.NewGuid() };
            var user2 = new User { Id = Guid.NewGuid() };
            var user3 = new User { Id = Guid.NewGuid() };
            context.Users.AddRange(user1, user2, user3);
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var targetUserIds = new List<Guid> { user1.Id, user2.Id };

            // Act
            await service.CreateNotificationAsync("Test message", targetUserIds);

            // Assert
            var notifications = await context.Notifications.ToListAsync();
            Assert.Equal(2, notifications.Count);
            Assert.Contains(notifications, n => n.UserId == user1.Id);
            Assert.Contains(notifications, n => n.UserId == user2.Id);
            Assert.DoesNotContain(notifications, n => n.UserId == user3.Id);
        }

        #endregion

        #region GetUserNotificationsAsync Tests

        [Fact]
        public async Task GetUserNotificationsAsync_ExcludeRead_ReturnsOnlyUnread()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var userId = Guid.NewGuid();
            var notifications = new List<Notification>
            {
                new() { Id = Guid.NewGuid(), UserId = userId, IsRead = false },
                new() { Id = Guid.NewGuid(), UserId = userId, IsRead = true }
            };
            context.Notifications.AddRange(notifications);
            await context.SaveChangesAsync();

            _mockMapper.Setup(m => m.Map<IEnumerable<NotificationDto>>(It.IsAny<List<Notification>>()))
                .Returns((List<Notification> n) => n.Select(x => new NotificationDto { Id = x.Id }));

            var service = CreateService(context);

            // Act
            var result = await service.GetUserNotificationsAsync(userId);

            // Assert
            Assert.Single(result);
        }

        [Fact]
        public async Task GetUserNotificationsAsync_IncludeRead_ReturnsAll()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var userId = Guid.NewGuid();
            var notifications = new List<Notification>
            {
                new() { Id = Guid.NewGuid(), UserId = userId, IsRead = false },
                new() { Id = Guid.NewGuid(), UserId = userId, IsRead = true }
            };
            context.Notifications.AddRange(notifications);
            await context.SaveChangesAsync();

            _mockMapper.Setup(m => m.Map<IEnumerable<NotificationDto>>(It.IsAny<List<Notification>>()))
                .Returns((List<Notification> n) => n.Select(x => new NotificationDto { Id = x.Id }));

            var service = CreateService(context);

            // Act
            var result = await service.GetUserNotificationsAsync(userId, includeRead: true);

            // Assert
            Assert.Equal(2, result.Count());
        }

        #endregion

        #region MarkAsReadAsync Tests

        [Fact]
        public async Task MarkAsReadAsync_NotificationExists_MarksAsRead()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var userId = Guid.NewGuid();
            var notificationId = Guid.NewGuid();
            var notification = new Notification
            {
                Id = notificationId,
                UserId = userId,
                IsRead = false
            };
            context.Notifications.Add(notification);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            // Act
            await service.MarkAsReadAsync(userId, notificationId);

            // Assert
            var updatedNotification = await context.Notifications.FindAsync(notificationId);
            Assert.NotNull(updatedNotification);
            Assert.True(updatedNotification.IsRead);
        }

        [Fact]
        public async Task MarkAsReadAsync_NotificationDoesNotExist_NoException()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var service = CreateService(context);

            // Act & Assert
            await service.MarkAsReadAsync(Guid.NewGuid(), Guid.NewGuid());
            // Should not throw any exception
        }

        #endregion

        #region MarkAllAsReadAsync Tests

        [Fact]
        public async Task MarkAllAsReadAsync_MarksAllUnreadAsRead()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var userId = Guid.NewGuid();
            var notifications = new List<Notification>
            {
                new() { Id = Guid.NewGuid(), UserId = userId, IsRead = false },
                new() { Id = Guid.NewGuid(), UserId = userId, IsRead = false },
                new() { Id = Guid.NewGuid(), UserId = userId, IsRead = true }
            };
            context.Notifications.AddRange(notifications);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            // Act
            await service.MarkAllAsReadAsync(userId);

            // Assert
            var unreadCount = await context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);
            Assert.Equal(0, unreadCount);
        }

        #endregion

        #region GetUnreadCountAsync Tests

        [Fact]
        public async Task GetUnreadCountAsync_ReturnsCorrectCount()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var userId = Guid.NewGuid();
            var notifications = new List<Notification>
            {
                new() { Id = Guid.NewGuid(), UserId = userId, IsRead = false },
                new() { Id = Guid.NewGuid(), UserId = userId, IsRead = false },
                new() { Id = Guid.NewGuid(), UserId = userId, IsRead = true }
            };
            context.Notifications.AddRange(notifications);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            // Act
            var result = await service.GetUnreadCountAsync(userId);

            // Assert
            Assert.Equal(2, result);
        }

        #endregion

        #region DeleteNotificationAsync Tests

        [Fact]
        public async Task DeleteNotificationAsync_NotificationNotFound_ReturnsFalse()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var service = CreateService(context);

            // Act
            var result = await service.DeleteNotificationAsync(Guid.NewGuid(), Guid.NewGuid());

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task DeleteNotificationAsync_Success_DeletesNotification()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var userId = Guid.NewGuid();
            var notificationId = Guid.NewGuid();
            var notification = new Notification
            {
                Id = notificationId,
                UserId = userId
            };
            context.Notifications.Add(notification);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            // Act
            var result = await service.DeleteNotificationAsync(userId, notificationId);

            // Assert
            Assert.True(result);
            Assert.Null(await context.Notifications.FindAsync(notificationId));
        }

        #endregion

        private NotificationService CreateService(AppDbContext context)
        {
            return new NotificationService(
                context,
                _mockMapper.Object,
                _mockLogger.Object);
        }
    }
}
