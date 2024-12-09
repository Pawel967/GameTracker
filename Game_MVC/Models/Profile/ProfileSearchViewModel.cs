namespace Game_MVC.Models.Profile
{
    public class ProfileSearchViewModel
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public int GamesCount { get; set; }
        public int FollowersCount { get; set; }
        public bool IsFollowedByCurrentUser { get; set; }
    }
}
