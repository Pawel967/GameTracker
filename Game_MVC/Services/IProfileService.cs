using Game_MVC.Models.Profile;

namespace Game_MVC.Services
{
    public interface IProfileService
    {
        Task<UserProfileViewModel?> GetUserProfileAsync(Guid userId);
        Task<bool> ToggleProfilePrivacyAsync();
        Task<bool> FollowUserAsync(Guid userId);
        Task<bool> UnfollowUserAsync(Guid userId);
        Task<IEnumerable<FollowedUserViewModel>> GetFollowersAsync(Guid userId);
        Task<IEnumerable<FollowedUserViewModel>> GetFollowingAsync(Guid userId);
        Task<IEnumerable<ProfileSearchViewModel>> SearchProfilesAsync(string searchTerm, int limit = 10);
    }
}
