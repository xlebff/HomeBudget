using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace HomeBudgetClient.Services
{
    internal class ApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly TokenStorageService _tokenStorage;

        public ApiClient(HttpClient httpClient, TokenStorageService tokenStorage)
        {
            _httpClient = httpClient;
            _tokenStorage = tokenStorage;
        }

        private async Task AddAuthorizationHeaderAsync()
        {
            var token = await _tokenStorage.GetTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", token);
            }
        }

        public async Task<HttpResponseMessage> GetAsync(string url)
        {
            await AddAuthorizationHeaderAsync();
            return await _httpClient.GetAsync(url);
        }

        public async Task<T?> GetFromJsonAsync<T>(string url)
        {
            await AddAuthorizationHeaderAsync();
            return await _httpClient.GetFromJsonAsync<T>(url);
        }

        public async Task<HttpResponseMessage> PostAsync<T>(string url, T data)
        {
            await AddAuthorizationHeaderAsync();
            return await _httpClient.PostAsJsonAsync(url, data);
        }

        public async Task<TResponse?> PostAsync<TRequest, TResponse>(string url, TRequest data)
        {
            await AddAuthorizationHeaderAsync();
            var response = await _httpClient.PostAsJsonAsync(url, data);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<TResponse>();
            }
            return default;
        }

        public async Task<HttpResponseMessage> PutAsync<T>(string url, T data)
        {
            await AddAuthorizationHeaderAsync();
            return await _httpClient.PutAsJsonAsync(url, data);
        }

        public async Task<HttpResponseMessage> DeleteAsync(string url)
        {
            await AddAuthorizationHeaderAsync();
            return await _httpClient.DeleteAsync(url);
        }

        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
        {
            await AddAuthorizationHeaderAsync();
            return await _httpClient.SendAsync(request);
        }

        public string GetBaseAddress() => _httpClient.BaseAddress?.ToString() ?? "не установлен";
    }
}
