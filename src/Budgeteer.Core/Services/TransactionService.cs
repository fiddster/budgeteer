using Budgeteer.Core.Data;
using Budgeteer.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace Budgeteer.Core.Services;

public class TransactionService(BudgeteerDbContext db) : ITransactionService
{
    public async Task<IReadOnlyList<Transaction>> GetAllAsync(
        int? accountId = null,
        int? categoryId = null,
        DateOnly? from = null,
        DateOnly? to = null)
    {
        var query = db.Transactions
            .Include(t => t.Account)
            .Include(t => t.Category)
            .AsQueryable();

        if (accountId.HasValue)
            query = query.Where(t => t.AccountId == accountId.Value);

        if (categoryId.HasValue)
            query = query.Where(t => t.CategoryId == categoryId.Value);

        if (from.HasValue)
            query = query.Where(t => t.Date >= from.Value);

        if (to.HasValue)
            query = query.Where(t => t.Date <= to.Value);

        return await query.OrderByDescending(t => t.Date).ToListAsync();
    }

    public async Task MarkAsTransferAsync(int id, bool isTransfer)
    {
        var transaction = await db.Transactions.FindAsync(id)
            ?? throw new InvalidOperationException($"Transaction {id} not found.");
        transaction.IsTransfer = isTransfer;
        await db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<Transaction>> GetSpendingTransactionsAsync(DateOnly? from = null, DateOnly? to = null)
    {
        var query = db.Transactions
            .Include(t => t.Account)
            .Include(t => t.Category)
            .Where(t => !t.IsTransfer)
            .AsQueryable();

        if (from.HasValue)
            query = query.Where(t => t.Date >= from.Value);

        if (to.HasValue)
            query = query.Where(t => t.Date <= to.Value);

        return await query.OrderByDescending(t => t.Date).ToListAsync();
    }
}
