using AutoMapper;
using Game_API.Data;
using Game_API.Dtos.UserLibrary;
using Game_API.Models.Auth;
using Game_API.Models.IGDB;
using Game_API.Models.Library;
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
    public class UserLibraryServiceTests
    {
        private readonly Mock<IIGDBService> _mockIgdbService;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<UserLibraryService>> _mockLogger;
        private readonly DbContextOptions<AppDbContext> _dbContextOptions;

        public UserLibraryServiceTests()
        {
            _mockIgdbService = new Mock<IIGDBService>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<UserLibraryService>>();

            _dbContextOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
        }

        #region AddGameToLibraryAsync Tests

        [Fact]
        public async Task AddGameToLibraryAsync_GameAlreadyExists_ReturnsFalse()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var userId = Guid.NewGuid();
            var gameId = 123L;

            var existingGame = new UserGameLibrary
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                GameId = gameId
            };
            context.UserGameLibraries.Add(existingGame);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            // Act
            var result = await service.AddGameToLibraryAsync(userId, gameId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task AddGameToLibraryAsync_GameNotFoundInIGDB_ReturnsFalse()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var userId = Guid.NewGuid();
            var gameId = 123L;

            _mockIgdbService.Setup(x => x.GetGameByIdAsync(gameId))
                .ReturnsAsync(() => null!);

            var service = CreateService(context);

            // Act
            var result = await service.AddGameToLibraryAsync(userId, gameId);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task AddGameToLibraryAsync_Success_AddsGameWithGenresAndThemes()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var userId = Guid.NewGuid();
            var gameId = 123L;

            var igdbGame = new IGDBGame
            {
                Id = gameId,
                Name = "Test Game",
                Genres = new List<string> { "Action", "RPG" },
                Themes = new List<string> { "Fantasy" }
            };

            var game = new Game
            {
                Id = gameId,
                Name = "Test Game"
            };

            var userGameLibrary = new UserGameLibrary
            {
                Id = Guid.NewGuid(),
                GameId = gameId,
                UserId = userId,
                Game = game
            };

            _mockIgdbService.Setup(x => x.GetGameByIdAsync(gameId))
                .ReturnsAsync(igdbGame);

            _mockMapper.Setup(x => x.Map<Game>(igdbGame))
                .Returns(game);

            _mockMapper.Setup(x => x.Map<UserGameLibrary>(igdbGame))
                .Returns(userGameLibrary);

            var service = CreateService(context);

            // Act
            var result = await service.AddGameToLibraryAsync(userId, gameId);

            // Assert
            Assert.True(result);

            var savedGame = await context.Games
                .Include(g => g.GameGenres)
                    .ThenInclude(gg => gg.Genre)
                .Include(g => g.GameThemes)
                    .ThenInclude(gt => gt.Theme)
                .FirstOrDefaultAsync(g => g.Id == gameId);

            Assert.NotNull(savedGame);
            Assert.Equal(2, savedGame.GameGenres.Count);
            Assert.Single(savedGame.GameThemes);
            Assert.Contains(savedGame.GameGenres, gg => gg.Genre.Name == "Action");
            Assert.Contains(savedGame.GameGenres, gg => gg.Genre.Name == "RPG");
            Assert.Contains(savedGame.GameThemes, gt => gt.Theme.Name == "Fantasy");
        }

        #endregion

        #region GetUserLibraryAsync Tests

        [Fact]
        public async Task GetUserLibraryAsync_UserNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var service = CreateService(context);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.GetUserLibraryAsync(Guid.NewGuid(), null));
        }

        [Fact]
        public async Task GetUserLibraryAsync_PrivateProfile_ReturnsEmptyList()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var userId = Guid.NewGuid();
            var requestingUserId = Guid.NewGuid();

            var user = new User
            {
                Id = userId,
                IsProfilePrivate = true
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var service = CreateService(context);

            // Act
            var result = await service.GetUserLibraryAsync(userId, requestingUserId);

            // Assert
            Assert.Empty(result.Items);
            Assert.Equal(0, result.TotalCount);
        }

        [Fact]
        public async Task GetUserLibraryAsync_WithStatusFilter_ReturnsFilteredGames()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var userId = Guid.NewGuid();

            var user = new User { Id = userId, IsProfilePrivate = false };
            context.Users.Add(user);

            var game1 = new Game { Id = 1, Name = "Game 1" };
            var game2 = new Game { Id = 2, Name = "Game 2" };
            context.Games.AddRange(game1, game2);

            var games = new List<UserGameLibrary>
            {
                new() { Id = Guid.NewGuid(), UserId = userId, GameId = 1, Game = game1, Status = GameStatus.Playing },
                new() { Id = Guid.NewGuid(), UserId = userId, GameId = 2, Game = game2, Status = GameStatus.Completed }
            };
            context.UserGameLibraries.AddRange(games);
            await context.SaveChangesAsync();

            _mockMapper.Setup(x => x.Map<IEnumerable<UserGameLibraryDto>>(It.IsAny<List<UserGameLibrary>>()))
                .Returns((List<UserGameLibrary> g) => g.Select(game => new UserGameLibraryDto()));

            var service = CreateService(context);

            // Act
            var result = await service.GetUserLibraryAsync(userId, userId, statusFilter: GameStatus.Playing);

            // Assert
            Assert.Equal(1, result.TotalCount);
        }

        #endregion

        #region GetAllGenresAsync Tests

        [Fact]
        public async Task GetAllGenresAsync_ReturnsAllGenres()
        {
            // Arrange
            using var context = new AppDbContext(_dbContextOptions);
            var genres = new List<Genre>
            {
                new() { Id = 1, Name = "Action" },
                new() { Id = 2, Name = "RPG" },
                new() { Id = 3, Name = "Strategy" }
            };
            context.Genres.AddRange(genres);
            await context.SaveChangesAsync();

            var expectedDtos = genres.Select(g => new GenreDto { Id = g.Id, Name = g.Name }).ToList();
            _mockMapper.Setup(x => x.Map<IEnumerable<GenreDto>>(It.IsAny<List<Genre>>()))
                .Returns(expectedDtos);

            var service = CreateService(context);

            // Act
            var result = await service.GetAllGenresAsync();

            // Assert
            Assert.Equal(3, result.Count());
            Assert.Contains(result, g => g.Name == "Action");
            Assert.Contains(result, g => g.Name == "RPG");
            Assert.Contains(result, g => g.Name == "Strategy");
        }

        #endregion

        private UserLibraryService CreateService(AppDbContext context)
        {
            return new UserLibraryService(
                context,
                _mockIgdbService.Object,
                _mockMapper.Object,
                _mockLogger.Object);
        }
    }
}
