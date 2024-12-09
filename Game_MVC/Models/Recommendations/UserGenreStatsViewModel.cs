namespace Game_MVC.Models.Recommendations
{
    public class UserGenreStatsViewModel
    {
        public string GenreName { get; set; } = string.Empty;
        public int GamesCount { get; set; }
        public double Percentage { get; set; }
    }
}
