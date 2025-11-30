using Microsoft.EntityFrameworkCore;
using OfficeNexus.Data;
using OfficeNexus.Models;

namespace OfficeNexus.Services
{
    public class NotificationService : INotificationService
    {
        private readonly OfficeDbContext _context;

        public NotificationService(OfficeDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Sends notification to all admin users
        /// </summary>
        public async Task NotifyAdmins(string message, string? link, NotificationType type)
        {
            // Get all admin users
            var adminUsers = await _context.Users
                .Where(u => u.Role == UserRole.Admin)
                .ToListAsync();

            // Create notification for each admin
            foreach (var admin in adminUsers)
            {
                var notification = new Notification
                {
                    RecipientUserId = admin.Id,
                    Message = message,
                    Link = link,
                    Type = type,
                    IsRead = false,
                    CreatedAt = DateTime.Now
                };

                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Sends notification to a specific user
        /// </summary>
        public async Task NotifyUser(int userId, string message, string? link, NotificationType type)
        {
            var notification = new Notification
            {
                RecipientUserId = userId,
                Message = message,
                Link = link,
                Type = type,
                IsRead = false,
                CreatedAt = DateTime.Now
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Marks a notification as read
        /// </summary>
        public async Task MarkAsRead(int notificationId)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Gets count of unread notifications for a user
        /// </summary>
        public async Task<int> GetUnreadCount(int userId)
        {
            return await _context.Notifications
                .Where(n => n.RecipientUserId == userId && !n.IsRead)
                .CountAsync();
        }

        /// <summary>
        /// Gets recent notifications for a user
        /// </summary>
        public async Task<List<Notification>> GetRecentNotifications(int userId, int count)
        {
            return await _context.Notifications
                .Where(n => n.RecipientUserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(count)
                .ToListAsync();
        }
    }
}
