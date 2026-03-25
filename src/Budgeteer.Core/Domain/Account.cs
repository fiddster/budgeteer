namespace Budgeteer.Core.Domain;

public class Account
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public AccountType Type { get; set; }
    public string? Notes { get; set; }

    public ColumnMapping? ColumnMapping { get; set; }
    public ICollection<Transaction> Transactions { get; set; } = [];
}
