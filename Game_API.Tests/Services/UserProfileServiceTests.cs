using AutoMapper;
using Game_API.Data;
using Game_API.Dtos.Profile;
using Game_API.Models.Auth;
using Game_API.Models.Profile;
using Game_API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game_API.Tests.Services
{
    public class UserProfileServiceTests
    {
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<UserProfileService>> _mockLogger;
        private readonly Mock<INotificationService> _mockNotificationService;
        private readonly DbContextOptions<AppDbContext> _dbContextOptions;

        public UserProfileServiceTests()
        {
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<UserProfileService>>();
            _mockNotificationService = new Mock<INotificationService>();

            // Configure the in-memory database with warning suppression
            _dbContextOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
        }

        #region GetUserProfileAsync Tests

        [Fact]
        public async Task GetUserProfileAsync_UserNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var service = CreateService(context);
            var userId = Guid.NewGuid();

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.GetUserProfileAsync(userId, null));
        }

        [Fact]
        public async Task GetUserProfileAsync_PrivateProfile_NonOwnerRequest_ReturnsLimitedProfile()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                IsProfilePrivate = true
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var service = CreateService(context);
            var requestingUserId = Guid.NewGuid();

            // Act
            var result = await service.GetUserProfileAsync(user.Id, requestingUserId);

            // Assert
            Assert.True(result.IsProfilePrivate);
            Assert.Empty(result.Games);
            Assert.Equal(user.Username, result.Username);
            Assert.Equal(user.Id, result.Id);
            Assert.Empty(result.GameStatusCounts);
        }

        [Fact]
        public async Task GetUserProfileAsync_PrivateProfile_OwnerRequest_ReturnsFullProfile()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var userId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                IsProfilePrivate = true
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var expectedProfile = new UserProfileDto
            {
                Id = userId,
                Username = user.Username,
                IsProfilePrivate = true
            };
            _mockMapper.Setup(m => m.Map<UserProfileDto>(It.IsAny<User>()))
                .Returns(expectedProfile);

            var service = CreateService(context);

            // Act
            var result = await service.GetUserProfileAsync(userId, userId);

            // Assert
            Assert.Equal(expectedProfile.Username, result.Username);
            Assert.True(result.IsProfilePrivate);
        }

        [Fact]
        public async Task GetUserProfileAsync_PublicProfile_ReturnsFullProfile()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var user = new User
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                IsProfilePrivate = false
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var expectedProfile = new UserProfileDto
            {
                Id = user.Id,
                Username = user.Username,
                IsProfilePrivate = false
            };
            _mockMapper.Setup(m => m.Map<UserProfileDto>(It.IsAny<User>()))
                .Returns(expectedProfile);

            var service = CreateService(context);

            // Act
            var result = await service.GetUserProfileAsync(user.Id, null);

            // Assert
            Assert.Equal(expectedProfile.Username, result.Username);
            Assert.False(result.IsProfilePrivate);
        }

        [Fact]
        public async Task GetUserProfileAsync_ChecksFollowStatus_WhenRequestingUserProvided()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var userId = Guid.NewGuid();
            var requestingUserId = Guid.NewGuid();
            var user = new User
            {
                Id = userId,
                Username = "testuser",
                IsProfilePrivate = false
            };
            context.Users.Add(user);
            context.UserFollowings.Add(new UserFollowing
            {
                FollowerId = requestingUserId,
                FollowedId = userId
            });
            await context.SaveChangesAsync();

            var expectedProfile = new UserProfileDto
            {
                Id = userId,
                Username = user.Username,
                IsProfilePrivate = false
            };
            _mockMapper.Setup(m => m.Map<UserProfileDto>(It.IsAny<User>()))
                .Returns(expectedProfile);

            var service = CreateService(context);

            // Act
            var result = await service.GetUserProfileAsync(userId, requestingUserId);

            // Assert
            Assert.True(result.IsFollowedByCurrentUser);
        }

        #endregion

        #region ToggleProfilePrivacyAsync Tests

        [Fact]
        public async Task ToggleProfilePrivacyAsync_UserNotFound_ReturnsFalse()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var service = CreateService(context);
            var userId = Guid.NewGuid();

            // Act
            var result = await service.ToggleProfilePrivacyAsync(userId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ToggleProfilePrivacyAsync_TogglesPrivacy_WhenPublic()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var user = new User
            {
                Id = Guid.NewGuid(),
                IsProfilePrivate = false
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            // Act
            var result = await service.ToggleProfilePrivacyAsync(user.Id);

            // Assert
            Assert.True(result);
            Assert.True(user.IsProfilePrivate);
        }

        [Fact]
        public async Task ToggleProfilePrivacyAsync_TogglesPrivacy_WhenPrivate()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var user = new User
            {
                Id = Guid.NewGuid(),
                IsProfilePrivate = true
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            // Act
            var result = await service.ToggleProfilePrivacyAsync(user.Id);

            // Assert
            Assert.True(result);
            Assert.False(user.IsProfilePrivate);
        }

        [Fact]
        public async Task ToggleProfilePrivacyAsync_RemovesFollowers_WhenMadePrivate()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var userId = Guid.NewGuid();
            var follower1Id = Guid.NewGuid();
            var follower2Id = Guid.NewGuid();

            var user = new User
            {
                Id = userId,
                IsProfilePrivate = false
            };
            context.Users.Add(user);

            var followers = new List<UserFollowing>
            {
                new UserFollowing { FollowerId = follower1Id, FollowedId = userId },
                new UserFollowing { FollowerId = follower2Id, FollowedId = userId }
            };
            context.UserFollowings.AddRange(followers);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            // Act
            var result = await service.ToggleProfilePrivacyAsync(userId);

            // Assert
            Assert.True(result);
            Assert.True(user.IsProfilePrivate);
            Assert.Empty(await context.UserFollowings.Where(f => f.FollowedId == userId).ToListAsync());
        }

        #endregion

        #region Follow/Unfollow Tests

        [Fact]
        public async Task FollowUserAsync_SelfFollow_ReturnsFalse()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var service = CreateService(context);
            var userId = Guid.NewGuid();

            // Act
            var result = await service.FollowUserAsync(userId, userId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task FollowUserAsync_PrivateProfile_ReturnsFalse()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var followedUser = new User
            {
                Id = Guid.NewGuid(),
                IsProfilePrivate = true
            };
            context.Users.Add(followedUser);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            // Act
            var result = await service.FollowUserAsync(Guid.NewGuid(), followedUser.Id);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task FollowUserAsync_AlreadyFollowing_ReturnsFalse()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var followerId = Guid.NewGuid();
            var followedId = Guid.NewGuid();

            var followedUser = new User
            {
                Id = followedId,
                IsProfilePrivate = false
            };
            context.Users.Add(followedUser);

            var existingFollow = new UserFollowing
            {
                FollowerId = followerId,
                FollowedId = followedId
            };
            context.UserFollowings.Add(existingFollow);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            // Act
            var result = await service.FollowUserAsync(followerId, followedId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task FollowUserAsync_Success_CreatesFollowRelationshipAndNotification()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var followerId = Guid.NewGuid();
            var followedId = Guid.NewGuid();

            var followedUser = new User
            {
                Id = followedId,
                IsProfilePrivate = false
            };
            context.Users.Add(followedUser);
            await context.SaveChangesAsync();

            _mockNotificationService
                .Setup(x => x.CreateFollowNotificationAsync(followerId, followedId))
                .Returns(Task.CompletedTask);

            var service = CreateService(context);

            // Act
            var result = await service.FollowUserAsync(followerId, followedId);

            // Assert
            Assert.True(result);

            using (var verificationContext = new AppDbContext(_dbContextOptions))
            {
                var following = await verificationContext.UserFollowings
                    .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowedId == followedId);
                Assert.NotNull(following);
                Assert.Equal(followerId, following.FollowerId);
                Assert.Equal(followedId, following.FollowedId);
            }

            _mockNotificationService.Verify(
                x => x.CreateFollowNotificationAsync(followerId, followedId),
                Times.Once);
        }

        [Fact]
        public async Task FollowUserAsync_NotificationFails_DoesNotCreateFollowRelationship()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var followerId = Guid.NewGuid();
            var followedId = Guid.NewGuid();

            var followedUser = new User
            {
                Id = followedId,
                IsProfilePrivate = false
            };
            context.Users.Add(followedUser);
            await context.SaveChangesAsync();

            _mockNotificationService
                .Setup(x => x.CreateFollowNotificationAsync(followerId, followedId))
                .ThrowsAsync(new Exception("Notification failed"));

            var service = CreateService(context);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() =>
                service.FollowUserAsync(followerId, followedId));

            using (var verificationContext = new AppDbContext(_dbContextOptions))
            {
                var following = await verificationContext.UserFollowings
                    .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowedId == followedId);
                Assert.Null(following);
            }
        }

        [Fact]
        public async Task UnfollowUserAsync_NotFollowing_ReturnsFalse()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var service = CreateService(context);

            // Act
            var result = await service.UnfollowUserAsync(Guid.NewGuid(), Guid.NewGuid());

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task UnfollowUserAsync_Success_RemovesFollowRelationship()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var followerId = Guid.NewGuid();
            var followedId = Guid.NewGuid();

            var following = new UserFollowing
            {
                FollowerId = followerId,
                FollowedId = followedId
            };
            context.UserFollowings.Add(following);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            // Act
            var result = await service.UnfollowUserAsync(followerId, followedId);

            // Assert
            Assert.True(result);
            Assert.Empty(await context.UserFollowings.ToListAsync());
        }

        #endregion

        #region Search Tests

        [Fact]
        public async Task SearchProfilesAsync_ReturnsMatchingProfiles()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var users = new List<User>
            {
                new User { Id = Guid.NewGuid(), Username = "test1", IsProfilePrivate = false },
                new User { Id = Guid.NewGuid(), Username = "test2", IsProfilePrivate = true },
                new User { Id = Guid.NewGuid(), Username = "other", IsProfilePrivate = false }
            };
            context.Users.AddRange(users);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            // Act
            var results = await service.SearchProfilesAsync("test", null);

            // Assert
            Assert.Single(results);
            Assert.Equal("test1", results.First().Username);
        }
        [Fact]
        public async Task SearchProfilesAsync_EmptySearchTerm_ReturnsBadRequest()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var users = new List<User>
            {
                new User { Id = Guid.NewGuid(), Username = "test1", IsProfilePrivate = false },
                new User { Id = Guid.NewGuid(), Username = "test2", IsProfilePrivate = false }
            };
            context.Users.AddRange(users);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            // Act & Assert
            var results = await service.SearchProfilesAsync(" ", null);
            Assert.Empty(results);
        }

        [Fact]
        public async Task SearchProfilesAsync_RespectsLimit()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var users = new List<User>
            {
                new User { Id = Guid.NewGuid(), Username = "test1", IsProfilePrivate = false },
                new User { Id = Guid.NewGuid(), Username = "test2", IsProfilePrivate = false },
                new User { Id = Guid.NewGuid(), Username = "test3", IsProfilePrivate = false }
            };
            context.Users.AddRange(users);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            // Act
            var results = await service.SearchProfilesAsync("test", null, 2);

            // Assert
            Assert.Equal(2, results.Count());
        }

        [Fact]
        public async Task SearchProfilesAsync_IncludesFollowStatus_WhenRequested()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var requestingUserId = Guid.NewGuid();
            var targetUserId = Guid.NewGuid();

            var targetUser = new User
            {
                Id = targetUserId,
                Username = "test1",
                IsProfilePrivate = false
            };
            context.Users.Add(targetUser);

            var following = new UserFollowing
            {
                FollowerId = requestingUserId,
                FollowedId = targetUserId
            };
            context.UserFollowings.Add(following);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            // Act
            var results = await service.SearchProfilesAsync("test", requestingUserId);

            // Assert
            var result = results.First();
            Assert.True(result.IsFollowedByCurrentUser);
        }

        [Fact]
        public async Task SearchProfilesAsync_ExcludesPrivateProfiles()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var users = new List<User>
            {
                new User { Id = Guid.NewGuid(), Username = "searchtest1", IsProfilePrivate = true },
                new User { Id = Guid.NewGuid(), Username = "searchtest2", IsProfilePrivate = false },
                new User { Id = Guid.NewGuid(), Username = "searchtest3", IsProfilePrivate = true }
            };
            context.Users.AddRange(users);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            // Act
            var results = await service.SearchProfilesAsync("searchtest", null);

            // Assert
            Assert.Single(results);
            Assert.Equal("searchtest2", results.First().Username);
        }

        #endregion

        #region Get Followers/Following Tests

        [Fact]
        public async Task GetFollowersAsync_ReturnsFollowers()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var userId = Guid.NewGuid();
            var follower1 = new User { Id = Guid.NewGuid(), Username = "follower1", IsProfilePrivate = false };
            var follower2 = new User { Id = Guid.NewGuid(), Username = "follower2", IsProfilePrivate = false };

            context.Users.AddRange(new[] { follower1, follower2 });
            context.UserFollowings.AddRange(new[]
            {
                new UserFollowing { FollowerId = follower1.Id, FollowedId = userId },
                new UserFollowing { FollowerId = follower2.Id, FollowedId = userId }
            });
            await context.SaveChangesAsync();

            _mockMapper.Setup(m => m.Map<FollowedUserDto>(It.IsAny<User>()))
                .Returns<User>(u => new FollowedUserDto { Id = u.Id, Username = u.Username });

            var service = CreateService(context);

            // Act
            var followers = await service.GetFollowersAsync(userId);

            // Assert
            Assert.Equal(2, followers.Count());
            Assert.Contains(followers, f => f.Username == "follower1");
            Assert.Contains(followers, f => f.Username == "follower2");
        }

        [Fact]
        public async Task GetFollowersAsync_ExcludesPrivateProfiles()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var userId = Guid.NewGuid();
            var follower1 = new User { Id = Guid.NewGuid(), Username = "follower1", IsProfilePrivate = false };
            var follower2 = new User { Id = Guid.NewGuid(), Username = "follower2", IsProfilePrivate = true };

            context.Users.AddRange(new[] { follower1, follower2 });
            context.UserFollowings.AddRange(new[]
            {
                new UserFollowing { FollowerId = follower1.Id, FollowedId = userId },
                new UserFollowing { FollowerId = follower2.Id, FollowedId = userId }
            });
            await context.SaveChangesAsync();

            _mockMapper.Setup(m => m.Map<FollowedUserDto>(It.IsAny<User>()))
                .Returns<User>(u => new FollowedUserDto { Id = u.Id, Username = u.Username });

            var service = CreateService(context);

            // Act
            var followers = await service.GetFollowersAsync(userId);

            // Assert
            Assert.Single(followers);
            Assert.Equal("follower1", followers.First().Username);
        }

        [Fact]
        public async Task GetFollowingAsync_ReturnsFollowing()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var userId = Guid.NewGuid();
            var followed1 = new User { Id = Guid.NewGuid(), Username = "followed1", IsProfilePrivate = false };
            var followed2 = new User { Id = Guid.NewGuid(), Username = "followed2", IsProfilePrivate = false };

            context.Users.AddRange(new[] { followed1, followed2 });
            context.UserFollowings.AddRange(new[]
            {
                new UserFollowing { FollowerId = userId, FollowedId = followed1.Id },
                new UserFollowing { FollowerId = userId, FollowedId = followed2.Id }
            });
            await context.SaveChangesAsync();

            _mockMapper.Setup(m => m.Map<FollowedUserDto>(It.IsAny<User>()))
                .Returns<User>(u => new FollowedUserDto { Id = u.Id, Username = u.Username });

            var service = CreateService(context);

            // Act
            var following = await service.GetFollowingAsync(userId);

            // Assert
            Assert.Equal(2, following.Count());
            Assert.Contains(following, f => f.Username == "followed1");
            Assert.Contains(following, f => f.Username == "followed2");
        }

        [Fact]
        public async Task GetFollowingAsync_ExcludesPrivateProfiles()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var userId = Guid.NewGuid();
            var followed1 = new User { Id = Guid.NewGuid(), Username = "followed1", IsProfilePrivate = false };
            var followed2 = new User { Id = Guid.NewGuid(), Username = "followed2", IsProfilePrivate = true };

            context.Users.AddRange(new[] { followed1, followed2 });
            context.UserFollowings.AddRange(new[]
            {
                new UserFollowing { FollowerId = userId, FollowedId = followed1.Id },
                new UserFollowing { FollowerId = userId, FollowedId = followed2.Id }
            });
            await context.SaveChangesAsync();

            _mockMapper.Setup(m => m.Map<FollowedUserDto>(It.IsAny<User>()))
                .Returns<User>(u => new FollowedUserDto { Id = u.Id, Username = u.Username });

            var service = CreateService(context);

            // Act
            var following = await service.GetFollowingAsync(userId);

            // Assert
            Assert.Single(following);
            Assert.Equal("followed1", following.First().Username);
        }

        #endregion

        private UserProfileService CreateService(AppDbContext context)
        {
            return new UserProfileService(
                context,
                _mockMapper.Object,
                _mockLogger.Object,
                _mockNotificationService.Object);
        }
    }
}
