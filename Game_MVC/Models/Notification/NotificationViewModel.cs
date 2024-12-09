namespace Game_MVC.Models.Notification
{
    public class NotificationViewModel
    {
        public Guid Id { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public UserNotificationViewModel? TriggerUser { get; set; }
    }

}
