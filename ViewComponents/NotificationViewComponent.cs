using Microsoft.AspNetCore.Mvc;
using OfficeNexus.Services;
using System.Security.Claims;

namespace OfficeNexus.ViewComponents
{
    public class NotificationViewComponent : ViewComponent
    {
        private readonly INotificationService _notificationService;

        public NotificationViewComponent(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            // Get current user ID from HttpContext
            var userIdStr = HttpContext.User.FindFirstValue("UserId");
            if (string.IsNullOrEmpty(userIdStr))
            {
                return View(new NotificationViewModel { UnreadCount = 0, RecentNotifications = new List<OfficeNexus.Models.Notification>() });
            }

            var userId = int.Parse(userIdStr);

            // Get unread count and recent notifications
            var unreadCount = await _notificationService.GetUnreadCount(userId);
            var recentNotifications = await _notificationService.GetRecentNotifications(userId, 5);

            var viewModel = new NotificationViewModel
            {
                UnreadCount = unreadCount,
                RecentNotifications = recentNotifications
            };

            return View(viewModel);
        }
    }

    public class NotificationViewModel
    {
        public int UnreadCount { get; set; }
        public List<OfficeNexus.Models.Notification> RecentNotifications { get; set; } = new();
    }
}
