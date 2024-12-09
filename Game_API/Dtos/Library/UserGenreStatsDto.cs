namespace Game_API.Dtos.Library
{
    public class UserGenreStatsDto
    {
        public string GenreName { get; set; } = string.Empty;
        public int GamesCount { get; set; }
        public double Percentage { get; set; }
        public List<UserGameLibraryDto> Games { get; set; } = new();
    }
}
