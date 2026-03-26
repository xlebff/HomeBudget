using HomeBudgetShared.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

public partial class Program
{
    private static void Main(string[] args)
    {
        IdentityModelEventSource.ShowPII = true;

        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

        var jwtSettings = builder.Configuration.GetSection("Jwt");
        var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("my-32-character-ultra-secure-key-1234567890"))
                };
                options.UseSecurityTokenValidators = true;
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        Console.WriteLine($"Authentication failed: {context.Exception.Message}");
                        var raw = context.Request.Headers["Authorization"].ToString();

                        Console.WriteLine($"[{raw}]");
                        Console.WriteLine($"Length: {raw.Length}");

                        foreach (var c in raw)
                        {
                            Console.Write($"{(int)c} ");
                        }
                        Console.WriteLine();

                        return Task.CompletedTask;
                    }
                };
            });

        

        builder.Services.AddAuthorization();

        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddControllers();

        builder.Services.AddOpenApi();

        IdentityModelEventSource.ShowPII = true;

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseAuthentication();
        app.Use(async (context, next) =>
        {
            if (context.Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                Console.WriteLine($"Authorization header: {authHeader}");
            }
            else
            {
                Console.WriteLine("Authorization header missing");
            }
            await next();
        });
        app.UseAuthorization();

        app.MapControllers();
        IdentityModelEventSource.LogCompleteSecurityArtifact = true;
        app.Run();
        IdentityModelEventSource.ShowPII = true;
    }
}