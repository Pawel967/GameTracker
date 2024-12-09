using Game_MVC.Services;
using Microsoft.AspNetCore.Mvc;

namespace Game_MVC
{
    public class NotificationCounterViewComponent : ViewComponent
    {
        private readonly INotificationService _notificationService;

        public NotificationCounterViewComponent(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            try
            {
                var count = await _notificationService.GetUnreadCountAsync();
                return View(count);
            }
            catch
            {
                return View(0);
            }
        }
    }
}
