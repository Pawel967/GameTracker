using Game_MVC.Models.Library;

namespace Game_MVC.Models.Profile
{
    public class UserProfileViewModel
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public bool IsProfilePrivate { get; set; }
        public int FollowersCount { get; set; }
        public int FollowingCount { get; set; }
        public bool IsFollowedByCurrentUser { get; set; }
        public Dictionary<GameStatus, int> GameStatusCounts { get; set; } = new();
        public ICollection<UserGameLibraryViewModel> Games { get; set; } = new List<UserGameLibraryViewModel>();
    }
}
