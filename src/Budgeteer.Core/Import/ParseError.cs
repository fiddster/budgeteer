namespace Budgeteer.Core.Import;

public record ParseError(int RowNumber, string RawLine, string Message);
