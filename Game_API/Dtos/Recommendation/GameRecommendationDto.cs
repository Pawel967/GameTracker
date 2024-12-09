using Game_API.Models.IGDB;

namespace Game_API.Dtos.Recommendation
{
    public class GameRecommendationDto
    {
        public IGDBGame Game { get; set; } = new();
        public string RecommendationReason { get; set; } = string.Empty;
        public double Score { get; set; }
    }
}
