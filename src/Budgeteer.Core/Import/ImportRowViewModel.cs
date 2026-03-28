namespace Budgeteer.Core.Import;

public class ImportRowViewModel(ParsedTransaction transaction)
{
    public ParsedTransaction Transaction { get; } = transaction;
    public bool IsIncluded { get; set; } = true;
}
