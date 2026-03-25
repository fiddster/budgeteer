using Budgeteer.Core.Data;
using Budgeteer.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Budgeteer;

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
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		var dbPath = Path.Combine(FileSystem.AppDataDirectory, "budgeteer.db");
		builder.Services.AddDbContext<BudgeteerDbContext>(options =>
			options.UseSqlite($"DataSource={dbPath}"));

		builder.Services.AddTransient<DatabaseInitializer>();
		builder.Services.AddTransient<ICategoryService, CategoryService>();
		builder.Services.AddTransient<IAccountService, AccountService>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		var app = builder.Build();

		using var scope = app.Services.CreateScope();
		var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
		initializer.EnsureCreatedAsync().GetAwaiter().GetResult();

		return app;
	}
}
