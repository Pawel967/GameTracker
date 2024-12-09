namespace Game_API.Dtos.Profile
{
    public class FollowedUserDto
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public int GamesCount { get; set; }
    }
}
