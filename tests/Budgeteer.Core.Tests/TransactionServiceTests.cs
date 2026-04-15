using Budgeteer.Core.Data;
using Budgeteer.Core.Domain;
using Budgeteer.Core.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Budgeteer.Core.Tests;

public class TransactionServiceTests : IAsyncLifetime
{
    private BudgeteerDbContext _db = null!;
    private TransactionService _sut = null!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<BudgeteerDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;
        _db = new BudgeteerDbContext(options);
        await _db.Database.OpenConnectionAsync();
        await _db.Database.EnsureCreatedAsync();
        _sut = new TransactionService(_db);
    }

    public async Task DisposeAsync() => await _db.DisposeAsync();

    private async Task<Account> CreateAccountAsync(string name = "Test Account")
    {
        var account = new Account { Name = name, Type = AccountType.Checking };
        _db.Accounts.Add(account);
        await _db.SaveChangesAsync();
        return account;
    }

    private async Task<Category> CreateCategoryAsync(string name = "Test Category")
    {
        var category = new Category { Name = name };
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();
        return category;
    }

    private async Task<Transaction> CreateTransactionAsync(
        int accountId,
        DateOnly? date = null,
        string description = "Test Transaction",
        decimal amount = 100m,
        int? categoryId = null,
        bool isTransfer = false)
    {
        var tx = new Transaction
        {
            AccountId = accountId,
            Date = date ?? new DateOnly(2024, 6, 15),
            Description = description,
            Amount = amount,
            CategoryId = categoryId,
            IsTransfer = isTransfer
        };
        _db.Transactions.Add(tx);
        await _db.SaveChangesAsync();
        return tx;
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllTransactions()
    {
        var account = await CreateAccountAsync();
        await CreateTransactionAsync(account.Id, description: "Tx1");
        await CreateTransactionAsync(account.Id, description: "Tx2");
        await CreateTransactionAsync(account.Id, description: "Tx3");

        var result = await _sut.GetAllAsync();

        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetAllAsync_FilterByAccountId()
    {
        var account1 = await CreateAccountAsync("Account 1");
        var account2 = await CreateAccountAsync("Account 2");
        await CreateTransactionAsync(account1.Id, description: "A1-Tx1");
        await CreateTransactionAsync(account1.Id, description: "A1-Tx2");
        await CreateTransactionAsync(account2.Id, description: "A2-Tx1");

        var result = await _sut.GetAllAsync(accountId: account1.Id);

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(t => t.AccountId.Should().Be(account1.Id));
    }

    [Fact]
    public async Task GetAllAsync_FilterByCategoryId()
    {
        var account = await CreateAccountAsync();
        var category = await CreateCategoryAsync();
        await CreateTransactionAsync(account.Id, description: "Cat-Tx1", categoryId: category.Id);
        await CreateTransactionAsync(account.Id, description: "Cat-Tx2", categoryId: category.Id);
        await CreateTransactionAsync(account.Id, description: "NoCat-Tx1");

        var result = await _sut.GetAllAsync(categoryId: category.Id);

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(t => t.CategoryId.Should().Be(category.Id));
    }

    [Fact]
    public async Task GetAllAsync_FilterByDateRange()
    {
        var account = await CreateAccountAsync();
        await CreateTransactionAsync(account.Id, date: new DateOnly(2024, 1, 1), description: "Jan");
        await CreateTransactionAsync(account.Id, date: new DateOnly(2024, 6, 15), description: "Jun");
        await CreateTransactionAsync(account.Id, date: new DateOnly(2024, 12, 31), description: "Dec");

        var result = await _sut.GetAllAsync(
            from: new DateOnly(2024, 6, 1),
            to: new DateOnly(2024, 12, 31));

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_OrderedByDateDescending()
    {
        var account = await CreateAccountAsync();
        await CreateTransactionAsync(account.Id, date: new DateOnly(2024, 1, 1), description: "Oldest");
        await CreateTransactionAsync(account.Id, date: new DateOnly(2024, 6, 15), description: "Middle");
        await CreateTransactionAsync(account.Id, date: new DateOnly(2024, 12, 31), description: "Newest");

        var result = await _sut.GetAllAsync();

        result[0].Date.Should().Be(new DateOnly(2024, 12, 31));
    }

    [Fact]
    public async Task MarkAsTransferAsync_PersistsFlag()
    {
        var account = await CreateAccountAsync();
        var tx = await CreateTransactionAsync(account.Id, isTransfer: false);

        await _sut.MarkAsTransferAsync(tx.Id, true);

        var result = await _db.Transactions.FindAsync(tx.Id);
        result!.IsTransfer.Should().BeTrue();
    }

    [Fact]
    public async Task MarkAsTransferAsync_ThrowsWhenNotFound()
    {
        var act = async () => await _sut.MarkAsTransferAsync(9999, true);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetSpendingTransactionsAsync_ExcludesTransfers()
    {
        var account = await CreateAccountAsync();
        await CreateTransactionAsync(account.Id, description: "Transfer", isTransfer: true);
        await CreateTransactionAsync(account.Id, description: "Spending1", isTransfer: false);
        await CreateTransactionAsync(account.Id, description: "Spending2", isTransfer: false);

        var result = await _sut.GetSpendingTransactionsAsync();

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(t => t.IsTransfer.Should().BeFalse());
    }
}
