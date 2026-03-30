//using Microsoft.AspNetCore.Components.Authorization;
//using Microsoft.Extensions.Logging;
//using System.IdentityModel.Tokens.Jwt;
//using System.Security.Claims;

//namespace HomeBudgetClient.Services
//{
//    public class CustomAuthStateProvider : AuthenticationStateProvider
//    {
//        private readonly ITokenStorage _tokenStorage;
//        private readonly ILogger<CustomAuthStateProvider> _logger;
//        private ClaimsPrincipal _currentUser = new(new ClaimsIdentity());

//        public CustomAuthStateProvider(ITokenStorage tokenStorage, ILogger<CustomAuthStateProvider> logger)
//        {
//            _tokenStorage = tokenStorage;
//            _logger = logger;
//        }

//        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
//        {
//            var token = await _tokenStorage.GetTokenAsync();
//            if (string.IsNullOrEmpty(token))
//            {
//                _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
//                return new AuthenticationState(_currentUser);
//            }

//            try
//            {
//                var handler = new JwtSecurityTokenHandler();
//                var jwtToken = handler.ReadJwtToken(token);
//                var identity = new ClaimsIdentity(jwtToken.Claims, "jwt");
//                _currentUser = new ClaimsPrincipal(identity);
//                return new AuthenticationState(_currentUser);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error reading JWT token");
//                _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
//                return new AuthenticationState(_currentUser);
//            }
//        }

//        public void NotifyUserAuthentication(string userName)
//        {
//            var identity = new ClaimsIdentity(new[]
//            {
//            new Claim(ClaimTypes.Name, userName),
//            new Claim(ClaimTypes.NameIdentifier, userName) // we might need to extract actual user id from token
//        }, "jwt");
//            var user = new ClaimsPrincipal(identity);
//            _currentUser = user;
//            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
//        }

//        public void NotifyUserLogout()
//        {
//            _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
//            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_currentUser)));
//        }
//    }
//}
