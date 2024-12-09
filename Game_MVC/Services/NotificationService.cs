using Game_MVC.Models.Notification;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Text;

namespace Game_MVC.Services
{
    public class NotificationService : INotificationService
    {
        private readonly AuthenticatedHttpClient _httpClient;
        private readonly ILogger<NotificationService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public NotificationService(
            AuthenticatedHttpClient httpClient,
            ILogger<NotificationService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        public async Task<IEnumerable<NotificationViewModel>> GetMyNotificationsAsync(bool includeRead = false)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/notification?includeRead={includeRead}");
                if (!response.IsSuccessStatusCode)
                    return Enumerable.Empty<NotificationViewModel>();

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<IEnumerable<NotificationViewModel>>(content, _jsonOptions)
                    ?? Enumerable.Empty<NotificationViewModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notifications");
                throw;
            }
        }

        public async Task<int> GetUnreadCountAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/notification/unread-count");
                if (!response.IsSuccessStatusCode)
                    return 0;

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<int>(content, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread count");
                throw;
            }
        }

        public async Task MarkAsReadAsync(Guid notificationId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"/api/notification/{notificationId}/read", null);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification as read");
                throw;
            }
        }

        public async Task MarkAllAsReadAsync()
        {
            try
            {
                var response = await _httpClient.PostAsync("/api/notification/mark-all-read", null);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read");
                throw;
            }
        }

        public async Task DeleteNotificationAsync(Guid notificationId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"/api/notification/{notificationId}");
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting notification");
                throw;
            }
        }

        public async Task SendNotificationAsync(CreateNotificationViewModel model)
        {
            try
            {
                var content = JsonSerializer.Serialize(model, _jsonOptions);
                var response = await _httpClient.PostAsync("/api/notification/send",
                    new StringContent(content, Encoding.UTF8, "application/json"));
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification");
                throw;
            }
        }
    }
}
