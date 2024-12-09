using System.Net.Http.Headers;

namespace Game_MVC
{
    public class AuthenticatedHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly string _baseUrl;

        public AuthenticatedHttpClient(
            IHttpClientFactory httpClientFactory,
            IHttpContextAccessor httpContextAccessor,
            IConfiguration configuration)
        {
            _httpClient = httpClientFactory.CreateClient();
            _httpContextAccessor = httpContextAccessor;
            _baseUrl = configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5114";
        }

        // Add PatchAsync method
        public async Task<HttpResponseMessage> PatchAsync(string url, HttpContent? content)
        {
            var request = CreateRequest(HttpMethod.Patch, url);
            if (content != null)
            {
                request.Content = content;
            }
            return await _httpClient.SendAsync(request);
        }

        public async Task<HttpResponseMessage> GetAsync(string url)
        {
            var request = CreateRequest(HttpMethod.Get, url);
            return await _httpClient.SendAsync(request);
        }

        public async Task<HttpResponseMessage> PostAsync(string url, HttpContent? content)
        {
            var request = CreateRequest(HttpMethod.Post, url);
            if (content != null)
            {
                request.Content = content;
            }
            return await _httpClient.SendAsync(request);
        }

        public async Task<HttpResponseMessage> PutAsync(string url, HttpContent? content)
        {
            var request = CreateRequest(HttpMethod.Put, url);
            if (content != null)
            {
                request.Content = content;
            }
            return await _httpClient.SendAsync(request);
        }

        public async Task<HttpResponseMessage> DeleteAsync(string url)
        {
            var request = CreateRequest(HttpMethod.Delete, url);
            return await _httpClient.SendAsync(request);
        }

        private HttpRequestMessage CreateRequest(HttpMethod method, string url)
        {
            var request = new HttpRequestMessage(method, new Uri(_baseUrl + url));

            // Get token from cookie instead of session
            var token = _httpContextAccessor.HttpContext?.Request.Cookies["JWTToken"];
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            return request;
        }
    }
}