using Budgeteer.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace Budgeteer.Core.Data;

public class DatabaseInitializer(BudgeteerDbContext db)
{
    private static readonly string[] DefaultCategories =
    [
        "Groceries", "Dining", "Rent/Mortgage", "Utilities", "Transport",
        "Entertainment", "Healthcare", "Shopping", "Subscriptions",
        "Personal Care", "Education", "Travel", "Savings", "Income"
    ];

    public async Task EnsureCreatedAsync()
    {
        await db.Database.EnsureCreatedAsync();

        if (!await db.Categories.AnyAsync())
        {
            db.Categories.AddRange(DefaultCategories.Select(name => new Category { Name = name }));
            await db.SaveChangesAsync();
        }
    }
}
