using Budgeteer.Core.Domain;

namespace Budgeteer.Core.Services;

public interface ITransactionService
{
    Task<IReadOnlyList<Transaction>> GetAllAsync(
        int? accountId = null,
        int? categoryId = null,
        DateOnly? from = null,
        DateOnly? to = null);

    Task MarkAsTransferAsync(int id, bool isTransfer);
    Task<IReadOnlyList<Transaction>> GetSpendingTransactionsAsync(DateOnly? from = null, DateOnly? to = null);
}
