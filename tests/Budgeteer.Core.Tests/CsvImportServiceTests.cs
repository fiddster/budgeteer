using Budgeteer.Core.Data;
using Budgeteer.Core.Domain;
using Budgeteer.Core.Import;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Budgeteer.Core.Tests;

public class CsvImportServiceTests : IAsyncLifetime
{
    private BudgeteerDbContext _db = null!;
    private ICsvImportService _sut = null!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<BudgeteerDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;
        _db = new BudgeteerDbContext(options);
        await _db.Database.OpenConnectionAsync();
        await _db.Database.EnsureCreatedAsync();
        _sut = new CsvImportService(_db);
    }

    public async Task DisposeAsync() => await _db.DisposeAsync();

    private static ColumnMapping FullMapping() => new()
    {
        DateColumn = "Date",
        DescriptionColumn = "Description",
        AmountColumn = "Amount",
        BalanceColumn = "Balance",
        ReferenceColumn = "Reference"
    };

    // ── ParseHeaders ─────────────────────────────────────────────────────────

    [Fact]
    public void ParseHeaders_ReturnsColumnNamesFromFirstRow()
    {
        var csv = "Date,Description,Amount,Balance\n2024-01-01,Coffee,-3.50,100.00";

        var headers = _sut.ParseHeaders(csv);

        headers.Should().Equal("Date", "Description", "Amount", "Balance");
    }

    // ── ParseTransactions ─────────────────────────────────────────────────────

    [Fact]
    public void ParseTransactions_ParsesRowsWithCompleteMapping()
    {
        var csv = "Date,Description,Amount,Balance,Reference\n" +
                  "2024-01-15,Supermarket,-52.30,1200.00,REF001";

        var result = _sut.ParseTransactions(csv, FullMapping(), accountId: 1);

        result.Errors.Should().BeEmpty();
        result.Transactions.Should().HaveCount(1);
        var tx = result.Transactions[0];
        tx.Date.Should().Be(new DateOnly(2024, 1, 15));
        tx.Description.Should().Be("Supermarket");
        tx.Amount.Should().Be(-52.30m);
        tx.Balance.Should().Be(1200.00m);
        tx.Reference.Should().Be("REF001");
        tx.AccountId.Should().Be(1);
    }

    [Fact]
    public void ParseTransactions_MissingOptionalColumns_ProducesNulls()
    {
        var csv = "Date,Description,Amount\n2024-02-01,Rent,-900.00";
        var mapping = new ColumnMapping
        {
            DateColumn = "Date",
            DescriptionColumn = "Description",
            AmountColumn = "Amount"
            // Balance and Reference not mapped
        };

        var result = _sut.ParseTransactions(csv, mapping, accountId: 1);

        result.Errors.Should().BeEmpty();
        var tx = result.Transactions.Single();
        tx.Balance.Should().BeNull();
        tx.Reference.Should().BeNull();
    }

    [Fact]
    public void ParseTransactions_MalformedDate_AppearsInErrorList()
    {
        var csv = "Date,Description,Amount\n" +
                  "2024-01-01,Coffee,-3.50\n" +
                  "not-a-date,Broken row,-10.00\n" +
                  "2024-01-03,Lunch,-12.00";

        var result = _sut.ParseTransactions(csv, FullMapping(), accountId: 1);

        result.Transactions.Should().HaveCount(2);
        result.Errors.Should().HaveCount(1);
        var error = result.Errors.Single();
        error.RowNumber.Should().Be(3); // row 1 = header, row 2 = good, row 3 = bad
        error.RawLine.Should().Contain("not-a-date");
        error.Message.Should().Contain("not-a-date");
    }

    // ── DetectDuplicates ──────────────────────────────────────────────────────

    [Fact]
    public void DetectDuplicates_FlagsCandidatesMatchingExistingTransactions()
    {
        var candidates = new[]
        {
            new ParsedTransaction(1, new DateOnly(2024, 1, 1), "Coffee", -3.50m, null, null),
            new ParsedTransaction(1, new DateOnly(2024, 1, 2), "Rent",  -900m,  null, null)
        };
        var existing = new[]
        {
            new Transaction { AccountId = 1, Date = new DateOnly(2024, 1, 1), Description = "Coffee", Amount = -3.50m }
        };

        var duplicates = _sut.DetectDuplicates(candidates, existing);

        duplicates.Should().HaveCount(1);
        duplicates[0].Description.Should().Be("Coffee");
    }

    [Fact]
    public void DetectDuplicates_DoesNotFlagNonMatching()
    {
        var candidates = new[]
        {
            new ParsedTransaction(1, new DateOnly(2024, 1, 5), "Gym", -50m, null, null)
        };
        var existing = new[]
        {
            new Transaction { AccountId = 1, Date = new DateOnly(2024, 1, 1), Description = "Coffee", Amount = -3.50m }
        };

        var duplicates = _sut.DetectDuplicates(candidates, existing);

        duplicates.Should().BeEmpty();
    }

    [Fact]
    public void DetectDuplicates_DifferentAccountNotFlagged()
    {
        var candidates = new[]
        {
            new ParsedTransaction(AccountId: 2, new DateOnly(2024, 1, 1), "Coffee", -3.50m, null, null)
        };
        var existing = new[]
        {
            new Transaction { AccountId = 1, Date = new DateOnly(2024, 1, 1), Description = "Coffee", Amount = -3.50m }
        };

        var duplicates = _sut.DetectDuplicates(candidates, existing);

        duplicates.Should().BeEmpty();
    }

    // ── CommitAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task CommitAsync_PersistsAcceptedTransactionsAsUncategorized()
    {
        var account = new Domain.Account { Name = "Bank", Type = Domain.AccountType.Checking };
        _db.Accounts.Add(account);
        await _db.SaveChangesAsync();

        var accepted = new[]
        {
            new ParsedTransaction(account.Id, new DateOnly(2024, 3, 1), "Supermarket", -42m, 800m, null),
            new ParsedTransaction(account.Id, new DateOnly(2024, 3, 2), "Salary",      2000m, null, "REF99")
        };

        var mapping = FullMapping();
        mapping.AccountId = account.Id;
        await _sut.CommitAsync(account.Id, accepted, mapping);

        var saved = _db.Transactions.Where(t => t.AccountId == account.Id).ToList();
        saved.Should().HaveCount(2);
        saved.Should().AllSatisfy(t =>
        {
            t.CategoryId.Should().BeNull();
            t.IsTransfer.Should().BeFalse();
        });
    }

    [Fact]
    public async Task CommitAsync_SavesColumnMappingToAccount()
    {
        var account = new Domain.Account { Name = "Bank", Type = Domain.AccountType.Checking };
        _db.Accounts.Add(account);
        await _db.SaveChangesAsync();

        var mapping = new ColumnMapping
        {
            AccountId = account.Id,
            DateColumn = "Txn Date",
            DescriptionColumn = "Narration",
            AmountColumn = "Amount"
        };

        await _sut.CommitAsync(account.Id, [], mapping);

        var saved = await _db.ColumnMappings.FindAsync(account.Id);
        saved.Should().NotBeNull();
        saved!.DateColumn.Should().Be("Txn Date");
        saved.DescriptionColumn.Should().Be("Narration");
    }
}
