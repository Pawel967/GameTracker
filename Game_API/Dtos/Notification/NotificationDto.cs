namespace Game_API.Dtos.Notification
{
    public class NotificationDto
    {
        public Guid Id { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public UserNotificationDto? TriggerUser { get; set; }
    }
}
