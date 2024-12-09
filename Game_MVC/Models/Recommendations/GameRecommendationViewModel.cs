using Game_MVC.Models.Game;

namespace Game_MVC.Models.Recommendations
{
    public class GameRecommendationViewModel
    {
        public GameViewModel Game { get; set; } = new();
        public string RecommendationReason { get; set; } = string.Empty;
        public double Score { get; set; }
    }
}
