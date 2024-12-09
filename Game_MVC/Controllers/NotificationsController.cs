using Game_MVC.Models.Notification;
using Game_MVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Game_MVC.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(
            INotificationService notificationService,
            ILogger<NotificationsController> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<IActionResult> Index(bool includeRead = false)
        {
            try
            {
                var notifications = await _notificationService.GetMyNotificationsAsync(includeRead);
                ViewBag.IncludeRead = includeRead;
                return View(notifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notifications");
                TempData["Error"] = "Failed to load notifications.";
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead(Guid id)
        {
            try
            {
                await _notificationService.MarkAsReadAsync(id);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification as read");
                TempData["Error"] = "Failed to mark notification as read.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead()
        {
            try
            {
                await _notificationService.MarkAllAsReadAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read");
                TempData["Error"] = "Failed to mark all notifications as read.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                await _notificationService.DeleteNotificationAsync(id);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting notification");
                TempData["Error"] = "Failed to delete notification.";
                return RedirectToAction(nameof(Index));
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Send()
        {
            return View(new CreateNotificationViewModel());
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult> Send(CreateNotificationViewModel model)
        {
            try
            {
                model.UserIds = string.IsNullOrWhiteSpace(model.UserIds) ? null : model.UserIds;

                await _notificationService.SendNotificationAsync(model);
                TempData["Success"] = "Notification sent successfully.";
                return RedirectToAction(nameof(Index), new { includeRead = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification");
                ModelState.AddModelError("", "Failed to send notification.");
                return View(model);
            }
        }
    }
}
