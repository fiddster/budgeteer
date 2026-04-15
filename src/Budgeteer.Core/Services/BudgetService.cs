using Budgeteer.Core.Data;
using Budgeteer.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace Budgeteer.Core.Services;

public class BudgetService(BudgeteerDbContext db) : IBudgetService
{
    public async Task<IReadOnlyList<Budget>> GetAllAsync() =>
        await db.Budgets
            .Include(b => b.BudgetCategories)
            .ThenInclude(bc => bc.Category)
            .OrderBy(b => b.Name)
            .ToListAsync();

    public async Task<Budget> AddAsync(string name, decimal spendingLimit, BudgetTimespan timespan,
        bool rollover, bool alertEnabled, int? alertThresholdPercent,
        IEnumerable<int> categoryIds)
    {
        var budget = new Budget
        {
            Name = name,
            SpendingLimit = spendingLimit,
            Timespan = timespan,
            Rollover = rollover,
            AlertEnabled = alertEnabled,
            AlertThresholdPercent = alertThresholdPercent
        };
        db.Budgets.Add(budget);
        await db.SaveChangesAsync();

        foreach (var id in categoryIds)
            db.BudgetCategories.Add(new BudgetCategory { BudgetId = budget.Id, CategoryId = id });
        await db.SaveChangesAsync();

        return budget;
    }

    public async Task UpdateAsync(int id, string name, decimal spendingLimit, BudgetTimespan timespan,
        bool rollover, bool alertEnabled, int? alertThresholdPercent,
        IEnumerable<int> categoryIds)
    {
        var budget = await db.Budgets
            .Include(b => b.BudgetCategories)
            .FirstOrDefaultAsync(b => b.Id == id)
            ?? throw new InvalidOperationException($"Budget {id} not found.");

        budget.Name = name;
        budget.SpendingLimit = spendingLimit;
        budget.Timespan = timespan;
        budget.Rollover = rollover;
        budget.AlertEnabled = alertEnabled;
        budget.AlertThresholdPercent = alertThresholdPercent;

        db.BudgetCategories.RemoveRange(budget.BudgetCategories);

        foreach (var categoryId in categoryIds)
            db.BudgetCategories.Add(new BudgetCategory { BudgetId = budget.Id, CategoryId = categoryId });

        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var budget = await db.Budgets.FindAsync(id)
            ?? throw new InvalidOperationException($"Budget {id} not found.");
        db.Budgets.Remove(budget);
        await db.SaveChangesAsync();
    }
}
