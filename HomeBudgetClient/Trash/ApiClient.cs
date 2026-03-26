//using Microsoft.Extensions.Logging;
//using System.Net.Http.Headers;
//using System.Net.Http.Json;

//namespace HomeBudgetClient.Services
//{
//    public class ApiClient
//    {
//        private readonly HttpClient _httpClient;
//        private readonly ITokenStorage _tokenStorage;
//        private readonly ILogger<ApiClient> _logger;

//        public ApiClient(HttpClient httpClient, ITokenStorage tokenStorage, ILogger<ApiClient> logger)
//        {
//            _httpClient = httpClient;
//            _tokenStorage = tokenStorage;
//            _logger = logger;
//        }

//        public async Task<T?> GetAsync<T>(string uri)
//        {
//            await AddTokenToHeader();
//            var response = await _httpClient.GetAsync(uri);
//            if (response.IsSuccessStatusCode)
//                return await response.Content.ReadFromJsonAsync<T>();
//            _logger.LogWarning($"GET {uri} failed: {response.StatusCode}");
//            return default;
//        }

//        public async Task<TResponse?> PostAsync<TRequest, TResponse>(string uri, TRequest data)
//        {
//            await AddTokenToHeader();
//            var response = await _httpClient.PostAsJsonAsync(uri, data);
//            if (response.IsSuccessStatusCode)
//                return await response.Content.ReadFromJsonAsync<TResponse>();
//            var error = await response.Content.ReadAsStringAsync();
//            _logger.LogWarning($"POST {uri} failed: {response.StatusCode} - {error}");
//            throw new HttpRequestException($"Request failed: {response.StatusCode}", null, response.StatusCode);
//        }

//        public async Task<T?> PutAsync<TRequest, T>(string uri, TRequest data)
//        {
//            await AddTokenToHeader();
//            var response = await _httpClient.PutAsJsonAsync(uri, data);
//            if (response.IsSuccessStatusCode)
//                return await response.Content.ReadFromJsonAsync<T>();
//            _logger.LogWarning($"PUT {uri} failed: {response.StatusCode}");
//            return default;
//        }

//        public async Task<bool> DeleteAsync(string uri)
//        {
//            await AddTokenToHeader();
//            var response = await _httpClient.DeleteAsync(uri);
//            return response.IsSuccessStatusCode;
//        }

//        private async Task AddTokenToHeader()
//        {
//            var token = await _tokenStorage.GetTokenAsync();
//            if (!string.IsNullOrEmpty(token))
//                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
//        }
//    }
//}