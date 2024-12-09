using Game_API.Controllers;
using Game_API.Dtos.UserLibrary;
using Game_API.Models.Library;
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
    public class UserLibraryControllerTests
    {
        private readonly Mock<IUserLibraryService> _mockLibraryService;
        private readonly Mock<ILogger<UserLibraryController>> _mockLogger;
        private readonly UserLibraryController _controller;
        private readonly Guid _testUserId;

        public UserLibraryControllerTests()
        {
            _mockLibraryService = new Mock<IUserLibraryService>();
            _mockLogger = new Mock<ILogger<UserLibraryController>>();
            _controller = new UserLibraryController(_mockLibraryService.Object, _mockLogger.Object);
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

        private void SetupUnauthenticatedUser()
        {
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
        }

        private PaginatedResponse<UserGameLibraryDto> CreateSamplePaginatedResponse()
        {
            return new PaginatedResponse<UserGameLibraryDto>
            {
                Items = new List<UserGameLibraryDto>
                {
                    new UserGameLibraryDto
                    {
                        GameId = 1, // Changed from IGDBGameId to GameId
                        Name = "Test Game",
                        Status = GameStatus.Playing
                    }
                },
                PageNumber = 1,
                PageSize = 10,
                TotalCount = 1
            };
        }

        #endregion

        #region GetMyLibrary Tests

        [Fact]
        public async Task GetMyLibrary_AuthenticatedUser_ReturnsOkResult()
        {
            // Arrange
            SetupAuthenticatedUser(_testUserId);
            var expectedResponse = CreateSamplePaginatedResponse();

            _mockLibraryService.Setup(x => x.GetUserLibraryAsync(
                _testUserId, _testUserId, 1, 10, null, null, true))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetMyLibrary();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<PaginatedResponse<UserGameLibraryDto>>(okResult.Value);
            Assert.Single(response.Items);
        }

        [Fact]
        public async Task GetMyLibrary_WithFilters_ReturnsFilteredResults()
        {
            // Arrange
            SetupAuthenticatedUser(_testUserId);
            var expectedResponse = CreateSamplePaginatedResponse();

            _mockLibraryService.Setup(x => x.GetUserLibraryAsync(
                _testUserId, _testUserId, 2, 5, GameStatus.Playing, "name", false))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetMyLibrary(2, 5, GameStatus.Playing, "name", false);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<PaginatedResponse<UserGameLibraryDto>>(okResult.Value);
            Assert.Single(response.Items);
        }

        #endregion

        #region GetUserLibrary Tests

        [Fact]
        public async Task GetUserLibrary_AnonymousUser_ReturnsOkResult()
        {
            // Arrange
            SetupUnauthenticatedUser();
            var targetUserId = Guid.NewGuid();
            var expectedResponse = CreateSamplePaginatedResponse();

            _mockLibraryService.Setup(x => x.GetUserLibraryAsync(
                targetUserId, null, 1, 10, null, null, true))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetUserLibrary(targetUserId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<PaginatedResponse<UserGameLibraryDto>>(okResult.Value);
            Assert.Single(response.Items);
        }

        #endregion

        #region AddGame Tests

        [Fact]
        public async Task AddGame_Success_ReturnsOkResult()
        {
            // Arrange
            SetupAuthenticatedUser(_testUserId);
            long gameId = 123;

            _mockLibraryService.Setup(x => x.AddGameToLibraryAsync(_testUserId, gameId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.AddGame(gameId);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task AddGame_AlreadyExists_ReturnsBadRequest()
        {
            // Arrange
            SetupAuthenticatedUser(_testUserId);
            long gameId = 123;

            _mockLibraryService.Setup(x => x.AddGameToLibraryAsync(_testUserId, gameId))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.AddGame(gameId);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Game could not be added to library. It might already exist in your library.",
                badRequestResult.Value);
        }

        #endregion

        #region RemoveGame Tests

        [Fact]
        public async Task RemoveGame_Success_ReturnsOkResult()
        {
            // Arrange
            SetupAuthenticatedUser(_testUserId);
            long gameId = 123;

            _mockLibraryService.Setup(x => x.RemoveGameFromLibraryAsync(_testUserId, gameId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.RemoveGame(gameId);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task RemoveGame_NotFound_ReturnsNotFound()
        {
            // Arrange
            SetupAuthenticatedUser(_testUserId);
            long gameId = 123;

            _mockLibraryService.Setup(x => x.RemoveGameFromLibraryAsync(_testUserId, gameId))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.RemoveGame(gameId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Game not found in your library.", notFoundResult.Value);
        }

        #endregion

        #region UpdateGameStatus Tests

        [Fact]
        public async Task UpdateGameStatus_Success_ReturnsOkResult()
        {
            // Arrange
            SetupAuthenticatedUser(_testUserId);
            long gameId = 123;
            var status = GameStatus.Completed;

            _mockLibraryService.Setup(x => x.UpdateGameStatusAsync(_testUserId, gameId, status))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.UpdateGameStatus(gameId, status);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task UpdateGameStatus_GameNotFound_ReturnsNotFound()
        {
            // Arrange
            SetupAuthenticatedUser(_testUserId);
            long gameId = 123;
            var status = GameStatus.Completed;

            _mockLibraryService.Setup(x => x.UpdateGameStatusAsync(_testUserId, gameId, status))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.UpdateGameStatus(gameId, status);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Game not found in your library.", notFoundResult.Value);
        }

        #endregion

        #region UpdateGameRating Tests

        [Theory]
        [InlineData(0)]
        [InlineData(11)]
        public async Task UpdateGameRating_InvalidRating_ReturnsBadRequest(int rating)
        {
            // Arrange
            SetupAuthenticatedUser(_testUserId);
            long gameId = 123;

            // Act
            var result = await _controller.UpdateGameRating(gameId, rating);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Rating must be between 1 and 10.", badRequestResult.Value);
        }

        [Fact]
        public async Task UpdateGameRating_ValidRating_ReturnsOkResult()
        {
            // Arrange
            SetupAuthenticatedUser(_testUserId);
            long gameId = 123;
            int rating = 5;

            _mockLibraryService.Setup(x => x.UpdateGameRatingAsync(_testUserId, gameId, rating))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.UpdateGameRating(gameId, rating);

            // Assert
            Assert.IsType<OkResult>(result);
        }

        #endregion

        #region GetAllGenres Tests

        [Fact]
        public async Task GetAllGenres_ReturnsOkResult()
        {
            // Arrange
            var expectedGenres = new List<GenreDto>
            {
                new GenreDto { Id = 1, Name = "Action" },
                new GenreDto { Id = 2, Name = "RPG" }
            };

            _mockLibraryService.Setup(x => x.GetAllGenresAsync())
                .ReturnsAsync(expectedGenres);

            // Act
            var result = await _controller.GetAllGenres();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var genres = Assert.IsAssignableFrom<IEnumerable<GenreDto>>(okResult.Value);
            Assert.Equal(2, genres.Count());
        }

        #endregion

        #region GetMyGenreStatistics Tests

        [Fact]
        public async Task GetMyGenreStatistics_ReturnsOkResult()
        {
            // Arrange
            SetupAuthenticatedUser(_testUserId);
            var expectedStats = new List<UserGenreStatsDto>
            {
                new UserGenreStatsDto
                {
                    GenreName = "Action",
                    GamesCount = 5,
                    Percentage = 50
                }
            };

            _mockLibraryService.Setup(x => x.GetUserGenreStatisticsAsync(_testUserId, _testUserId))
                .ReturnsAsync(expectedStats);

            // Act
            var result = await _controller.GetMyGenreStatistics();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var stats = Assert.IsAssignableFrom<IEnumerable<UserGenreStatsDto>>(okResult.Value);
            Assert.Single(stats);
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public async Task AnyEndpoint_ServiceThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            SetupAuthenticatedUser(_testUserId);
            _mockLibraryService.Setup(x => x.GetUserLibraryAsync(
                It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<int>(),
                It.IsAny<int>(), It.IsAny<GameStatus?>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ThrowsAsync(new Exception("Test exception"));

            // Act
            var result = await _controller.GetMyLibrary();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            Assert.Equal("An error occurred while retrieving your library.", statusCodeResult.Value);
        }

        #endregion
    }
}
