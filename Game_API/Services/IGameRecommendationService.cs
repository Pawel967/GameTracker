using Game_API.Dtos.Recommendation;

namespace Game_API.Services
{
    public interface IGameRecommendationService
    {
        Task<IEnumerable<GameRecommendationDto>> GetPersonalizedRecommendationsAsync(Guid userId, int count = 10);
    }
}
