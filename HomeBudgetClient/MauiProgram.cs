//using HomeBudgetClient.ViewModels;
//using Microsoft.AspNetCore.Components.Authorization;
using HomeBudgetClient.Services;
using HomeBudgetShared.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HomeBudgetClient
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();

            //builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
            //builder.Services.AddScoped<IAuthService, AuthService>();

            builder.Services.AddScoped<TokenStorageService>();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "HomeBudget.db3");
            var connectionString = $"Data Source={dbPath}";

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(connectionString));

            builder.Services.AddScoped(sp => new HttpClient
            {
                BaseAddress = new Uri("http://localhost:5284/api/")
            });
            builder.Services.AddScoped<ApiClient>();

            //builder.Services.AddSingleton<CurrenciesViewModel>();

            builder.Services.AddBlazorWebViewDeveloperTools();

            return builder.Build();
        }
    }
}
