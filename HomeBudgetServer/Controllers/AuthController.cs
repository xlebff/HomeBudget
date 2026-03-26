using HomeBudgetServer.Data;
using HomeBudgetShared.Contracts;
using HomeBudgetShared.Data;
using HomeBudgetShared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace HomeBudgetServer.Controllers
{
    [ApiController]
    [Route("api/auth")]
    [AllowAnonymous]
    public class AuthController(AppDbContext context,
        IConfiguration configuration) : ControllerBase
    {
        private readonly AppDbContext _context = context;
        private readonly IConfiguration _configuration = configuration;

        // POST: api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register(
            [FromBody] RegisterRequest request)
        {
            var existingUser = await _context.GetFilteredAsync<User>(
                u => u.Login == request.Login);
            if (existingUser.Any())
                return BadRequest("User with this email already exists.");

            var passwordHash = BCrypt.Net.BCrypt.HashPassword(
                request.Password);
            var currency = (await _context.GetFilteredAsync<Currency>(
                c => c.Code == "RUB")).FirstOrDefault();

            if (currency == null)
                return BadRequest($"Unable to register {request.Login}");

            var user = new User
            {
                Login = request.Login,
                PasswordHash = passwordHash,
                CurrencyId = currency.Id
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(user);

            return Ok(new AuthResponse
            {
                Token = token,
                Login = user.Login,
                UserId = user.Id
            });
        }

        // POST: api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login(
            [FromBody] LoginRequest request)
        {
            var user = (await _context.GetFilteredAsync<User>
                (u => u.Login == request.Login)).
                FirstOrDefault();
            if (user == null)
                return Unauthorized("Invalid email or password.");

            bool isValidPassword = BCrypt.Net.BCrypt.Verify(
                request.Password, user.PasswordHash);
            if (!isValidPassword)
                return Unauthorized("Invalid email or password.");

            var token = GenerateJwtToken(user);

            return Ok(new AuthResponse
            {
                Token = token,
                Login = user.Login,
                UserId = user.Id
            });
        }

        // PUT: api/auth/currency

        [HttpPut("currency/{id}")]
        public async Task<IActionResult> EditCurrency([FromRoute] Guid? id)
        {
            var user = await this.GetAuthenticatedUserAsync(_context);
            if (user == null) return Unauthorized();

            if (!id.HasValue) return BadRequest("Invalid currency id.");

            var currency = await _context.Currencies.FindAsync(id);

            if (currency is null) return BadRequest(
                $"No currency with id \"{id}\" found.");

            user.CurrencyId = (Guid)id;

            await _context.SaveChangesAsync();

            return Ok(new CurrencySetResponse
            {
                UserId = user.Id,
                CurrencyId = currency.Id,
                Code = currency.Code
            });
        }

        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);
            var issuer = jwtSettings["Issuer"];
            var audience = jwtSettings["Audience"];
            // var expireMinutes = double.Parse(jwtSettings["ExpireMinutes"]!);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(JwtRegisteredClaimNames.Email, user.Login)
            };

            var credentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddYears(100),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
