using Game_API.Dtos.Notification;

namespace Game_API.Services
{
    public interface INotificationService
    {
        Task CreateFollowNotificationAsync(Guid followerId, Guid followedId);
        Task CreateNotificationAsync(string message, List<Guid>? userIds = null);
        Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(Guid userId, bool includeRead = false);
        Task MarkAsReadAsync(Guid userId, Guid notificationId);
        Task MarkAllAsReadAsync(Guid userId);
        Task<int> GetUnreadCountAsync(Guid userId);
        Task<bool> DeleteNotificationAsync(Guid userId, Guid notificationId);
    }
}
