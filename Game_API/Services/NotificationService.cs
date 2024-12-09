using AutoMapper;
using Game_API.Data;
using Game_API.Dtos.Notification;
using Game_API.Models.Notification;
using Microsoft.EntityFrameworkCore;

namespace Game_API.Services
{
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            AppDbContext context,
            IMapper mapper,
            ILogger<NotificationService> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task CreateFollowNotificationAsync(Guid followerId, Guid followedId)
        {
            var follower = await _context.Users.FindAsync(followerId)
                ?? throw new KeyNotFoundException("Follower not found");

            var notification = new Notification
            {
                UserId = followedId,
                TriggerUserId = followerId,
                Message = $"{follower.Username} started following you",
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            await _context.Notifications.AddAsync(notification);
            await _context.SaveChangesAsync();
        }

        public async Task CreateNotificationAsync(string message, List<Guid>? userIds = null)
        {
            var notifications = new List<Notification>();
            var users = userIds != null
                ? await _context.Users.Where(u => userIds.Contains(u.Id)).ToListAsync()
                : await _context.Users.ToListAsync();

            foreach (var user in users)
            {
                notifications.Add(new Notification
                {
                    UserId = user.Id,
                    Message = message,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                });
            }

            await _context.Notifications.AddRangeAsync(notifications);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<NotificationDto>> GetUserNotificationsAsync(Guid userId, bool includeRead = false)
        {
            var query = _context.Notifications
                .Include(n => n.TriggerUser)
                .Where(n => n.UserId == userId);

            if (!includeRead)
            {
                query = query.Where(n => !n.IsRead);
            }

            var notifications = await query
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return _mapper.Map<IEnumerable<NotificationDto>>(notifications);
        }

        public async Task MarkAsReadAsync(Guid userId, Guid notificationId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync(Guid userId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<int> GetUnreadCountAsync(Guid userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task<bool> DeleteNotificationAsync(Guid userId, Guid notificationId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

            if (notification == null)
                return false;

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
