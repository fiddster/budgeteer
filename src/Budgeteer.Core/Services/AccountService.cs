using Budgeteer.Core.Data;
using Budgeteer.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace Budgeteer.Core.Services;

public class AccountService(BudgeteerDbContext db) : IAccountService
{
    public async Task<IReadOnlyList<Account>> GetAllAsync() =>
        await db.Accounts.Include(a => a.ColumnMapping).OrderBy(a => a.Name).ToListAsync();

    public async Task<Account> AddAsync(string name, AccountType type, string? notes = null)
    {
        var account = new Account { Name = name, Type = type, Notes = notes };
        db.Accounts.Add(account);
        await db.SaveChangesAsync();
        return account;
    }

    public async Task UpdateAsync(int id, string name, AccountType type, string? notes = null)
    {
        var account = await db.Accounts.FindAsync(id)
            ?? throw new InvalidOperationException($"Account {id} not found.");
        account.Name = name;
        account.Type = type;
        account.Notes = notes;
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var account = await db.Accounts.FindAsync(id)
            ?? throw new InvalidOperationException($"Account {id} not found.");
        db.Accounts.Remove(account);
        await db.SaveChangesAsync();
    }

    public async Task SaveColumnMappingAsync(int accountId, ColumnMapping mapping)
    {
        var existing = await db.ColumnMappings.FindAsync(accountId);
        if (existing is null)
        {
            mapping.AccountId = accountId;
            db.ColumnMappings.Add(mapping);
        }
        else
        {
            existing.DateColumn = mapping.DateColumn;
            existing.DescriptionColumn = mapping.DescriptionColumn;
            existing.AmountColumn = mapping.AmountColumn;
            existing.BalanceColumn = mapping.BalanceColumn;
            existing.ReferenceColumn = mapping.ReferenceColumn;
        }
        await db.SaveChangesAsync();
    }

    public async Task<ColumnMapping?> GetColumnMappingAsync(int accountId) =>
        await db.ColumnMappings.FindAsync(accountId);
}
