using Game_MVC.Models.Auth;
using System.Text.Json;
using System.Text;
using Game_MVC.Models.Admin;

namespace Game_MVC.Services
{
    public class AuthService : IAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AuthService> _logger;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly IHttpContextAccessor _httpContextAccessor;  // Add this

        public AuthService(
            IHttpClientFactory httpClientFactory,
            ILogger<AuthService> logger,
            IHttpContextAccessor httpContextAccessor)  // Add this
        {
            _httpClient = httpClientFactory.CreateClient("GameApiClient");
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;  // Add this
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }
        public async Task<LoginResponseViewModel> LoginAsync(LoginViewModel model)
        {
            var content = new StringContent(
                JsonSerializer.Serialize(model),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync("/api/auth/login", content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception(error);
            }

            var responseString = await response.Content.ReadAsStringAsync();
            var loginResponse = JsonSerializer.Deserialize<LoginResponseViewModel>(responseString, _jsonOptions);

            if (loginResponse == null)
                throw new Exception("Failed to deserialize login response");

            return loginResponse;
        }

        public async Task<UserViewModel> RegisterAsync(RegisterViewModel model)
        {
            var content = new StringContent(
                JsonSerializer.Serialize(model),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync("/api/auth/register", content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception(error);
            }

            var responseString = await response.Content.ReadAsStringAsync();
            var user = JsonSerializer.Deserialize<UserViewModel>(responseString, _jsonOptions);

            if (user == null)
                throw new Exception("Failed to deserialize user response");

            return user;
        }

        public async Task<UserViewModel> UpdateCurrentUserAsync(UpdateMeViewModel model)
        {
            try
            {
                // Get the JWT token from the cookie
                var token = _httpContextAccessor.HttpContext?.Request.Cookies["JWTToken"];
                if (string.IsNullOrEmpty(token))
                {
                    throw new Exception("Not authenticated");
                }

                // Add the token to the request header
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var content = new StringContent(
                    JsonSerializer.Serialize(model, _jsonOptions),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PutAsync("/api/auth/me", content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("API Error: {StatusCode}, Content: {Content}",
                        response.StatusCode, responseContent);
                    throw new Exception($"Failed to update profile: {responseContent}");
                }

                var user = JsonSerializer.Deserialize<UserViewModel>(responseContent, _jsonOptions);
                if (user == null)
                    throw new Exception("Failed to deserialize user response");

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile");
                throw;
            }
        }
    }
}
