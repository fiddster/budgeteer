using Budgeteer.Core.Domain;

namespace Budgeteer.Core.Import;

public class ImportWizardViewModel(
    ICsvImportService importService,
    IReadOnlyList<Transaction> existing,
    ColumnMapping? savedMapping = null)
{
    private string? _csvContent;

    public IReadOnlyList<string> Headers { get; private set; } = [];

    public char DetectedDelimiter { get; private set; } = ',';
    public char Delimiter { get; set; } = savedMapping?.Delimiter is { Length: 1 } d ? d[0] : ',';
    public string Encoding { get; set; } = savedMapping?.Encoding ?? "utf-8";

    public string? DateColumn { get; set; } = savedMapping?.DateColumn;
    public string? DescriptionColumn { get; set; } = savedMapping?.DescriptionColumn;
    public string? AmountColumn { get; set; } = savedMapping?.AmountColumn;
    public string? BalanceColumn { get; set; } = savedMapping?.BalanceColumn;
    public string? ReferenceColumn { get; set; } = savedMapping?.ReferenceColumn;

    public IReadOnlyList<ImportRowViewModel> ValidRows { get; private set; } = [];
    public IReadOnlyList<ImportRowViewModel> DuplicateRows { get; private set; } = [];
    public IReadOnlyList<ErrorRowViewModel> ErrorRows { get; private set; } = [];

    public void LoadCsv(string csvContent)
    {
        _csvContent = csvContent;
        DetectedDelimiter = importService.DetectDelimiter(csvContent);
        Delimiter = DetectedDelimiter;
        Headers = importService.ParseHeaders(csvContent, Delimiter);
    }

    public void RefreshHeaders()
    {
        if (_csvContent is not null)
            Headers = importService.ParseHeaders(_csvContent, Delimiter);
    }

    public void Parse(int accountId)
    {
        var mapping = CurrentMapping();
        var result = importService.ParseTransactions(_csvContent!, mapping, accountId);
        var duplicates = importService.DetectDuplicates(result.Transactions, existing);
        var duplicateSet = duplicates.ToHashSet();

        ValidRows = result.Transactions
            .Where(t => !duplicateSet.Contains(t))
            .Select(t => new ImportRowViewModel(t))
            .ToList();

        DuplicateRows = duplicates
            .Select(t => new ImportRowViewModel(t))
            .ToList();

        ErrorRows = result.Errors
            .Select(e => new ErrorRowViewModel(e, accountId))
            .ToList();
    }

    public void SkipError(ErrorRowViewModel row)
    {
        ErrorRows = ErrorRows.Where(r => r != row).ToList();
    }

    public void ApplyCorrection(ErrorRowViewModel row)
    {
        if (!DateOnly.TryParse(row.CorrectedDate, out var date))
            return;
        if (!decimal.TryParse(row.CorrectedAmount, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var amount))
            return;

        var corrected = new ParsedTransaction(
            row.AccountId, date, row.CorrectedDescription, amount, null, null);

        ValidRows = [.. ValidRows, new ImportRowViewModel(corrected)];
        ErrorRows = ErrorRows.Where(r => r != row).ToList();
    }

    public Task CommitAsync(int accountId)
    {
        var accepted = ValidRows.Concat(DuplicateRows)
            .Where(r => r.IsIncluded)
            .Select(r => r.Transaction)
            .ToList();

        return importService.CommitAsync(accountId, accepted, CurrentMapping());
    }

    private ColumnMapping CurrentMapping() => new()
    {
        DateColumn = DateColumn,
        DescriptionColumn = DescriptionColumn,
        AmountColumn = AmountColumn,
        BalanceColumn = BalanceColumn,
        ReferenceColumn = ReferenceColumn,
        Delimiter = Delimiter.ToString(),
        Encoding = Encoding
    };
}
