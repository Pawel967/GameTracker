namespace Game_API.Models.IGDB
{
    public class IGDBGameBasic
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string CoverUrl { get; set; } = string.Empty;
    }
}
