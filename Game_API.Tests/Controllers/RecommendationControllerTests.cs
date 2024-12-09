using Game_API.Controllers;
using Game_API.Dtos.Recommendation;
using Game_API.Models.IGDB;
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
    public class RecommendationControllerTests
    {
        private readonly Mock<IGameRecommendationService> _mockRecommendationService;
        private readonly Mock<ILogger<RecommendationController>> _mockLogger;
        private readonly RecommendationController _controller;
        private readonly Guid _testUserId;

        public RecommendationControllerTests()
        {
            _mockRecommendationService = new Mock<IGameRecommendationService>();
            _mockLogger = new Mock<ILogger<RecommendationController>>();
            _controller = new RecommendationController(_mockRecommendationService.Object, _mockLogger.Object);
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
        }

        private List<GameRecommendationDto> CreateSampleRecommendations()
        {
            return new List<GameRecommendationDto>
            {
                new GameRecommendationDto
                {
                    Game = new IGDBGame
                    {
                        Id = 1,
                        Name = "Test Game 1",
                        Rating = 85,
                        RatingCount = 1000
                    },
                    RecommendationReason = "Based on your gaming history",
                    Score = 0.85
                },
                new GameRecommendationDto
                {
                    Game = new IGDBGame
                    {
                        Id = 2,
                        Name = "Test Game 2",
                        Rating = 90,
                        RatingCount = 2000
                    },
                    RecommendationReason = "Popular in your favorite genre",
                    Score = 0.90
                }
            };
        }

        #endregion

        #region GetPersonalizedRecommendations Tests

        [Fact]
        public async Task GetPersonalizedRecommendations_AuthenticatedUser_ReturnsOkResult()
        {
            // Arrange
            SetupAuthenticatedUser(_testUserId);
            var expectedRecommendations = CreateSampleRecommendations();
            _mockRecommendationService.Setup(x => x.GetPersonalizedRecommendationsAsync(_testUserId, 10))
                .ReturnsAsync(expectedRecommendations);

            // Act
            var result = await _controller.GetPersonalizedRecommendations();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var recommendations = Assert.IsAssignableFrom<IEnumerable<GameRecommendationDto>>(okResult.Value);
            Assert.Equal(2, recommendations.Count());
        }

        [Fact]
        public async Task GetPersonalizedRecommendations_CustomCount_ReturnsCorrectNumberOfRecommendations()
        {
            // Arrange
            SetupAuthenticatedUser(_testUserId);
            var customCount = 5;
            var expectedRecommendations = CreateSampleRecommendations().Take(1).ToList();

            _mockRecommendationService.Setup(x => x.GetPersonalizedRecommendationsAsync(_testUserId, customCount))
                .ReturnsAsync(expectedRecommendations);

            // Act
            var result = await _controller.GetPersonalizedRecommendations(customCount);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var recommendations = Assert.IsAssignableFrom<IEnumerable<GameRecommendationDto>>(okResult.Value);
            Assert.Single(recommendations);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task GetPersonalizedRecommendations_InvalidCount_StillReturnsRecommendations(int count)
        {
            // Arrange
            SetupAuthenticatedUser(_testUserId);
            var expectedRecommendations = CreateSampleRecommendations();

            _mockRecommendationService.Setup(x => x.GetPersonalizedRecommendationsAsync(_testUserId, count))
                .ReturnsAsync(expectedRecommendations);

            // Act
            var result = await _controller.GetPersonalizedRecommendations(count);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var recommendations = Assert.IsAssignableFrom<IEnumerable<GameRecommendationDto>>(okResult.Value);
            Assert.Equal(2, recommendations.Count());
        }

        [Fact]
        public async Task GetPersonalizedRecommendations_ServiceException_ReturnsInternalServerError()
        {
            // Arrange
            SetupAuthenticatedUser(_testUserId);
            _mockRecommendationService.Setup(x => x.GetPersonalizedRecommendationsAsync(_testUserId, 10))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.GetPersonalizedRecommendations();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Equal("An error occurred while getting recommendations.", statusCodeResult.Value);
        }

        [Fact]
        public async Task GetPersonalizedRecommendations_NoAuthenticatedUser_ReturnsInternalServerError()
        {
            // Arrange
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            // Act
            var result = await _controller.GetPersonalizedRecommendations();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Equal("An error occurred while getting recommendations.", statusCodeResult.Value);
        }

        [Fact]
        public async Task GetPersonalizedRecommendations_InvalidUserIdFormat_ReturnsInternalServerError()
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
            var result = await _controller.GetPersonalizedRecommendations();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Equal("An error occurred while getting recommendations.", statusCodeResult.Value);
        }

        [Fact]
        public async Task GetPersonalizedRecommendations_EmptyRecommendations_ReturnsOkWithEmptyList()
        {
            // Arrange
            SetupAuthenticatedUser(_testUserId);
            _mockRecommendationService.Setup(x => x.GetPersonalizedRecommendationsAsync(_testUserId, 10))
                .ReturnsAsync(new List<GameRecommendationDto>());

            // Act
            var result = await _controller.GetPersonalizedRecommendations();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var recommendations = Assert.IsAssignableFrom<IEnumerable<GameRecommendationDto>>(okResult.Value);
            Assert.Empty(recommendations);
        }

        #endregion
    }
}
