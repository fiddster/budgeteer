namespace Budgeteer.Core.Domain;

public class ColumnMapping
{
    public int AccountId { get; set; }
    public Account Account { get; set; } = null!;

    public string? DateColumn { get; set; }
    public string? DescriptionColumn { get; set; }
    public string? AmountColumn { get; set; }
    public string? BalanceColumn { get; set; }
    public string? ReferenceColumn { get; set; }
}
