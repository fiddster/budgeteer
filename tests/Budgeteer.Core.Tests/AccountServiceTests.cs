using Budgeteer.Core.Data;
using Budgeteer.Core.Domain;
using Budgeteer.Core.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Budgeteer.Core.Tests;

public class AccountServiceTests : IAsyncLifetime
{
    private BudgeteerDbContext _db = null!;
    private AccountService _sut = null!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<BudgeteerDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;
        _db = new BudgeteerDbContext(options);
        await _db.Database.OpenConnectionAsync();
        await _db.Database.EnsureCreatedAsync();
        _sut = new AccountService(_db);
    }

    public async Task DisposeAsync() => await _db.DisposeAsync();

    [Fact]
    public async Task AddAsync_AddsAccount()
    {
        var account = await _sut.AddAsync("Main Checking", AccountType.Checking, "Primary account");

        account.Id.Should().BeGreaterThan(0);
        account.Name.Should().Be("Main Checking");
        account.Type.Should().Be(AccountType.Checking);
        account.Notes.Should().Be("Primary account");
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllAccounts()
    {
        await _sut.AddAsync("Checking", AccountType.Checking);
        await _sut.AddAsync("Savings", AccountType.Savings);

        var all = await _sut.GetAllAsync();

        all.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesAccount()
    {
        var account = await _sut.AddAsync("Old Name", AccountType.Checking);

        await _sut.UpdateAsync(account.Id, "New Name", AccountType.Savings, "updated notes");

        var all = await _sut.GetAllAsync();
        var updated = all.Single(a => a.Id == account.Id);
        updated.Name.Should().Be("New Name");
        updated.Type.Should().Be(AccountType.Savings);
        updated.Notes.Should().Be("updated notes");
    }

    [Fact]
    public async Task DeleteAsync_RemovesAccount()
    {
        var account = await _sut.AddAsync("To Delete", AccountType.CreditCard);

        await _sut.DeleteAsync(account.Id);

        var all = await _sut.GetAllAsync();
        all.Should().NotContain(a => a.Id == account.Id);
    }

    [Fact]
    public async Task SaveColumnMappingAsync_SavesNewMapping()
    {
        var account = await _sut.AddAsync("Bank", AccountType.Checking);
        var mapping = new ColumnMapping
        {
            DateColumn = "Date",
            DescriptionColumn = "Description",
            AmountColumn = "Amount",
            BalanceColumn = "Balance",
            ReferenceColumn = "Ref"
        };

        await _sut.SaveColumnMappingAsync(account.Id, mapping);

        var retrieved = await _sut.GetColumnMappingAsync(account.Id);
        retrieved.Should().NotBeNull();
        retrieved!.DateColumn.Should().Be("Date");
        retrieved.DescriptionColumn.Should().Be("Description");
        retrieved.AmountColumn.Should().Be("Amount");
        retrieved.BalanceColumn.Should().Be("Balance");
        retrieved.ReferenceColumn.Should().Be("Ref");
    }

    [Fact]
    public async Task SaveColumnMappingAsync_UpdatesExistingMapping()
    {
        var account = await _sut.AddAsync("Bank", AccountType.Checking);
        await _sut.SaveColumnMappingAsync(account.Id, new ColumnMapping { DateColumn = "Date", AmountColumn = "Amt" });

        await _sut.SaveColumnMappingAsync(account.Id, new ColumnMapping { DateColumn = "TxDate", AmountColumn = "Value" });

        var retrieved = await _sut.GetColumnMappingAsync(account.Id);
        retrieved!.DateColumn.Should().Be("TxDate");
        retrieved.AmountColumn.Should().Be("Value");
    }

    [Fact]
    public async Task GetColumnMappingAsync_ReturnsNullWhenNoMapping()
    {
        var account = await _sut.AddAsync("Bank", AccountType.Checking);

        var mapping = await _sut.GetColumnMappingAsync(account.Id);

        mapping.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_ThrowsWhenNotFound()
    {
        var act = async () => await _sut.UpdateAsync(9999, "X", AccountType.Checking);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task DeleteAsync_ThrowsWhenNotFound()
    {
        var act = async () => await _sut.DeleteAsync(9999);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
