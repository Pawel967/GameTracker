namespace Game_API.Dtos.Notification
{
    public class CreateNotificationDto
    {
        public string Message { get; set; } = string.Empty;
        public List<Guid>? UserIds { get; set; }  // If null, send to all users
    }
}
