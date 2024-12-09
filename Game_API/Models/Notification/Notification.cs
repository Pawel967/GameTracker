using Game_API.Models.Auth;

namespace Game_API.Models.Notification
{
    public class Notification
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }

        // Optional: Reference to the user who triggered the notification (e.g., follower)
        public Guid? TriggerUserId { get; set; }

        // Navigation properties
        public User User { get; set; } = null!;
        public User? TriggerUser { get; set; }
    }
}
