using Game_API.Services;
using Microsoft.Extensions.Logging;
using Moq.Protected;
using Moq;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Game_API.Tests.Services
{
    public class IGDBServiceTests
    {
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<IGDBService>> _mockLogger;
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly HttpClient _httpClient;

        public IGDBServiceTests()
        {
            // Setup configuration mock
            _mockConfiguration = new Mock<IConfiguration>();
            _mockConfiguration.Setup(x => x["IGDB:BaseUrl"]).Returns("https://api.igdb.com/v4");
            _mockConfiguration.Setup(x => x["IGDB:ClientId"]).Returns("test-client-id");
            _mockConfiguration.Setup(x => x["IGDB:AccessToken"]).Returns("test-access-token");

            // Setup logger mock
            _mockLogger = new Mock<ILogger<IGDBService>>();

            // Setup HTTP message handler mock
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();

            // Create HTTP client with mocked handler
            _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri("https://api.igdb.com/v4")
            };
        }

        [Fact]
        public async Task GetGameByIdAsync_ValidId_ReturnsGame()
        {
            // Arrange
            var gameId = 123;
            var expectedResponse = new JArray
            {
                new JObject
                {
                    ["id"] = gameId,
                    ["name"] = "Test Game",
                    ["summary"] = "Test Summary",
                    ["rating"] = 85.5,
                    ["rating_count"] = 1000,
                    ["first_release_date"] = 1609459200, // 2021-01-01
                    ["cover"] = new JObject
                    {
                        ["url"] = "//images.igdb.com/test.jpg"
                    },
                    ["genres"] = new JArray
                    {
                        new JObject
                        {
                            ["name"] = "Action"
                        }
                    },
                    ["themes"] = new JArray
                    {
                        new JObject
                        {
                            ["name"] = "Fantasy"
                        }
                    },
                    ["involved_companies"] = new JArray
                    {
                        new JObject
                        {
                            ["developer"] = true,
                            ["company"] = new JObject
                            {
                                ["name"] = "Test Developer"
                            }
                        },
                        new JObject
                        {
                            ["publisher"] = true,
                            ["company"] = new JObject
                            {
                                ["name"] = "Test Publisher"
                            }
                        }
                    }
                }
            };

            SetupMockHttpResponse("/games", HttpStatusCode.OK, expectedResponse.ToString());

            var service = new IGDBService(_httpClient, _mockConfiguration.Object, _mockLogger.Object);

            // Act
            var result = await service.GetGameByIdAsync(gameId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(gameId, result.Id);
            Assert.Equal("Test Game", result.Name);
            Assert.Equal("Test Summary", result.Summary);
            Assert.Equal(85.5, result.Rating);
            Assert.Equal(1000, result.RatingCount);
            Assert.Equal(new DateTime(2021, 1, 1), result.ReleaseDate);
            Assert.Equal("//images.igdb.com/test.jpg", result.CoverUrl);
            Assert.Contains("Action", result.Genres);
            Assert.Contains("Fantasy", result.Themes);
            Assert.Equal("Test Developer", result.Developer);
            Assert.Equal("Test Publisher", result.Publisher);
        }

        [Fact]
        public async Task GetGameByIdAsync_InvalidId_ReturnsNull()
        {
            // Arrange
            var gameId = 999;
            SetupMockHttpResponse("/games", HttpStatusCode.OK, "[]");

            var service = new IGDBService(_httpClient, _mockConfiguration.Object, _mockLogger.Object);

            // Act
            var result = await service.GetGameByIdAsync(gameId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task SearchGamesAsync_ValidQuery_ReturnsPaginatedGames()
        {
            // Arrange
            var searchQuery = "test";
            var expectedGames = new JArray
            {
                new JObject
                {
                    ["id"] = 1,
                    ["name"] = "Test Game 1",
                    ["rating"] = 80.0,
                    ["rating_count"] = 500
                },
                new JObject
                {
                    ["id"] = 2,
                    ["name"] = "Test Game 2",
                    ["rating"] = 85.0,
                    ["rating_count"] = 600
                }
            };

            var expectedCount = new JObject
            {
                ["count"] = 2
            };

            SetupMockHttpResponse("/games", HttpStatusCode.OK, expectedGames.ToString());
            SetupMockHttpResponse("/games/count", HttpStatusCode.OK, expectedCount.ToString());

            var service = new IGDBService(_httpClient, _mockConfiguration.Object, _mockLogger.Object);

            // Act
            var result = await service.SearchGamesAsync(searchQuery, 1, 10);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Items.Count());
            Assert.Equal(1, result.PageNumber);
            Assert.Equal(10, result.PageSize);
        }

        [Fact]
        public async Task GetAllGenresAsync_Success_ReturnsGenres()
        {
            // Arrange
            var expectedGenres = new JArray
            {
                new JObject
                {
                    ["id"] = 1,
                    ["name"] = "Action"
                },
                new JObject
                {
                    ["id"] = 2,
                    ["name"] = "Adventure"
                }
            };

            SetupMockHttpResponse("/genres", HttpStatusCode.OK, expectedGenres.ToString());

            var service = new IGDBService(_httpClient, _mockConfiguration.Object, _mockLogger.Object);

            // Act
            var result = await service.GetAllGenresAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal(2, result.Genres.Count());
            Assert.Contains(result.Genres, g => g.Name == "Action");
            Assert.Contains(result.Genres, g => g.Name == "Adventure");
        }

        [Fact]
        public async Task GetGamesByGenreAsync_ValidGenre_ReturnsPaginatedGames()
        {
            // Arrange
            var genre = "Action";
            var expectedGames = new JArray
            {
                new JObject
                {
                    ["id"] = 1,
                    ["name"] = "Action Game 1",
                    ["rating"] = 80.0,
                    ["rating_count"] = 500,
                    ["genres"] = new JArray
                    {
                        new JObject { ["name"] = "Action" }
                    }
                }
            };

            var expectedCount = new JObject
            {
                ["count"] = 1
            };

            SetupMockHttpResponse("/games", HttpStatusCode.OK, expectedGames.ToString());
            SetupMockHttpResponse("/games/count", HttpStatusCode.OK, expectedCount.ToString());

            var service = new IGDBService(_httpClient, _mockConfiguration.Object, _mockLogger.Object);

            // Act
            var result = await service.GetGamesByGenreAsync(genre, 1, 10);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.TotalCount);
            Assert.Single(result.Items);
            Assert.Contains(result.Items, g => g.Name == "Action Game 1");
        }

        [Fact]
        public async Task GetGamesByDeveloperAsync_ValidDeveloper_ReturnsPaginatedGames()
        {
            // Arrange
            var developer = "Test Developer";

            // Mock company search response
            var companyResponse = new JArray
            {
                new JObject
                {
                    ["id"] = 1,
                    ["name"] = "Test Developer"
                }
            };

            // Mock games response
            var gamesResponse = new JArray
            {
                new JObject
                {
                    ["id"] = 1,
                    ["name"] = "Developer Game 1",
                    ["rating"] = 80.0,
                    ["rating_count"] = 500,
                    ["involved_companies"] = new JArray
                    {
                        new JObject
                        {
                            ["developer"] = true,
                            ["company"] = new JObject { ["name"] = "Test Developer" }
                        }
                    }
                }
            };

            var expectedCount = new JObject
            {
                ["count"] = 1
            };

            // Setup mock responses
            SetupMockHttpResponse("/companies", HttpStatusCode.OK, companyResponse.ToString());
            SetupMockHttpResponse("/games", HttpStatusCode.OK, gamesResponse.ToString());
            SetupMockHttpResponse("/games/count", HttpStatusCode.OK, expectedCount.ToString());

            var service = new IGDBService(_httpClient, _mockConfiguration.Object, _mockLogger.Object);

            // Act
            var result = await service.GetGamesByDeveloperAsync(developer, 1, 10);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.TotalCount);
            Assert.Single(result.Items);
            Assert.Contains(result.Items, g => g.Name == "Developer Game 1");
        }

        private void SetupMockHttpResponse(string endpoint, HttpStatusCode statusCode, string content)
        {
            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.AbsolutePath.Contains(endpoint)),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new StringContent(content)
                });
        }
    }
}
