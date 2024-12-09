using Game_API.Dtos.Notification;
using Game_API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Game_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(
            INotificationService notificationService,
            ILogger<NotificationController> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<NotificationDto>>> GetMyNotifications(
            [FromQuery] bool includeRead = false)
        {
            try
            {
                var userId = GetUserId();
                var notifications = await _notificationService.GetUserNotificationsAsync(userId, includeRead);
                return Ok(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notifications");
                return StatusCode(500, "An error occurred while retrieving notifications");
            }
        }

        [HttpGet("unread-count")]
        public async Task<ActionResult<int>> GetUnreadCount()
        {
            try
            {
                var userId = GetUserId();
                var count = await _notificationService.GetUnreadCountAsync(userId);
                return Ok(count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving unread count");
                return StatusCode(500, "An error occurred while retrieving unread count");
            }
        }

        [HttpPost("{notificationId}/read")]
        public async Task<ActionResult> MarkAsRead(Guid notificationId)
        {
            try
            {
                var userId = GetUserId();
                await _notificationService.MarkAsReadAsync(userId, notificationId);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification as read");
                return StatusCode(500, "An error occurred while marking notification as read");
            }
        }

        [HttpPost("mark-all-read")]
        public async Task<ActionResult> MarkAllAsRead()
        {
            try
            {
                var userId = GetUserId();
                await _notificationService.MarkAllAsReadAsync(userId);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read");
                return StatusCode(500, "An error occurred while marking all notifications as read");
            }
        }

        [HttpPost("send")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult> SendNotification([FromBody] CreateNotificationDto dto)
        {
            try
            {
                await _notificationService.CreateNotificationAsync(dto.Message, dto.UserIds);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification");
                return StatusCode(500, "An error occurred while sending notification");
            }
        }

        [HttpDelete("{notificationId}")]
        public async Task<ActionResult> DeleteNotification(Guid notificationId)
        {
            try
            {
                var userId = GetUserId();
                var result = await _notificationService.DeleteNotificationAsync(userId, notificationId);

                if (!result)
                    return NotFound("Notification not found");

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting notification");
                return StatusCode(500, "An error occurred while deleting the notification");
            }
        }

        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? throw new UnauthorizedAccessException("User ID not found");
            return Guid.Parse(userIdClaim);
        }
    }
}
