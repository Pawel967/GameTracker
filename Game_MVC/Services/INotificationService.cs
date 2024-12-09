using Game_MVC.Models.Notification;

namespace Game_MVC.Services
{
    public interface INotificationService
    {
        Task<IEnumerable<NotificationViewModel>> GetMyNotificationsAsync(bool includeRead = false);
        Task<int> GetUnreadCountAsync();
        Task MarkAsReadAsync(Guid notificationId);
        Task MarkAllAsReadAsync();
        Task DeleteNotificationAsync(Guid notificationId);
        Task SendNotificationAsync(CreateNotificationViewModel model);
    }
}
