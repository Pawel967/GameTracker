namespace Game_API.Models.IGDB
{
    public class IGDBConfiguration
    {
        public string ClientId { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
        public string BaseUrl { get; set; } = "https://api.igdb.com/v4";
    }
}
