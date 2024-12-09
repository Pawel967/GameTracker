namespace Game_MVC.Models.Profile
{
    public class FollowedUserViewModel
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public int GamesCount { get; set; }
    }
}
