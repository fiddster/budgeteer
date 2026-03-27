using Budgeteer.Core.Data;
using Budgeteer.Core.Domain;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

namespace Budgeteer.Core.Import;

public class CsvImportService(BudgeteerDbContext db) : ICsvImportService
{
    public IReadOnlyList<string> ParseHeaders(string csvContent)
    {
        using var reader = new StringReader(csvContent);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true
        });
        csv.Read();
        csv.ReadHeader();
        return csv.HeaderRecord ?? [];
    }

    public ParseResult ParseTransactions(string csvContent, ColumnMapping mapping, int accountId)
    {
        var transactions = new List<ParsedTransaction>();
        var errors = new List<ParseError>();

        using var reader = new StringReader(csvContent);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null  // treat missing columns as null rather than throwing
        });

        csv.Read();
        csv.ReadHeader();

        int rowNumber = 1;
        while (csv.Read())
        {
            rowNumber++;
            var rawLine = csv.Parser.RawRecord.TrimEnd('\r', '\n');

            var dateRaw = mapping.DateColumn is not null ? csv.GetField(mapping.DateColumn) : null;
            if (!DateOnly.TryParse(dateRaw, out var date))
            {
                errors.Add(new ParseError(rowNumber, rawLine, $"Could not parse date '{dateRaw}'"));
                continue;
            }

            var description = mapping.DescriptionColumn is not null
                ? csv.GetField(mapping.DescriptionColumn) ?? string.Empty
                : string.Empty;

            var amountRaw = mapping.AmountColumn is not null ? csv.GetField(mapping.AmountColumn) : null;
            if (!decimal.TryParse(amountRaw, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
            {
                errors.Add(new ParseError(rowNumber, rawLine, $"Could not parse amount '{amountRaw}'"));
                continue;
            }

            decimal? balance = null;
            if (mapping.BalanceColumn is not null)
            {
                var balanceRaw = csv.GetField(mapping.BalanceColumn);
                if (decimal.TryParse(balanceRaw, NumberStyles.Any, CultureInfo.InvariantCulture, out var b))
                    balance = b;
            }

            string? reference = mapping.ReferenceColumn is not null
                ? csv.GetField(mapping.ReferenceColumn)
                : null;

            transactions.Add(new ParsedTransaction(accountId, date, description, amount, balance, reference));
        }

        return new ParseResult(transactions, errors);
    }

    public IReadOnlyList<ParsedTransaction> DetectDuplicates(
        IReadOnlyList<ParsedTransaction> candidates,
        IReadOnlyList<Transaction> existing)
    {
        var existingKeys = existing
            .Select(t => (t.AccountId, t.Date, t.Description, t.Amount))
            .ToHashSet();

        return candidates
            .Where(c => existingKeys.Contains((c.AccountId, c.Date, c.Description, c.Amount)))
            .ToList();
    }

    public async Task CommitAsync(int accountId, IReadOnlyList<ParsedTransaction> accepted, ColumnMapping mapping)
    {
        foreach (var p in accepted)
        {
            db.Transactions.Add(new Transaction
            {
                AccountId = p.AccountId,
                Date = p.Date,
                Description = p.Description,
                Amount = p.Amount,
                Balance = p.Balance,
                Reference = p.Reference,
                CategoryId = null,
                IsTransfer = false
            });
        }

        var existingMapping = await db.ColumnMappings.FindAsync(accountId);
        if (existingMapping is null)
        {
            mapping.AccountId = accountId;
            db.ColumnMappings.Add(mapping);
        }
        else
        {
            existingMapping.DateColumn = mapping.DateColumn;
            existingMapping.DescriptionColumn = mapping.DescriptionColumn;
            existingMapping.AmountColumn = mapping.AmountColumn;
            existingMapping.BalanceColumn = mapping.BalanceColumn;
            existingMapping.ReferenceColumn = mapping.ReferenceColumn;
        }

        await db.SaveChangesAsync();
    }
}
