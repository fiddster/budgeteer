namespace Budgeteer.Core.Domain;

public class CategorizationRule
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public string Keyword { get; set; } = string.Empty;
    public int Precedence { get; set; }
}
