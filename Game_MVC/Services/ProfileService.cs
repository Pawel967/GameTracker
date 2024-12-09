using Game_MVC.Models.Profile;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace Game_MVC.Services
{
    public class ProfileService : IProfileService
    {
        private readonly AuthenticatedHttpClient _httpClient;
        private readonly ILogger<ProfileService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public ProfileService(
            AuthenticatedHttpClient httpClient,
            ILogger<ProfileService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        public async Task<UserProfileViewModel?> GetUserProfileAsync(Guid userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/profiles/{userId}");

                // Only return null if we get a 404 Not Found
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null;

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<UserProfileViewModel>(content, _jsonOptions);
                }

                _logger.LogError("Failed to get profile with status code: {StatusCode}", response.StatusCode);
                throw new Exception($"Failed to get profile. Status code: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user profile");
                throw;
            }
        }

        public async Task<bool> ToggleProfilePrivacyAsync()
        {
            try
            {
                var response = await _httpClient.PostAsync("/api/profiles/privacy", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling profile privacy");
                throw;
            }
        }

        public async Task<bool> FollowUserAsync(Guid userId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"/api/profiles/follow/{userId}", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error following user");
                throw;
            }
        }

        public async Task<bool> UnfollowUserAsync(Guid userId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"/api/profiles/follow/{userId}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unfollowing user");
                throw;
            }
        }

        public async Task<IEnumerable<FollowedUserViewModel>> GetFollowersAsync(Guid userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/profiles/{userId}/followers");
                if (!response.IsSuccessStatusCode)
                    return Enumerable.Empty<FollowedUserViewModel>();

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<IEnumerable<FollowedUserViewModel>>(content, _jsonOptions)
                    ?? Enumerable.Empty<FollowedUserViewModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting followers");
                throw;
            }
        }

        public async Task<IEnumerable<FollowedUserViewModel>> GetFollowingAsync(Guid userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/profiles/{userId}/following");
                if (!response.IsSuccessStatusCode)
                    return Enumerable.Empty<FollowedUserViewModel>();

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<IEnumerable<FollowedUserViewModel>>(content, _jsonOptions)
                    ?? Enumerable.Empty<FollowedUserViewModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting following users");
                throw;
            }
        }

        public async Task<IEnumerable<ProfileSearchViewModel>> SearchProfilesAsync(string searchTerm, int limit = 10)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/profiles/search?searchTerm={Uri.EscapeDataString(searchTerm)}&limit={limit}");
                if (!response.IsSuccessStatusCode)
                    return Enumerable.Empty<ProfileSearchViewModel>();

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<IEnumerable<ProfileSearchViewModel>>(content, _jsonOptions)
                    ?? Enumerable.Empty<ProfileSearchViewModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching profiles");
                throw;
            }
        }
    }
}
