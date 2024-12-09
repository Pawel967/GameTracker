using Game_MVC.Models.Library;
using System.Text.Json.Serialization;
using System.Text.Json;
using Game_MVC.Models;
using Game_MVC.Models.Recommendations;

namespace Game_MVC.Services
{
    public class LibraryService : ILibraryService
    {
        private readonly AuthenticatedHttpClient _httpClient;
        private readonly ILogger<LibraryService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public LibraryService(
            AuthenticatedHttpClient httpClient,
            ILogger<LibraryService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        public async Task<PaginatedResponse<UserGameLibraryViewModel>> GetMyLibraryAsync(LibraryFilterViewModel filter)
        {
            try
            {
                var query = $"/api/library/my?pageNumber={filter.PageNumber}&pageSize={filter.PageSize}";
                if (filter.Status.HasValue)
                    query += $"&status={filter.Status}";
                if (!string.IsNullOrEmpty(filter.SortBy))
                    query += $"&sortBy={filter.SortBy}&ascending={filter.Ascending}";

                var response = await _httpClient.GetAsync(query);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to get library with status code: {StatusCode}", response.StatusCode);
                    return new PaginatedResponse<UserGameLibraryViewModel>();
                }

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<PaginatedResponse<UserGameLibraryViewModel>>(content, _jsonOptions)
                    ?? new PaginatedResponse<UserGameLibraryViewModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user library");
                throw;
            }
        }

        public async Task<UserGameLibraryViewModel?> GetGameAsync(long gameId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/library/games/{gameId}");
                if (!response.IsSuccessStatusCode)
                    return null;

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<UserGameLibraryViewModel>(content, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting game {GameId}", gameId);
                throw;
            }
        }

        public async Task<bool> AddGameAsync(long gameId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"/api/library/games/{gameId}", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding game {GameId}", gameId);
                throw;
            }
        }

        public async Task<bool> RemoveGameAsync(long gameId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"/api/library/games/{gameId}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing game {GameId}", gameId);
                throw;
            }
        }

        public async Task<bool> UpdateGameStatusAsync(long gameId, GameStatus status)
        {
            try
            {
                var response = await _httpClient.PatchAsync(
                    $"/api/library/games/{gameId}/status",
                    JsonContent.Create(status));
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating game status {GameId}", gameId);
                throw;
            }
        }

        public async Task<bool> ToggleFavoriteAsync(long gameId)
        {
            try
            {
                var response = await _httpClient.PatchAsync($"/api/library/games/{gameId}/favorite", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling favorite {GameId}", gameId);
                throw;
            }
        }

        public async Task<bool> UpdateRatingAsync(long gameId, int rating)
        {
            try
            {
                var response = await _httpClient.PutAsync(
                    $"/api/library/games/{gameId}/rating",
                    JsonContent.Create(rating));
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating rating {GameId}", gameId);
                throw;
            }
        }
        public async Task<IEnumerable<UserGenreStatsViewModel>> GetMyGenreStatisticsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/library/my/genres");
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to get genre statistics with status code: {StatusCode}", response.StatusCode);
                    return Enumerable.Empty<UserGenreStatsViewModel>();
                }

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<IEnumerable<UserGenreStatsViewModel>>(content, _jsonOptions)
                    ?? Enumerable.Empty<UserGenreStatsViewModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting genre statistics");
                throw;
            }
        }
    }
}
