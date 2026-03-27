namespace Budgeteer.Core.Import;

public record ParseResult(
    IReadOnlyList<ParsedTransaction> Transactions,
    IReadOnlyList<ParseError> Errors);
