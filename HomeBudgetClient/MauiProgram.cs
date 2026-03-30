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

            Db(builder);

            Api(builder);

            DebugTools(builder);

            var app = builder.Build();

            return app;
        }

        private static void DebugTools(MauiAppBuilder builder)
        {
            builder.Services.AddMauiBlazorWebView();
#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif
        }

        private static void Api(MauiAppBuilder builder)
        {
            builder.Services.AddScoped<TokenStorageService>();

            builder.Services.AddScoped(sp => new HttpClient
            {
                BaseAddress = new Uri("http://localhost:5284/api/")
            });
            builder.Services.AddScoped<ApiClient>();

            builder.Services.AddTransient<AuthService>();
            builder.Services.AddTransient<SyncService>();
        }

        private static void Db(MauiAppBuilder builder)
        {
            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "HomeBudget.db3");
            var connectionString = $"Data Source={dbPath}";

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(connectionString));

            builder.Services.AddSingleton<TransactionsViewModel>();
        }
    }
}
