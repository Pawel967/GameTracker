using Game_API.Models.Library;
using Game_API.Models.Profile;
using System.ComponentModel.DataAnnotations;

namespace Game_API.Models.Auth
{
    public class User
    {
        public Guid Id { get; set; }

        [Required]
        [StringLength(50, MinimumLength = 3)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public bool IsProfilePrivate { get; set; }

        // Navigation properties
        public List<Role> Roles { get; set; } = new List<Role>();

        // Library relationship
        public ICollection<UserGameLibrary> UserGameLibraries { get; set; } = new List<UserGameLibrary>();

        // Social relationships
        public ICollection<UserFollowing> FollowedBy { get; set; } = new List<UserFollowing>();
        public ICollection<UserFollowing> Following { get; set; } = new List<UserFollowing>();
    }
}
