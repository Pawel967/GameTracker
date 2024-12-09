namespace Game_API.Models.Library
{
    public class GameGenre
    {
        public long GameId { get; set; }
        public long GenreId { get; set; }

        public Game Game { get; set; } = null!;
        public Genre Genre { get; set; } = null!;
    }
}
