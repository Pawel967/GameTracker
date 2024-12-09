namespace Game_API.Models.Library
{
    public class Theme
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public ICollection<GameTheme> GameThemes { get; set; } = new List<GameTheme>();
    }
}
