namespace Game_API.Models.IGDB
{
    public class GameListResponse
    {
        public IEnumerable<IGDBGame> Games { get; set; } = Enumerable.Empty<IGDBGame>();
        public int Count { get; set; }
    }
}
