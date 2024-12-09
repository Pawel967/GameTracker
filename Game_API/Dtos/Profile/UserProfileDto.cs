using Game_API.Dtos.UserLibrary;
using Game_API.Models.Library;

namespace Game_API.Dtos.Profile
{
    public class UserProfileDto
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public bool IsProfilePrivate { get; set; }

        // Following Info
        public int FollowersCount { get; set; }
        public int FollowingCount { get; set; }
        public bool IsFollowedByCurrentUser { get; set; }

        // Game Library Summary
        public Dictionary<GameStatus, int> GameStatusCounts { get; set; } = new();
        public ICollection<UserGameLibraryDto> Games { get; set; } = new List<UserGameLibraryDto>();
    }
}
