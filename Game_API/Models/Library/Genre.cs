namespace Game_API.Models.Library
{
    public class Genre
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public ICollection<GameGenre> GameGenres { get; set; } = new List<GameGenre>();
    }
}
