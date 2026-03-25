namespace Budgeteer.Core.Domain;

public class Budget
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal SpendingLimit { get; set; }
    public BudgetTimespan Timespan { get; set; }
    public bool Rollover { get; set; }
    public bool AlertEnabled { get; set; }
    public int? AlertThresholdPercent { get; set; }

    public ICollection<BudgetCategory> BudgetCategories { get; set; } = [];
}
