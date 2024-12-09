using Game_API.Models.Auth;

namespace Game_API.Models.Profile
{
    public class UserFollowing
    {
        public Guid FollowerId { get; set; }
        public Guid FollowedId { get; set; }

        // Navigation properties
        public User Follower { get; set; } = null!;
        public User Followed { get; set; } = null!;
    }
}
