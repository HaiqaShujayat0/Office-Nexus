using OfficeNexus.Data;
using OfficeNexus.Models;

namespace OfficeNexus.Services
{
    public interface INotificationService
    {
        Task NotifyAdmins(string message, string? link, NotificationType type);
        Task NotifyUser(int userId, string message, string? link, NotificationType type);
        Task MarkAsRead(int notificationId);
        Task<int> GetUnreadCount(int userId);
        Task<List<Notification>> GetRecentNotifications(int userId, int count);
    }
}
