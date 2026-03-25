namespace Budgeteer.Core.Domain;

public class BudgetCategory
{
    public int BudgetId { get; set; }
    public Budget Budget { get; set; } = null!;

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
}
