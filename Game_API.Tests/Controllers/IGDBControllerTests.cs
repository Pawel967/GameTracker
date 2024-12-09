using Game_API.Controllers;
using Game_API.Models.IGDB;
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
    public class GamesControllerTests
    {
        private readonly Mock<IIGDBService> _mockIgdbService;
        private readonly Mock<ILogger<GamesController>> _mockLogger;
        private readonly GamesController _controller;

        public GamesControllerTests()
        {
            _mockIgdbService = new Mock<IIGDBService>();
            _mockLogger = new Mock<ILogger<GamesController>>();
            _controller = new GamesController(_mockIgdbService.Object, _mockLogger.Object);
        }

        #region GetGame Tests

        [Fact]
        public async Task GetGame_ExistingGame_ReturnsOkResult()
        {
            // Arrange
            long gameId = 1234;
            var expectedGame = new IGDBGame
            {
                Id = gameId,
                Name = "Test Game",
                Summary = "Test Summary",
                Rating = 85.5,
                RatingCount = 1000
            };

            _mockIgdbService.Setup(x => x.GetGameByIdAsync(gameId))
                .ReturnsAsync(expectedGame);

            // Act
            var result = await _controller.GetGame(gameId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returnedGame = Assert.IsType<IGDBGame>(okResult.Value);
            Assert.Equal(expectedGame.Id, returnedGame.Id);
            Assert.Equal(expectedGame.Name, returnedGame.Name);
        }

        [Fact]
        public async Task GetGame_NonExistingGame_ReturnsNotFound()
        {
            // Arrange
            long gameId = 1234567890123456;
            _mockIgdbService.Setup(x => x.GetGameByIdAsync(gameId))
                .ReturnsAsync((IGDBGame)null);

            // Act
            var result = await _controller.GetGame(gameId);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        #endregion

        #region SearchGames Tests

        [Fact]
        public async Task SearchGames_ValidQuery_ReturnsOkResult()
        {
            // Arrange
            var query = "test game";
            var expectedResponse = new PaginatedResponse<IGDBGame>
            {
                Items = new List<IGDBGame>
                {
                    new IGDBGame { Id = 1, Name = "Test Game 1" },
                    new IGDBGame { Id = 2, Name = "Test Game 2" }
                },
                PageNumber = 1,
                PageSize = 10,
                TotalCount = 2
            };

            _mockIgdbService.Setup(x => x.SearchGamesAsync(query, 1, 10))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.SearchGames(query);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<PaginatedResponse<IGDBGame>>(okResult.Value);
            Assert.Equal(2, response.Items.Count());
        }

        [Fact]
        public async Task SearchGames_NoQuery_ReturnsOkResult()
        {
            // Arrange
            var expectedResponse = new PaginatedResponse<IGDBGame>
            {
                Items = new List<IGDBGame>(),
                PageNumber = 1,
                PageSize = 10,
                TotalCount = 0
            };

            _mockIgdbService.Setup(x => x.SearchGamesAsync(null, 1, 10))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.SearchGames(null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<PaginatedResponse<IGDBGame>>(okResult.Value);
            Assert.Empty(response.Items);
        }

        #endregion

        #region GetGamesByGenre Tests

        [Fact]
        public async Task GetGamesByGenre_ValidGenre_ReturnsOkResult()
        {
            // Arrange
            var genre = "RPG";
            var expectedResponse = new PaginatedResponse<IGDBGame>
            {
                Items = new List<IGDBGame>
                {
                    new IGDBGame { Id = 1, Name = "RPG Game 1" },
                    new IGDBGame { Id = 2, Name = "RPG Game 2" }
                },
                PageNumber = 1,
                PageSize = 10,
                TotalCount = 2
            };

            _mockIgdbService.Setup(x => x.GetGamesByGenreAsync(genre, 1, 10))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetGamesByGenre(genre);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<PaginatedResponse<IGDBGame>>(okResult.Value);
            Assert.Equal(2, response.Items.Count());
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("   ")]
        public async Task GetGamesByGenre_InvalidGenre_ReturnsBadRequest(string genre)
        {
            // Act
            var result = await _controller.GetGamesByGenre(genre);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Genre parameter is required", badRequestResult.Value);
        }

        #endregion

        #region GetGamesByDeveloper Tests

        [Fact]
        public async Task GetGamesByDeveloper_ValidDeveloper_ReturnsOkResult()
        {
            // Arrange
            var developer = "Nintendo";
            var expectedResponse = new PaginatedResponse<IGDBGame>
            {
                Items = new List<IGDBGame>
                {
                    new IGDBGame { Id = 1, Name = "Nintendo Game 1" },
                    new IGDBGame { Id = 2, Name = "Nintendo Game 2" }
                },
                PageNumber = 1,
                PageSize = 10,
                TotalCount = 2
            };

            _mockIgdbService.Setup(x => x.GetGamesByDeveloperAsync(developer, 1, 10))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetGamesByDeveloper(developer);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<PaginatedResponse<IGDBGame>>(okResult.Value);
            Assert.Equal(2, response.Items.Count());
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        [InlineData("   ")]
        public async Task GetGamesByDeveloper_InvalidDeveloper_ReturnsBadRequest(string developer)
        {
            // Act
            var result = await _controller.GetGamesByDeveloper(developer);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Developer parameter is required", badRequestResult.Value);
        }

        #endregion

        #region GetAllGames Tests

        [Fact]
        public async Task GetAllGames_ReturnsOkResult()
        {
            // Arrange
            var expectedResponse = new PaginatedResponse<IGDBGame>
            {
                Items = new List<IGDBGame>
                {
                    new IGDBGame { Id = 1, Name = "Game 1" },
                    new IGDBGame { Id = 2, Name = "Game 2" }
                },
                PageNumber = 1,
                PageSize = 10,
                TotalCount = 2
            };

            _mockIgdbService.Setup(x => x.GetAllGamesAsync(1, 10))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetAllGames();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<PaginatedResponse<IGDBGame>>(okResult.Value);
            Assert.Equal(2, response.Items.Count());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public async Task GetAllGames_InvalidPageNumber_ReturnsOkResult(int pageNumber)
        {
            // Arrange
            var expectedResponse = new PaginatedResponse<IGDBGame>
            {
                Items = new List<IGDBGame>(),
                PageNumber = pageNumber,
                PageSize = 10,
                TotalCount = 0
            };

            _mockIgdbService.Setup(x => x.GetAllGamesAsync(pageNumber, 10))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetAllGames(pageNumber);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<PaginatedResponse<IGDBGame>>(okResult.Value);
            Assert.Empty(response.Items);
        }

        #endregion

        #region GetAllGenres Tests

        [Fact]
        public async Task GetAllGenres_ReturnsOkResult()
        {
            // Arrange
            var expectedResponse = new GenreListResponse
            {
                Genres = new List<IGDBGenre>
                {
                    new IGDBGenre { Id = 1, Name = "Action" },
                    new IGDBGenre { Id = 2, Name = "RPG" }
                },
                Count = 2
            };

            _mockIgdbService.Setup(x => x.GetAllGenresAsync(null))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetAllGenres();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<GenreListResponse>(okResult.Value);
            Assert.Equal(2, response.Count);
            Assert.Equal(2, response.Genres.Count());
        }

        [Fact]
        public async Task GetAllGenres_WithLimit_ReturnsOkResult()
        {
            // Arrange
            var limit = 1;
            var expectedResponse = new GenreListResponse
            {
                Genres = new List<IGDBGenre>
                {
                    new IGDBGenre { Id = 1, Name = "Action" }
                },
                Count = 1
            };

            _mockIgdbService.Setup(x => x.GetAllGenresAsync(limit))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.GetAllGenres(limit);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<GenreListResponse>(okResult.Value);
            Assert.Equal(1, response.Count);
            Assert.Single(response.Genres);
        }

        #endregion
    }
}
