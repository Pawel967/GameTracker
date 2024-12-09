namespace Game_API.Dtos.Profile
{
    public class ProfileSearchDto
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public int GamesCount { get; set; }
        public int FollowersCount { get; set; }
        public bool IsFollowedByCurrentUser { get; set; }
    }
}
