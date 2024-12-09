using Game_API.Models.Auth;

namespace Game_API.Models.Library
{
    public class UserGameLibrary
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public long GameId { get; set; }  // Foreign key to Game table

        // User-specific data
        public DateTime DateAdded { get; set; }
        public bool IsFavorite { get; set; }
        public int? UserRating { get; set; }
        public GameStatus Status { get; set; } = GameStatus.Playing;

        // Navigation properties
        public User User { get; set; } = null!;
        public Game Game { get; set; } = null!;
    }
}
