namespace Game_API.Models.IGDB
{
    public class GenreListResponse
    {
        public IEnumerable<IGDBGenre> Genres { get; set; } = Enumerable.Empty<IGDBGenre>();
        public int Count { get; set; }
    }
}
