namespace Budgeteer.Core.Domain;

public class Transaction
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public Account Account { get; set; } = null!;

    public DateOnly Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal? Balance { get; set; }
    public string? Reference { get; set; }

    public int? CategoryId { get; set; }
    public Category? Category { get; set; }

    public bool IsTransfer { get; set; }
}
