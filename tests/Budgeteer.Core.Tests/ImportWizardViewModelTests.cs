using Budgeteer.Core.Data;
using Budgeteer.Core.Domain;
using Budgeteer.Core.Import;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Budgeteer.Core.Tests;

public class ImportWizardViewModelTests : IAsyncLifetime
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

    // ── Cycle 1: LoadCsv ─────────────────────────────────────────────────────

    [Fact]
    public void LoadCsv_PopulatesHeaders()
    {
        var csv = "Date,Description,Amount\n2024-01-01,Coffee,-3.50";
        var vm = new ImportWizardViewModel(_sut, []);

        vm.LoadCsv(csv);

        vm.Headers.Should().Equal("Date", "Description", "Amount");
    }

    // ── Cycle 2: Saved mapping pre-fills columns ─────────────────────────────

    [Fact]
    public void Constructor_SavedMapping_PreFillsColumnProperties()
    {
        var saved = new ColumnMapping
        {
            DateColumn = "Txn Date",
            DescriptionColumn = "Narration",
            AmountColumn = "Amount",
            BalanceColumn = "Balance",
            ReferenceColumn = "Ref"
        };

        var vm = new ImportWizardViewModel(_sut, [], saved);

        vm.DateColumn.Should().Be("Txn Date");
        vm.DescriptionColumn.Should().Be("Narration");
        vm.AmountColumn.Should().Be("Amount");
        vm.BalanceColumn.Should().Be("Balance");
        vm.ReferenceColumn.Should().Be("Ref");
    }

    // ── Cycle 3: Parse populates ValidRows ───────────────────────────────────

    [Fact]
    public void Parse_ValidCsv_PopulatesValidRows()
    {
        var csv = "Date,Description,Amount\n2024-01-15,Supermarket,-52.30\n2024-01-16,Salary,2000.00";
        var vm = new ImportWizardViewModel(_sut, []);
        vm.LoadCsv(csv);
        vm.DateColumn = "Date";
        vm.DescriptionColumn = "Description";
        vm.AmountColumn = "Amount";

        vm.Parse(accountId: 1);

        vm.ValidRows.Should().HaveCount(2);
        vm.ValidRows[0].Transaction.Description.Should().Be("Supermarket");
        vm.ValidRows[1].Transaction.Description.Should().Be("Salary");
        vm.ValidRows.Should().AllSatisfy(r => r.IsIncluded.Should().BeTrue());
    }

    // ── Cycle 4: Parse flags duplicates ──────────────────────────────────────

    [Fact]
    public void Parse_DuplicateRows_PopulatesDuplicateRows()
    {
        var csv = "Date,Description,Amount\n2024-01-01,Coffee,-3.50\n2024-01-02,Rent,-900.00";
        var existing = new[]
        {
            new Transaction { AccountId = 1, Date = new DateOnly(2024, 1, 1), Description = "Coffee", Amount = -3.50m }
        };
        var vm = new ImportWizardViewModel(_sut, existing);
        vm.LoadCsv(csv);
        vm.DateColumn = "Date"; vm.DescriptionColumn = "Description"; vm.AmountColumn = "Amount";

        vm.Parse(accountId: 1);

        vm.DuplicateRows.Should().HaveCount(1);
        vm.DuplicateRows[0].Transaction.Description.Should().Be("Coffee");
        vm.ValidRows.Should().HaveCount(1);
        vm.ValidRows[0].Transaction.Description.Should().Be("Rent");
    }

    // ── Cycle 5: Parse surfaces error rows ───────────────────────────────────

    [Fact]
    public void Parse_MalformedDate_PopulatesErrorRows()
    {
        var csv = "Date,Description,Amount\n2024-01-01,Coffee,-3.50\nnot-a-date,Broken,-10.00";
        var vm = new ImportWizardViewModel(_sut, []);
        vm.LoadCsv(csv);
        vm.DateColumn = "Date"; vm.DescriptionColumn = "Description"; vm.AmountColumn = "Amount";

        vm.Parse(accountId: 1);

        vm.ValidRows.Should().HaveCount(1);
        vm.ErrorRows.Should().HaveCount(1);
        vm.ErrorRows[0].Error.RawLine.Should().Contain("not-a-date");
    }

    // ── Cycle 6: IsIncluded=false excludes row from commit ───────────────────

    [Fact]
    public async Task CommitAsync_ExcludedRow_NotPersisted()
    {
        var account = new Domain.Account { Name = "Bank", Type = Domain.AccountType.Checking };
        _db.Accounts.Add(account);
        await _db.SaveChangesAsync();

        var csv = "Date,Description,Amount\n2024-01-01,Coffee,-3.50\n2024-01-02,Rent,-900.00";
        var vm = new ImportWizardViewModel(_sut, []);
        vm.LoadCsv(csv);
        vm.DateColumn = "Date"; vm.DescriptionColumn = "Description"; vm.AmountColumn = "Amount";
        vm.Parse(accountId: account.Id);

        vm.ValidRows[0].IsIncluded = false; // exclude Coffee

        await vm.CommitAsync(account.Id);

        var saved = _db.Transactions.Where(t => t.AccountId == account.Id).ToList();
        saved.Should().HaveCount(1);
        saved[0].Description.Should().Be("Rent");
    }

    // ── Cycle 7: CommitAsync persists accepted rows ───────────────────────────

    [Fact]
    public async Task CommitAsync_AcceptedRows_PersistedAsUncategorized()
    {
        var account = new Domain.Account { Name = "Bank", Type = Domain.AccountType.Checking };
        _db.Accounts.Add(account);
        await _db.SaveChangesAsync();

        var csv = "Date,Description,Amount\n2024-03-01,Supermarket,-42.00\n2024-03-02,Salary,2000.00";
        var vm = new ImportWizardViewModel(_sut, []);
        vm.LoadCsv(csv);
        vm.DateColumn = "Date"; vm.DescriptionColumn = "Description"; vm.AmountColumn = "Amount";
        vm.Parse(accountId: account.Id);

        await vm.CommitAsync(account.Id);

        var saved = _db.Transactions.Where(t => t.AccountId == account.Id).ToList();
        saved.Should().HaveCount(2);
        saved.Should().AllSatisfy(t =>
        {
            t.CategoryId.Should().BeNull();
            t.IsTransfer.Should().BeFalse();
        });
    }

    // ── Cycle 8: SkipError removes row from ErrorRows ─────────────────────────

    [Fact]
    public void SkipError_RemovesRowFromErrorRows()
    {
        var csv = "Date,Description,Amount\nnot-a-date,Coffee,-3.50";
        var vm = new ImportWizardViewModel(_sut, []);
        vm.LoadCsv(csv);
        vm.DateColumn = "Date"; vm.DescriptionColumn = "Description"; vm.AmountColumn = "Amount";
        vm.Parse(accountId: 1);

        var errorRow = vm.ErrorRows.Single();
        vm.SkipError(errorRow);

        vm.ErrorRows.Should().BeEmpty();
    }

    // ── Cycle 9: ApplyCorrection moves row to ValidRows ───────────────────────

    [Fact]
    public void ApplyCorrection_ValidDate_MovesRowToValidRows()
    {
        var csv = "Date,Description,Amount\nnot-a-date,Coffee,-3.50";
        var vm = new ImportWizardViewModel(_sut, []);
        vm.LoadCsv(csv);
        vm.DateColumn = "Date"; vm.DescriptionColumn = "Description"; vm.AmountColumn = "Amount";
        vm.Parse(accountId: 1);

        var errorRow = vm.ErrorRows.Single();
        errorRow.CorrectedDate = "2024-01-15";
        errorRow.CorrectedDescription = "Coffee";
        errorRow.CorrectedAmount = "-3.50";

        vm.ApplyCorrection(errorRow);

        vm.ErrorRows.Should().BeEmpty();
        vm.ValidRows.Should().HaveCount(1);
        vm.ValidRows[0].Transaction.Date.Should().Be(new DateOnly(2024, 1, 15));
        vm.ValidRows[0].Transaction.Description.Should().Be("Coffee");
        vm.ValidRows[0].Transaction.Amount.Should().Be(-3.50m);
    }
}
