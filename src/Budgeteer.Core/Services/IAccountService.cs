using Budgeteer.Core.Domain;

namespace Budgeteer.Core.Services;

public interface IAccountService
{
    Task<IReadOnlyList<Account>> GetAllAsync();
    Task<Account> AddAsync(string name, AccountType type, string? notes = null);
    Task UpdateAsync(int id, string name, AccountType type, string? notes = null);
    Task DeleteAsync(int id);
    Task SaveColumnMappingAsync(int accountId, ColumnMapping mapping);
    Task<ColumnMapping?> GetColumnMappingAsync(int accountId);
}
