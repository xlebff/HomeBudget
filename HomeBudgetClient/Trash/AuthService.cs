//using HomeBudgetShared.Contracts;
//using Microsoft.AspNetCore.Components.Authorization;

//namespace HomeBudgetClient.Services
//{
//    public interface IAuthService
//    {
//        Task<AuthResponse?> LoginAsync(LoginRequest request);
//        Task<AuthResponse?> RegisterAsync(RegisterRequest request);
//        Task LogoutAsync();
//    }

//    public class AuthService : IAuthService
//    {
//        private readonly ApiClient _apiClient;
//        private readonly ITokenStorage _tokenStorage;
//        private readonly AuthenticationStateProvider _authStateProvider;    

//        public AuthService(ApiClient apiClient, ITokenStorage tokenStorage, AuthenticationStateProvider authStateProvider)
//        {
//            _apiClient = apiClient;
//            _tokenStorage = tokenStorage;
//            _authStateProvider = authStateProvider;
//        }

//        public async Task<AuthResponse?> LoginAsync(LoginRequest request)
//        {
//            var response = await _apiClient.PostAsync<LoginRequest, AuthResponse>("auth/login", request);
//            if (response != null)
//            {
//                await _tokenStorage.SetTokenAsync(response.Token);
//                ((CustomAuthStateProvider)_authStateProvider).NotifyUserAuthentication(response.Login);
//                return response;
//            }
//            return null;
//        }

//        public async Task<AuthResponse?> RegisterAsync(RegisterRequest request)
//        {
//            var response = await _apiClient.PostAsync<RegisterRequest, AuthResponse>("auth/register", request);
//            if (response != null)
//            {
//                await _tokenStorage.SetTokenAsync(response.Token);
//                ((CustomAuthStateProvider)_authStateProvider).NotifyUserAuthentication(response.Login);
//                return response;
//            }
//            return null;
//        }

//        public async Task LogoutAsync()
//        {
//            await _tokenStorage.RemoveTokenAsync();
//            ((CustomAuthStateProvider)_authStateProvider).NotifyUserLogout();
//        }
//    }
//}
