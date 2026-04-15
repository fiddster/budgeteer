using Budgeteer.Core.Domain;

namespace Budgeteer.Core.Services;

public interface IBudgetService
{
    Task<IReadOnlyList<Budget>> GetAllAsync();
    Task<Budget> AddAsync(string name, decimal spendingLimit, BudgetTimespan timespan,
        bool rollover, bool alertEnabled, int? alertThresholdPercent,
        IEnumerable<int> categoryIds);
    Task UpdateAsync(int id, string name, decimal spendingLimit, BudgetTimespan timespan,
        bool rollover, bool alertEnabled, int? alertThresholdPercent,
        IEnumerable<int> categoryIds);
    Task DeleteAsync(int id);
}
