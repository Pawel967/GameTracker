using Game_MVC.Models.Admin;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Game_MVC.Services
{
    public class AdminService : IAdminService
    {
        private readonly AuthenticatedHttpClient _httpClient;
        private readonly ILogger<AdminService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public AdminService(
            AuthenticatedHttpClient httpClient,
            ILogger<AdminService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        public async Task<IEnumerable<UserListViewModel>> GetAllUsersAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/admin/users");
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to get users with status code: {StatusCode}", response.StatusCode);
                    return Enumerable.Empty<UserListViewModel>();
                }

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<IEnumerable<UserListViewModel>>(content, _jsonOptions)
                    ?? Enumerable.Empty<UserListViewModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting all users");
                throw;
            }
        }

        public async Task<UserListViewModel?> GetUserAsync(Guid userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"/api/admin/users/{userId}");
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to get user with status code: {StatusCode}", response.StatusCode);
                    return null;
                }

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<UserListViewModel>(content, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting user {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> DeleteUserAsync(Guid userId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"/api/admin/users/{userId}");
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to delete user with status code: {StatusCode}", response.StatusCode);
                }
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting user {UserId}", userId);
                throw;
            }
        }

        public async Task<bool> AssignRoleAsync(Guid userId, string roleName)
        {
            try
            {
                var content = JsonSerializer.Serialize(roleName, _jsonOptions);
                var response = await _httpClient.PostAsync(
                    $"/api/admin/users/{userId}/role",
                    new StringContent(content, System.Text.Encoding.UTF8, "application/json"));

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to assign role with status code: {StatusCode}", response.StatusCode);
                }
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while assigning role for user {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<string>> GetAllRolesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("/api/admin/roles");
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to get roles with status code: {StatusCode}", response.StatusCode);
                    return Enumerable.Empty<string>();
                }

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<IEnumerable<string>>(content, _jsonOptions)
                    ?? Enumerable.Empty<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting all roles");
                throw;
            }
        }

        public async Task<bool> UpdateUserAsync(Guid userId, UpdateUserViewModel model)
        {
            try
            {
                var content = JsonSerializer.Serialize(model, _jsonOptions);
                var response = await _httpClient.PutAsync(
                    $"/api/admin/users/{userId}",
                    new StringContent(content, System.Text.Encoding.UTF8, "application/json"));

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to update user. Status: {StatusCode}, Error: {Error}",
                        response.StatusCode, errorContent);
                }
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating user {UserId}", userId);
                throw;
            }
        }
    }
}
