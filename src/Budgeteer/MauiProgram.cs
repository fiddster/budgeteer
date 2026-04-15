using Budgeteer.Accounts;
using Budgeteer.Budgets;
using Budgeteer.Core.Data;
using Budgeteer.Core.Import;
using Budgeteer.Core.Services;
using Budgeteer.Import;
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
		builder.Services.AddTransient<ICsvImportService, CsvImportService>();
		builder.Services.AddTransient<IBudgetService, BudgetService>();

		builder.Services.AddSingleton<ImportSession>();
		builder.Services.AddTransient<AccountsPage>();
		builder.Services.AddTransient<ImportColumnMappingPage>();
		builder.Services.AddTransient<ImportPreviewPage>();
		builder.Services.AddTransient<BudgetsPage>();
		builder.Services.AddTransient<EditBudgetPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

		var app = builder.Build();

		using var scope = app.Services.CreateScope();

#if DEBUG
		// Uncomment to wipe and recreate the database on every debug launch (useful after schema changes).
		var db = scope.ServiceProvider.GetRequiredService<BudgeteerDbContext>();
		db.Database.EnsureDeleted();
#endif

		var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
		initializer.EnsureCreatedAsync().GetAwaiter().GetResult();

		return app;
	}
}
