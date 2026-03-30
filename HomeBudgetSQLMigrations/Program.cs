using HomeBudgetShared.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

// Укажите любой путь (например, в текущей папке)
var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "temp.db");
var connectionString = $"Data Source={dbPath}";

services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString));

// var serviceProvider = services.BuildServiceProvider();

// Эта строка нужна только для того, чтобы EF Tools могли разрешить DbContext
// var context = serviceProvider.GetRequiredService<AppDbContext>();

Console.WriteLine("Migrations project is ready.");