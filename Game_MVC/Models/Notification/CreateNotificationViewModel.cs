namespace Game_MVC.Models.Notification
{
    public class CreateNotificationViewModel
    {
        public string Message { get; set; } = string.Empty;
        public string? UserIds { get; set; }
    }
}
