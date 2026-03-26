namespace HomeBudgetClient.Services
{
    public class TokenStorageService
    {
        private const string AccessTokenKey = "access_token";

        public async Task SaveTokenAsync(string token)
        {
            try
            {
                await SecureStorage.SetAsync(AccessTokenKey, token);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Token saving error: {ex.Message}");
            }
        }

        public async Task<string?> GetTokenAsync()
        {
            try
            {
                return await SecureStorage.GetAsync(AccessTokenKey);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Token getting error: {ex.Message}");
                return null;
            }
        }

        public void RemoveToken()
        {
            try
            {
                SecureStorage.Remove(AccessTokenKey);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Token removing error: {ex.Message}");
            }
        }
    }
}
