using Game_API.Controllers;
using Game_API.Dtos.Profile;
using Game_API.Dtos.UserLibrary;
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
    public class UserProfileControllerTests
    {
        private readonly Mock<IUserProfileService> _mockProfileService;
        private readonly Mock<ILogger<UserProfileController>> _mockLogger;
        private readonly UserProfileController _controller;
        private readonly Guid _testUserId;

        public UserProfileControllerTests()
        {
            _mockProfileService = new Mock<IUserProfileService>();
            _mockLogger = new Mock<ILogger<UserProfileController>>();
            _controller = new UserProfileController(_mockProfileService.Object, _mockLogger.Object);
            _testUserId = Guid.NewGuid();
        }

        #region Helper Methods

        private void SetupAuthenticatedUser(Guid userId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
            _controller.ControllerContext.HttpContext.User = principal;
        }

        private void SetupUnauthenticatedUser()
        {
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        private UserProfileDto CreateSampleProfile(Guid userId)
        {
            return new UserProfileDto
            {
                Id = userId,
                Username = "TestUser",
                IsProfilePrivate = false,
                FollowersCount = 10,
                FollowingCount = 5,
                Games = new List<UserGameLibraryDto>()
            };
        }

        #endregion

        #region GetUserProfile Tests

        [Fact]
        public async Task GetUserProfile_UnauthenticatedUser_ReturnsOkResult()
        {
            // Arrange
            SetupUnauthenticatedUser();
            var targetUserId = Guid.NewGuid();
            var expectedProfile = CreateSampleProfile(targetUserId);

            _mockProfileService.Setup(x => x.GetUserProfileAsync(targetUserId, null))
                .ReturnsAsync(expectedProfile);

            // Act
            var result = await _controller.GetUserProfile(targetUserId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var profile = Assert.IsType<UserProfileDto>(okResult.Value);
            Assert.Equal(targetUserId, profile.Id);
        }

        [Fact]
        public async Task GetUserProfile_AuthenticatedUser_ReturnsOkResult()
        {
            // Arrange
            SetupAuthenticatedUser(_testUserId);
            var targetUserId = Guid.NewGuid();
            var expectedProfile = CreateSampleProfile(targetUserId);

            _mockProfileService.Setup(x => x.GetUserProfileAsync(targetUserId, _testUserId))
                .ReturnsAsync(expectedProfile);

            // Act
            var result = await _controller.GetUserProfile(targetUserId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var profile = Assert.IsType<UserProfileDto>(okResult.Value);
            Assert.Equal(targetUserId, profile.Id);
        }

        [Fact]
        public async Task GetUserProfile_ProfileNotFound_ReturnsNotFound()
        {
            // Arrange
            SetupUnauthenticatedUser();
            var targetUserId = Guid.NewGuid();

            _mockProfileService.Setup(x => x.GetUserProfileAsync(targetUserId, null))
                .ThrowsAsync(new KeyNotFoundException());

            // Act
            var result = await _controller.GetUserProfile(targetUserId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal("Profile not found", notFoundResult.Value);
        }

        #endregion

        #region ToggleProfilePrivacy Tests

        [Fact]
        public async Task ToggleProfilePrivacy_Success_ReturnsOkResult()
        {
            // Arrange
            SetupAuthenticatedUser(_testUserId);
            _mockProfileService.Setup(x => x.ToggleProfilePrivacyAsync(_testUserId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.ToggleProfilePrivacy();

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task ToggleProfilePrivacy_ProfileNotFound_ReturnsNotFound()
        {
            // Arrange
            SetupAuthenticatedUser(_testUserId);
            _mockProfileService.Setup(x => x.ToggleProfilePrivacyAsync(_testUserId))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.ToggleProfilePrivacy();

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Profile not found", notFoundResult.Value);
        }

        #endregion

        #region FollowUser Tests

        [Fact]
        public async Task FollowUser_Success_ReturnsOkResult()
        {
            // Arrange
            SetupAuthenticatedUser(_testUserId);
            var targetUserId = Guid.NewGuid();

            _mockProfileService.Setup(x => x.FollowUserAsync(_testUserId, targetUserId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.FollowUser(targetUserId);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task FollowUser_UnableToFollow_ReturnsBadRequest()
        {
            // Arrange
            SetupAuthenticatedUser(_testUserId);
            var targetUserId = Guid.NewGuid();

            _mockProfileService.Setup(x => x.FollowUserAsync(_testUserId, targetUserId))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.FollowUser(targetUserId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Unable to follow user", badRequestResult.Value);
        }

        #endregion

        #region UnfollowUser Tests

        [Fact]
        public async Task UnfollowUser_Success_ReturnsOkResult()
        {
            // Arrange
            SetupAuthenticatedUser(_testUserId);
            var targetUserId = Guid.NewGuid();

            _mockProfileService.Setup(x => x.UnfollowUserAsync(_testUserId, targetUserId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.UnfollowUser(targetUserId);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task UnfollowUser_NotFollowing_ReturnsNotFound()
        {
            // Arrange
            SetupAuthenticatedUser(_testUserId);
            var targetUserId = Guid.NewGuid();

            _mockProfileService.Setup(x => x.UnfollowUserAsync(_testUserId, targetUserId))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.UnfollowUser(targetUserId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Following relationship not found", notFoundResult.Value);
        }

        #endregion

        #region GetFollowers Tests

        [Fact]
        public async Task GetFollowers_ReturnsOkResult()
        {
            // Arrange
            var targetUserId = Guid.NewGuid();
            var expectedFollowers = new List<FollowedUserDto>
            {
                new FollowedUserDto { Id = Guid.NewGuid(), Username = "Follower1", GamesCount = 5 },
                new FollowedUserDto { Id = Guid.NewGuid(), Username = "Follower2", GamesCount = 3 }
            };

            _mockProfileService.Setup(x => x.GetFollowersAsync(targetUserId))
                .ReturnsAsync(expectedFollowers);

            // Act
            var result = await _controller.GetFollowers(targetUserId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var followers = Assert.IsAssignableFrom<IEnumerable<FollowedUserDto>>(okResult.Value);
            Assert.Equal(2, followers.Count());
        }

        #endregion

        #region GetFollowing Tests

        [Fact]
        public async Task GetFollowing_ReturnsOkResult()
        {
            // Arrange
            var targetUserId = Guid.NewGuid();
            var expectedFollowing = new List<FollowedUserDto>
            {
                new FollowedUserDto { Id = Guid.NewGuid(), Username = "Following1", GamesCount = 5 },
                new FollowedUserDto { Id = Guid.NewGuid(), Username = "Following2", GamesCount = 3 }
            };

            _mockProfileService.Setup(x => x.GetFollowingAsync(targetUserId))
                .ReturnsAsync(expectedFollowing);

            // Act
            var result = await _controller.GetFollowing(targetUserId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var following = Assert.IsAssignableFrom<IEnumerable<FollowedUserDto>>(okResult.Value);
            Assert.Equal(2, following.Count());
        }

        #endregion

        #region SearchProfiles Tests

        [Fact]
        public async Task SearchProfiles_ValidSearchTerm_ReturnsOkResult()
        {
            // Arrange
            SetupAuthenticatedUser(_testUserId);
            var searchTerm = "test";
            var expectedProfiles = new List<ProfileSearchDto>
            {
                new ProfileSearchDto { Id = Guid.NewGuid(), Username = "TestUser1", GamesCount = 5 },
                new ProfileSearchDto { Id = Guid.NewGuid(), Username = "TestUser2", GamesCount = 3 }
            };

            _mockProfileService.Setup(x => x.SearchProfilesAsync(searchTerm, _testUserId, 10))
                .ReturnsAsync(expectedProfiles);

            // Act
            var result = await _controller.SearchProfiles(searchTerm);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var profiles = Assert.IsAssignableFrom<IEnumerable<ProfileSearchDto>>(okResult.Value);
            Assert.Equal(2, profiles.Count());
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData(null)]
        public async Task SearchProfiles_InvalidSearchTerm_ReturnsBadRequest(string searchTerm)
        {
            // Act
            var result = await _controller.SearchProfiles(searchTerm);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Search term is required", badRequestResult.Value);
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task AnyEndpoint_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            SetupAuthenticatedUser(_testUserId);
            _mockProfileService.Setup(x => x.GetUserProfileAsync(It.IsAny<Guid>(), It.IsAny<Guid?>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.GetUserProfile(Guid.NewGuid());

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }

        #endregion
    }
}
