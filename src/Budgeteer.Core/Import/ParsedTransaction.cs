namespace Budgeteer.Core.Import;

public record ParsedTransaction(
    int AccountId,
    DateOnly Date,
    string Description,
    decimal Amount,
    decimal? Balance,
    string? Reference);
