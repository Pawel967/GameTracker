using Game_API.Data;
using Game_API.Models.IGDB;
using Game_API.Models.Library;
using Game_API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game_API.Models.Auth;
using IGDBPaginatedResponse = Game_API.Models.IGDB.PaginatedResponse<Game_API.Models.IGDB.IGDBGame>;

namespace Game_API.Tests.Services
{
    public class GameRecommendationServiceTests
    {
        private readonly Mock<ILogger<GameRecommendationService>> _loggerMock;
        private readonly Mock<IIGDBService> _igdbServiceMock;
        private readonly DbContextOptions<AppDbContext> _options;

        public GameRecommendationServiceTests()
        {
            _loggerMock = new Mock<ILogger<GameRecommendationService>>();
            _igdbServiceMock = new Mock<IIGDBService>();

            _options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
        }

        [Fact]
        public async Task GetPersonalizedRecommendations_WithGenrePreferences_ReturnsGenreBasedGames()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Create sample genres and themes
            var genre = new Genre { Id = 1, Name = "RPG" };
            var theme = new Theme { Id = 1, Name = "Fantasy" };

            // Create games
            var games = new List<Game>();
            for (int i = 1; i <= 5; i++)
            {
                var game = new Game
                {
                    Id = i,
                    Name = $"RPG Game {i}",
                    Rating = 85,
                    RatingCount = 1000,
                    Summary = "Existing game",
                    CoverUrl = "cover.jpg",
                    Developer = "Dev",
                    Publisher = "Pub",
                    GameGenres = new List<GameGenre>(),
                    GameThemes = new List<GameTheme>()
                };

                game.GameGenres.Add(new GameGenre
                {
                    GameId = game.Id,
                    GenreId = genre.Id,
                    Genre = genre
                });

                game.GameThemes.Add(new GameTheme
                {
                    GameId = game.Id,
                    ThemeId = theme.Id,
                    Theme = theme
                });

                games.Add(game);
            }

            // Create user game library entries
            var userGames = games.Select(g => new UserGameLibrary
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                GameId = g.Id,
                Game = g,
                UserRating = 9,
                IsFavorite = true,
                Status = GameStatus.Completed,
                DateAdded = DateTime.UtcNow
            }).ToList();

            // Create recommended games for IGDB service
            var recommendedGames = new List<IGDBGame>
    {
        new IGDBGame
        {
            Id = 100,
            Name = "New RPG Game 1",
            Rating = 85,
            Genres = new List<string> { "RPG" },
            Themes = new List<string> { "Fantasy" },
            SimilarGames = new List<IGDBGameBasic>(),
            Summary = "A great RPG game",
            RatingCount = 1000,
            CoverUrl = "cover1.jpg",
            Developer = "Dev1",
            Publisher = "Pub1"
        },
        new IGDBGame
        {
            Id = 101,
            Name = "New RPG Game 2",
            Rating = 88,
            Genres = new List<string> { "RPG", "Adventure" },
            Themes = new List<string> { "Fantasy" },
            SimilarGames = new List<IGDBGameBasic>(),
            Summary = "Another great RPG game",
            RatingCount = 1200,
            CoverUrl = "cover2.jpg",
            Developer = "Dev2",
            Publisher = "Pub2"
        }
    };

            // Setup database context
            using var context = new AppDbContext(_options);
            await context.Database.EnsureDeletedAsync();
            await context.Database.EnsureCreatedAsync();

            // Setup user and add all entities
            var user = new User
            {
                Id = userId,
                Username = "TestUser",
                Email = "test@example.com",
                PasswordHash = "hash",
                UserGameLibraries = userGames
            };

            context.Users.Add(user);
            context.Genres.Add(genre);
            context.Themes.Add(theme);
            context.Games.AddRange(games);
            await context.SaveChangesAsync();

            // Setup IGDB service mocks
            _igdbServiceMock.Setup(x => x.GetGamesByGenreAsync("RPG", It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new IGDBPaginatedResponse
                {
                    Items = recommendedGames,
                    PageNumber = 1,
                    PageSize = recommendedGames.Count,
                    TotalCount = recommendedGames.Count
                });

            // Setup game details for all existing games
            foreach (var game in games)
            {
                var igdbGame = new IGDBGame
                {
                    Id = game.Id,
                    Name = game.Name,
                    Rating = 85,
                    Genres = new List<string> { "RPG" },
                    Themes = new List<string> { "Fantasy" },
                    SimilarGames = new List<IGDBGameBasic>()
                };

                _igdbServiceMock.Setup(x => x.GetGameByIdAsync(game.Id))
                    .ReturnsAsync(igdbGame);
            }

            // Setup game details for recommended games
            foreach (var game in recommendedGames)
            {
                _igdbServiceMock.Setup(x => x.GetGameByIdAsync(game.Id))
                    .ReturnsAsync(game);
            }

            var service = new GameRecommendationService(context, _igdbServiceMock.Object, _loggerMock.Object);

            // Act
            var recommendations = await service.GetPersonalizedRecommendationsAsync(userId);

            // Assert
            Assert.NotNull(recommendations);
            Assert.Contains(recommendations, r => r.RecommendationReason.Contains("Based on your interest in"));
            Assert.Contains(recommendations, r => r.Game.Genres.Contains("RPG"));
            Assert.NotEmpty(recommendations);

            // Additional debugging assertions
            foreach (var recommendation in recommendations)
            {
                _loggerMock.Object.LogInformation(
                    "Recommendation: {Name}, Reason: {Reason}, Score: {Score}",
                    recommendation.Game.Name,
                    recommendation.RecommendationReason,
                    recommendation.Score);
            }
        }

        [Fact]
        public async Task GetPersonalizedRecommendations_EmptyLibrary_ReturnsPopularGames()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var popularGames = new List<IGDBGame>
            {
                new IGDBGame
                {
                    Id = 1,
                    Name = "Popular Game 1",
                    Rating = 90,
                    Genres = new List<string> { "Action" },
                    Themes = new List<string> { "Sci-fi" },
                    SimilarGames = new List<IGDBGameBasic>()
                }
            };

            _igdbServiceMock.Setup(x => x.GetAllGamesAsync(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(new IGDBPaginatedResponse
                {
                    Items = popularGames,
                    PageNumber = 1,
                    PageSize = popularGames.Count,
                    TotalCount = popularGames.Count
                });

            using var context = new AppDbContext(_options);
            var service = new GameRecommendationService(context, _igdbServiceMock.Object, _loggerMock.Object);

            // Act
            var recommendations = await service.GetPersonalizedRecommendationsAsync(userId);

            // Assert
            Assert.NotNull(recommendations);
            Assert.Equal(popularGames.Count, recommendations.Count());
            Assert.All(recommendations, r => Assert.Equal("Popular among players", r.RecommendationReason));
        }
    }
}