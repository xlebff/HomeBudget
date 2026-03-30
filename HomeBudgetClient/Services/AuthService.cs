using HomeBudgetShared.Data;
using HomeBudgetShared.Models;
using System.Diagnostics;

namespace HomeBudgetClient.Services
{
    internal class AuthService(AppDbContext context)
    {
        private readonly AppDbContext _context = context;

        public async Task AddUserAsync(
            Guid id,
            string login,
            Guid? currencyId = null)
        {
            await _context.EnsureDatabaseCreatedAsync();

            await _context.AddAsync(new User
            {
                Id = id,
                Login = login,
                PasswordHash = string.Empty,
                CurrencyId = currencyId
            });

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex) 
            {
                Debug.WriteLine(ex);
            }
        }
    }
}
