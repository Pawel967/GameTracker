using Game_API.Models.IGDB;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace Game_API.Services
{
    public class IGDBService : IIGDBService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<IGDBService> _logger;
        private const int MinimumRatingCount = 20;

        public IGDBService(HttpClient httpClient, IConfiguration configuration, ILogger<IGDBService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        private async Task<HttpRequestMessage> CreateIGDBRequest(string endpoint, string query)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_configuration["IGDB:BaseUrl"]}{endpoint}");

            request.Headers.Add("Client-ID", _configuration["IGDB:ClientId"]);
            request.Headers.Add("Authorization", $"Bearer {_configuration["IGDB:AccessToken"]}");

            request.Content = new StringContent(query);
            request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            return request;
        }

        private string BuildGameQuery(string? whereClause = null, int offset = 0, int? limit = null)
        {
            var queryBuilder = new StringBuilder();

            queryBuilder.AppendLine("fields name,summary,rating,rating_count," +
                                    "cover.url," +
                                    "genres.name,involved_companies.*,involved_companies.company.name," +
                                    "involved_companies.developer,involved_companies.publisher," +
                                    "themes.name,first_release_date," +
                                    "similar_games.name,similar_games.cover.url,similar_games.id,similar_games.rating_count;");

            if (!string.IsNullOrEmpty(whereClause))
            {
                queryBuilder.AppendLine($"where {whereClause};");
            }

            if (offset > 0)
            {
                queryBuilder.AppendLine($"offset {offset};");
            }

            if (limit.HasValue)
            {
                queryBuilder.AppendLine($"limit {limit.Value};");
            }

            return queryBuilder.ToString();
        }

        private IEnumerable<IGDBGame> ParseGamesResponse(string content)
        {
            var games = JArray.Parse(content);
            return games.Select(MapGameFromJson).ToList();
        }

        private IEnumerable<IGDBGame> HandleApiErrorResponse(HttpResponseMessage response)
        {
            var errorContent = response.Content.ReadAsStringAsync().Result;
            _logger.LogError("IGDB Error: {StatusCode} - {Content}", response.StatusCode, errorContent);
            throw new HttpRequestException($"IGDB API returned {response.StatusCode}: {errorContent}");
        }

        private IGDBGame MapGameFromJson(JToken game)
        {
            var involvedCompanies = game["involved_companies"] as JArray;
            var developer = involvedCompanies?
                .FirstOrDefault(ic => ic["developer"]?.Value<bool>() == true)?
                ["company"]?["name"]?.Value<string>() ?? string.Empty;

            var publisher = involvedCompanies?
                .FirstOrDefault(ic => ic["publisher"]?.Value<bool>() == true)?
                ["company"]?["name"]?.Value<string>() ?? string.Empty;

            DateTime? releaseDate = null;
            if (game["first_release_date"]?.Value<long>() is long timestamp)
            {
                releaseDate = DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;
            }

            var coverUrl = game["cover"]?["url"]?.Value<string>() ?? string.Empty;

            // Adjust the image URL to request a higher resolution by replacing `t_thumb`
            if (!string.IsNullOrEmpty(coverUrl))
            {
                coverUrl = coverUrl.Replace("t_thumb", "t_cover_big"); // Use t_cover_huge for even higher quality
            }

            var similarGames = (game["similar_games"] as JArray)?
                .Where(g => g["rating_count"]?.Value<int>() >= MinimumRatingCount)
                .Select(g => new IGDBGameBasic
                {
                    Id = g["id"]?.Value<long>() ?? 0,
                    Name = g["name"]?.Value<string>() ?? string.Empty,
                    CoverUrl = g["cover"]?["url"]?.Value<string>()?.Replace("t_thumb", "t_cover_big") ?? string.Empty
                }).ToList() ?? new List<IGDBGameBasic>();

            return new IGDBGame
            {
                Id = game["id"]?.Value<long>() ?? 0,
                Name = game["name"]?.Value<string>() ?? string.Empty,
                Summary = game["summary"]?.Value<string>() ?? string.Empty,
                Rating = game["rating"]?.Value<double>() ?? 0.0,
                RatingCount = game["rating_count"]?.Value<int>() ?? 0,
                ReleaseDate = releaseDate,
                CoverUrl = coverUrl,
                Genres = (game["genres"] as JArray)?.Select(g => g["name"]?.Value<string>() ?? string.Empty).ToList() ?? new List<string>(),
                Themes = (game["themes"] as JArray)?.Select(t => t["name"]?.Value<string>() ?? string.Empty).ToList() ?? new List<string>(),
                Developer = developer,
                Publisher = publisher,
                SimilarGames = similarGames
            };
        }

        public async Task<IGDBGame?> GetGameByIdAsync(long id)
        {
            try
            {
                var whereClause = $"id = {id} & rating_count >= {MinimumRatingCount}";
                var query = BuildGameQuery(whereClause);
                _logger.LogInformation("IGDB Get Game By ID Query: {Query}", query);

                var request = await CreateIGDBRequest("/games", query);
                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("IGDB Response: {Content}", content);

                    var games = JArray.Parse(content);
                    return games.Any() ? MapGameFromJson(games.First()) : null;
                }
                else
                {
                    return HandleApiErrorResponse(response).FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving game with ID: {Id}", id);
                throw;
            }
        }

        public async Task<PaginatedResponse<IGDBGame>> SearchGamesAsync(string? searchQuery, int pageNumber, int pageSize)
        {
            try
            {
                var whereClause = $"rating_count >= {MinimumRatingCount}";
                if (!string.IsNullOrEmpty(searchQuery))
                {
                    var escapedQuery = searchQuery.Replace("\"", "\\\"");
                    whereClause = $"search \"{escapedQuery}\"; " + whereClause;
                }

                var offset = (pageNumber - 1) * pageSize;
                var query = BuildGameQuery(whereClause, offset, pageSize);

                var countQuery = $"where {whereClause};";
                var totalCount = await GetTotalCount(countQuery);

                var request = await CreateIGDBRequest("/games", query);
                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var games = ParseGamesResponse(content);

                    return new PaginatedResponse<IGDBGame>
                    {
                        Items = games,
                        PageNumber = pageNumber,
                        PageSize = pageSize,
                        TotalCount = totalCount,
                        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                    };
                }

                return new PaginatedResponse<IGDBGame>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching games");
                throw;
            }
        }

        public async Task<PaginatedResponse<IGDBGame>> GetGamesByGenreAsync(string genre, int pageNumber, int pageSize)
        {
            try
            {
                var whereClause = $"genres.name = \"{genre}\" & rating_count >= {MinimumRatingCount}";
                var offset = (pageNumber - 1) * pageSize;
                var query = BuildGameQuery(whereClause, offset, pageSize);

                var countQuery = $"where {whereClause};";
                var totalCount = await GetTotalCount(countQuery);

                var request = await CreateIGDBRequest("/games", query);
                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var games = ParseGamesResponse(content);

                    return new PaginatedResponse<IGDBGame>
                    {
                        Items = games,
                        PageNumber = pageNumber,
                        PageSize = pageSize,
                        TotalCount = totalCount,
                        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                    };
                }

                return new PaginatedResponse<IGDBGame>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting games by genre");
                throw;
            }
        }

        public async Task<PaginatedResponse<IGDBGame>> GetGamesByDeveloperAsync(string developer, int pageNumber, int pageSize)
        {
            try
            {
                var companyQuery = $"fields id; where name = \"{developer}\";";
                var companyRequest = await CreateIGDBRequest("/companies", companyQuery);
                var companyResponse = await _httpClient.SendAsync(companyRequest);

                if (!companyResponse.IsSuccessStatusCode)
                {
                    return new PaginatedResponse<IGDBGame>();
                }

                var companyContent = await companyResponse.Content.ReadAsStringAsync();
                var companies = JArray.Parse(companyContent);
                if (!companies.Any()) return new PaginatedResponse<IGDBGame>();

                var companyId = companies.First()["id"]?.Value<long>();
                var whereClause = $"involved_companies.company = {companyId} & involved_companies.developer = true & rating_count >= {MinimumRatingCount}";

                var offset = (pageNumber - 1) * pageSize;
                var query = BuildGameQuery(whereClause, offset, pageSize);

                var countQuery = $"where {whereClause};";
                var totalCount = await GetTotalCount(countQuery);

                var gamesRequest = await CreateIGDBRequest("/games", query);
                var gamesResponse = await _httpClient.SendAsync(gamesRequest);

                if (gamesResponse.IsSuccessStatusCode)
                {
                    var content = await gamesResponse.Content.ReadAsStringAsync();
                    var games = ParseGamesResponse(content);

                    return new PaginatedResponse<IGDBGame>
                    {
                        Items = games,
                        PageNumber = pageNumber,
                        PageSize = pageSize,
                        TotalCount = totalCount,
                        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                    };
                }

                return new PaginatedResponse<IGDBGame>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting games by developer");
                throw;
            }
        }

        public async Task<PaginatedResponse<IGDBGame>> GetAllGamesAsync(int pageNumber, int pageSize)
        {
            try
            {
                var whereClause = $"rating != null & first_release_date != null & rating_count >= {MinimumRatingCount} & first_release_date < {DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
                var offset = (pageNumber - 1) * pageSize;
                var query = BuildGameQuery(whereClause, offset, pageSize) + "sort rating desc;";

                var countQuery = $"where {whereClause};";
                var totalCount = await GetTotalCount(countQuery);

                var request = await CreateIGDBRequest("/games", query);
                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var games = ParseGamesResponse(content);

                    return new PaginatedResponse<IGDBGame>
                    {
                        Items = games,
                        PageNumber = pageNumber,
                        PageSize = pageSize,
                        TotalCount = totalCount,
                        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                    };
                }

                return new PaginatedResponse<IGDBGame>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all games");
                throw;
            }
        }
        public async Task<GenreListResponse> GetAllGenresAsync(int? limit = null)
        {
            try
            {
                var query = "fields id,name; sort name asc; limit 30;";

                var request = await CreateIGDBRequest("/genres", query);
                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var genres = JArray.Parse(content);
                    _logger.LogInformation("Retrieved {count} genres", genres.Count());

                    return new GenreListResponse
                    {
                        Genres = genres.Select(g => new IGDBGenre
                        {
                            Id = g["id"]?.Value<long>() ?? 0,
                            Name = g["name"]?.Value<string>() ?? string.Empty
                        }).ToList(),
                        Count = genres.Count()
                    };
                }
                else
                {
                    _logger.LogError("IGDB Error: {StatusCode} - {Content}",
                        response.StatusCode,
                        await response.Content.ReadAsStringAsync());
                    return new GenreListResponse();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving genres");
                return new GenreListResponse();
            }
        }
        private async Task<int> GetTotalCount(string whereClause)
        {
            var countRequest = await CreateIGDBRequest("/games/count", whereClause);
            var countResponse = await _httpClient.SendAsync(countRequest);

            if (countResponse.IsSuccessStatusCode)
            {
                var countContent = await countResponse.Content.ReadAsStringAsync();
                var countObject = JObject.Parse(countContent);
                return countObject["count"]?.Value<int>() ?? 0;
            }

            return 0;
        }

    }
}