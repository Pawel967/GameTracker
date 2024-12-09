using Game_MVC.Models;
using Game_MVC.Models.Game;
using System.Text.Json;

namespace Game_MVC.Services
{
    public class GameService : IGameService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GameService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public GameService(IHttpClientFactory httpClientFactory, ILogger<GameService> logger)
        {
            _httpClient = httpClientFactory.CreateClient("GameApiClient");
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };
        }

        public async Task<GameViewModel?> GetGameByIdAsync(long id)
        {
            var url = $"/api/Games/{id}";
            return await GetApiResponse<GameViewModel>(url);
        }

        public async Task<PaginatedResponse<GameViewModel>> SearchGamesAsync(string? query, int pageNumber = 1, int pageSize = 10)
        {
            var url = BuildUrlWithParams("/api/Games/search", new Dictionary<string, string?>
            {
                { "query", query },
                { "pageNumber", pageNumber.ToString() },
                { "pageSize", pageSize.ToString() }
            });
            return await GetApiResponse<PaginatedResponse<GameViewModel>>(url);
        }

        public async Task<PaginatedResponse<GameViewModel>> GetGamesByGenreAsync(string genre, int pageNumber = 1, int pageSize = 10)
        {
            var url = BuildUrlWithParams("/api/Games/by-genre", new Dictionary<string, string?>
            {
                { "genre", genre },
                { "pageNumber", pageNumber.ToString() },
                { "pageSize", pageSize.ToString() }
            });
            return await GetApiResponse<PaginatedResponse<GameViewModel>>(url);
        }

        public async Task<PaginatedResponse<GameViewModel>> GetGamesByDeveloperAsync(string developer, int pageNumber = 1, int pageSize = 10)
        {
            var url = BuildUrlWithParams("/api/Games/by-developer", new Dictionary<string, string?>
            {
                { "developer", developer },
                { "pageNumber", pageNumber.ToString() },
                { "pageSize", pageSize.ToString() }
            });
            return await GetApiResponse<PaginatedResponse<GameViewModel>>(url);
        }

        public async Task<PaginatedResponse<GameViewModel>> GetAllGamesAsync(int pageNumber = 1, int pageSize = 10)
        {
            var url = BuildUrlWithParams("/api/Games/all", new Dictionary<string, string?>
            {
                { "pageNumber", pageNumber.ToString() },
                { "pageSize", pageSize.ToString() }
            });
            return await GetApiResponse<PaginatedResponse<GameViewModel>>(url);
        }

        public async Task<GenreListResponse> GetAllGenresAsync(int? limit = null)
        {
            var url = BuildUrlWithParams("/api/Games/genres", new Dictionary<string, string?>
            {
                { "limit", limit?.ToString() }
            });
            return await GetApiResponse<GenreListResponse>(url);
        }

        // Helper methods
        private async Task<T> GetApiResponse<T>(string url) where T : class, new()
        {
            try
            {
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<T>(content, _jsonOptions);
                    return result ?? new T();
                }

                _logger.LogWarning("API returned non-success status code: {StatusCode} for URL: {Url}",
                    response.StatusCode, url);
                return new T();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching data from {Url}", url);
                return new T();
            }
        }

        private string BuildUrlWithParams(string baseUrl, Dictionary<string, string?> parameters)
        {
            var query = string.Join("&", parameters
                .Where(p => !string.IsNullOrEmpty(p.Value))
                .Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value!)}"));

            return string.IsNullOrEmpty(query) ? baseUrl : $"{baseUrl}?{query}";
        }
    }
}
