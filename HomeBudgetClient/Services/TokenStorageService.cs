using HomeBudgetClient.Resources;

namespace HomeBudgetClient.Services
{
    public class TokenStorageService
    {
        private const string AccessTokenKey = "access_token";
        private const string UserId = "userId";

        public async Task SaveTokenAsync(string token, Guid userId)
        {
            try
            {
                await SecureStorage.SetAsync(AccessTokenKey, token);
                await SecureStorage.SetAsync(UserId, userId.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine(String.Format(
                    Messages.Error_TokenSaving,
                    ex.Message));
            }
        }

        public async Task<string?> GetTokenAsync()
        {
            try
            {
                string? token = await SecureStorage.GetAsync(AccessTokenKey);

                return token;
            }
            catch (Exception ex)
            {
                Console.WriteLine(String.Format(
                    Messages.Error_TokenGetting,
                    ex.Message));
                return null;
            }
        }

        public async Task<Guid?> GetUserIdAsync()
        {
            try
            {
                string? userIdString = await SecureStorage.GetAsync(UserId);

                if (userIdString is null)
                    return null;

                Guid userId = Guid.Parse(userIdString);

                return userId;
            }
            catch (Exception ex)
            {
                Console.WriteLine(String.Format(
                    Messages.Error_UserIdGetting,
                    ex.Message));
                return null;
            }
        }

        public void RemoveToken()
        {
            try
            {
                SecureStorage.Remove(AccessTokenKey);
                SecureStorage.Remove(UserId);
            }
            catch (Exception ex)
            {
                Console.WriteLine(String.Format(
                    Messages.Error_UserDataRemoving,
                    ex.Message));
            }
        }
    }
}
