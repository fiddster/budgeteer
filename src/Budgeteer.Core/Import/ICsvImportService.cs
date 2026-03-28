using Budgeteer.Core.Domain;

namespace Budgeteer.Core.Import;

public interface ICsvImportService
{
    char DetectDelimiter(string csvContent);

    IReadOnlyList<string> ParseHeaders(string csvContent, char delimiter = ',');

    ParseResult ParseTransactions(string csvContent, ColumnMapping mapping, int accountId);

    IReadOnlyList<ParsedTransaction> DetectDuplicates(
        IReadOnlyList<ParsedTransaction> candidates,
        IReadOnlyList<Transaction> existing);

    Task CommitAsync(int accountId, IReadOnlyList<ParsedTransaction> accepted, ColumnMapping mapping);
}
