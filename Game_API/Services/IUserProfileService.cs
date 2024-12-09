using Game_API.Dtos.Profile;

namespace Game_API.Services
{
    public interface IUserProfileService
    {
        Task<UserProfileDto> GetUserProfileAsync(Guid userId, Guid? requestingUserId);
        Task<bool> ToggleProfilePrivacyAsync(Guid userId);
        Task<bool> FollowUserAsync(Guid followerId, Guid followedId);
        Task<bool> UnfollowUserAsync(Guid followerId, Guid followedId);
        Task<IEnumerable<FollowedUserDto>> GetFollowersAsync(Guid userId);
        Task<IEnumerable<FollowedUserDto>> GetFollowingAsync(Guid userId);
        Task<IEnumerable<ProfileSearchDto>> SearchProfilesAsync(string searchTerm, Guid? requestingUserId, int limit);
    }
}
