namespace Budgeteer.Core.Import;

public class ErrorRowViewModel(ParseError error, int accountId)
{
    public ParseError Error { get; } = error;
    public int AccountId { get; } = accountId;
    public string CorrectedDate { get; set; } = string.Empty;
    public string CorrectedDescription { get; set; } = string.Empty;
    public string CorrectedAmount { get; set; } = string.Empty;
}
