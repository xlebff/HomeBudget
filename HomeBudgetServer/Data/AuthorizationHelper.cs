using HomeBudgetShared.Data;
using HomeBudgetShared.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HomeBudgetServer.Data
{
    public static class AuthorizationHelper
    {
        public static async Task<User?> GetAuthenticatedUserAsync(
            this ControllerBase controller,
            AppDbContext context) 
        {
            var currentUserId = GetCurrentUserId(controller.User);
            if (!currentUserId.HasValue) return null;

            var user = await context.Users.FindAsync(currentUserId.Value);
            if (user == null) return null;

            return user;
        }
        
        private static Guid? GetCurrentUserId(ClaimsPrincipal user)
        {
            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdClaim, out var userId))
                return userId;
            return null;
        }
    }
}
