namespace Game_API.Models.Library
{
    public class Game
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public double Rating { get; set; }
        public int RatingCount { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public string CoverUrl { get; set; } = string.Empty;
        public string Developer { get; set; } = string.Empty;
        public string Publisher { get; set; } = string.Empty;

        // Navigation properties
        public ICollection<UserGameLibrary> UserGameLibraries { get; set; } = new List<UserGameLibrary>();
        public ICollection<GameGenre> GameGenres { get; set; } = new List<GameGenre>();
        public ICollection<GameTheme> GameThemes { get; set; } = new List<GameTheme>();
    }
}
