namespace Game_API.Models.IGDB
{
    public class IGDBGame
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public double Rating { get; set; }
        public int RatingCount { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public string CoverUrl { get; set; } = string.Empty;
        public List<string> Genres { get; set; } = new();
        public List<string> Themes { get; set; } = new();
        public string Developer { get; set; } = string.Empty;
        public string Publisher { get; set; } = string.Empty;
        public List<IGDBGameBasic> SimilarGames { get; set; } = new();
    }
}
