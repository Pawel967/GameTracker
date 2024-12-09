namespace Game_MVC.Models.Recommendations
{
    public class RecommendationIndexViewModel
    {
        public IEnumerable<UserGenreStatsViewModel> GenreStats { get; set; } =
            Enumerable.Empty<UserGenreStatsViewModel>();
        public int TotalGames { get; set; }
        public IEnumerable<GameRecommendationViewModel>? Recommendations { get; set; }
    }
}
