namespace HomeBudgetShared.Contracts
{
    public class RegisterRequest
    {
        public string Login { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public Guid CurrencyId { get; set; } = Guid.Empty;
    }

    public class LoginRequest
    {
        public string Login { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class AuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public string Login { get; set; } = string.Empty;
        public Guid UserId { get; set; }
    }

    public class CurrencySetResponse
    {
        public Guid UserId { get; set; }
        public Guid CurrencyId { get; set; }
        public string Code { get; set; } = string.Empty;
    }
}
