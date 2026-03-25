namespace Budgeteer.Core.Domain;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<Transaction> Transactions { get; set; } = [];
    public ICollection<CategorizationRule> Rules { get; set; } = [];
    public ICollection<BudgetCategory> BudgetCategories { get; set; } = [];
}
