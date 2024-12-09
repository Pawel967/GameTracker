using Game_MVC.Models.Recommendations;

namespace Game_MVC.Services
{
    public interface IRecommendationService
    {
        Task<IEnumerable<GameRecommendationViewModel>> GetPersonalizedRecommendationsAsync(int count = 10);
    }
}
