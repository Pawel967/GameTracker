namespace Game_API.Models.Library
{
    public class GameTheme
    {
        public long GameId { get; set; }
        public long ThemeId { get; set; }

        public Game Game { get; set; } = null!;
        public Theme Theme { get; set; } = null!;
    }
}
