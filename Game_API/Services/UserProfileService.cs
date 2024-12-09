using AutoMapper;
using Game_API.Data;
using Game_API.Dtos.Profile;
using Game_API.Dtos.UserLibrary;
using Game_API.Models.Library;
using Game_API.Models.Profile;
using Microsoft.EntityFrameworkCore;

namespace Game_API.Services
{
    public class UserProfileService : IUserProfileService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<UserProfileService> _logger;
        private readonly INotificationService _notificationService;

        public UserProfileService(
            AppDbContext context,
            IMapper mapper,
            ILogger<UserProfileService> logger,
            INotificationService notificationService)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _notificationService = notificationService;
        }

        public async Task<UserProfileDto> GetUserProfileAsync(Guid userId, Guid? requestingUserId)
        {
            var user = await _context.Users
                .Include(u => u.UserGameLibraries)
                    .ThenInclude(ugl => ugl.Game)
                        .ThenInclude(g => g.GameGenres)
                            .ThenInclude(gg => gg.Genre)
                .Include(u => u.UserGameLibraries)
                    .ThenInclude(ugl => ugl.Game)
                        .ThenInclude(g => g.GameThemes)
                            .ThenInclude(gt => gt.Theme)
                .Include(u => u.FollowedBy)
                .Include(u => u.Following)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
                throw new KeyNotFoundException("User not found");

            if (user.IsProfilePrivate && requestingUserId != userId)
            {
                return new UserProfileDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    IsProfilePrivate = true,
                    FollowersCount = user.FollowedBy.Count,
                    FollowingCount = user.Following.Count,
                    Games = new List<UserGameLibraryDto>(),
                    GameStatusCounts = new Dictionary<GameStatus, int>()
                };
            }

            var profile = _mapper.Map<UserProfileDto>(user);

            if (requestingUserId.HasValue)
            {
                profile.IsFollowedByCurrentUser = await _context.UserFollowings
                    .AnyAsync(uf => uf.FollowerId == requestingUserId && uf.FollowedId == userId);
            }

            return profile;
        }

        public async Task<bool> ToggleProfilePrivacyAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            user.IsProfilePrivate = !user.IsProfilePrivate;

            if (user.IsProfilePrivate)
            {
                var followers = await _context.UserFollowings
                    .Where(uf => uf.FollowedId == userId)
                    .ToListAsync();
                _context.UserFollowings.RemoveRange(followers);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> FollowUserAsync(Guid followerId, Guid followedId)
        {
            if (followerId == followedId)
                return false;

            var followed = await _context.Users.FindAsync(followedId);
            if (followed == null || followed.IsProfilePrivate)
                return false;

            var existingFollow = await _context.UserFollowings
                .FirstOrDefaultAsync(uf => uf.FollowerId == followerId && uf.FollowedId == followedId);

            if (existingFollow != null)
                return false;

            try
            {
                await _notificationService.CreateFollowNotificationAsync(followerId, followedId);

                var following = new UserFollowing
                {
                    FollowerId = followerId,
                    FollowedId = followedId
                };
                _context.UserFollowings.Add(following);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error following user {FollowedId}", followedId);
                throw;
            }
        }

        public async Task<IEnumerable<FollowedUserDto>> GetFollowersAsync(Guid userId)
        {
            return await _context.UserFollowings
                .Include(uf => uf.Follower)
                    .ThenInclude(u => u.UserGameLibraries)
                .Where(uf => uf.FollowedId == userId && !uf.Follower.IsProfilePrivate)
                .Select(uf => _mapper.Map<FollowedUserDto>(uf.Follower))
                .ToListAsync();
        }

        public async Task<IEnumerable<FollowedUserDto>> GetFollowingAsync(Guid userId)
        {
            return await _context.UserFollowings
                .Include(uf => uf.Followed)
                    .ThenInclude(u => u.UserGameLibraries)
                .Where(uf => uf.FollowerId == userId && !uf.Followed.IsProfilePrivate)
                .Select(uf => _mapper.Map<FollowedUserDto>(uf.Followed))
                .ToListAsync();
        }

        public async Task<bool> UnfollowUserAsync(Guid followerId, Guid followedId)
        {
            var following = await _context.UserFollowings
                .FirstOrDefaultAsync(uf => uf.FollowerId == followerId && uf.FollowedId == followedId);

            if (following == null)
                return false;

            _context.UserFollowings.Remove(following);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<ProfileSearchDto>> SearchProfilesAsync(
            string searchTerm,
            Guid? requestingUserId = null,
            int limit = 10)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return Enumerable.Empty<ProfileSearchDto>();
            }

            var users = await _context.Users
                .Include(u => u.UserGameLibraries)
                    .ThenInclude(ugl => ugl.Game)
                .Include(u => u.FollowedBy)
                .Where(u => !u.IsProfilePrivate && u.Username.Contains(searchTerm))
                .Take(limit)
                .ToListAsync();

            return users.Select(user => new ProfileSearchDto
            {
                Id = user.Id,
                Username = user.Username,
                GamesCount = user.UserGameLibraries.Count,
                FollowersCount = user.FollowedBy.Count,
                IsFollowedByCurrentUser = requestingUserId.HasValue &&
                    user.FollowedBy.Any(f => f.FollowerId == requestingUserId)
            });
        }
    }
}
