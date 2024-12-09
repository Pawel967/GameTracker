using Game_MVC.Models.Recommendations;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace Game_MVC.Services
{
    public class RecommendationService : IRecommendationService
    {
        private readonly AuthenticatedHttpClient _httpClient;
        private readonly ILogger<RecommendationService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public RecommendationService(
            AuthenticatedHttpClient httpClient,
            ILogger<RecommendationService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        public async Task<IEnumerable<GameRecommendationViewModel>> GetPersonalizedRecommendationsAsync(int count = 10)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/recommendations/personalized?count={count}");
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to get recommendations with status code: {StatusCode}", response.StatusCode);
                    return Enumerable.Empty<GameRecommendationViewModel>();
                }

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<IEnumerable<GameRecommendationViewModel>>(content, _jsonOptions)
                    ?? Enumerable.Empty<GameRecommendationViewModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting personalized recommendations");
                throw;
            }
        }
    }
}
