using Budgeteer.Core.Data;
using Budgeteer.Core.Domain;
using Budgeteer.Core.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Budgeteer.Core.Tests;

public class BudgetServiceTests : IAsyncLifetime
{
    private BudgeteerDbContext _db = null!;
    private BudgetService _sut = null!;
    private CategoryService _categoryService = null!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<BudgeteerDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;
        _db = new BudgeteerDbContext(options);
        await _db.Database.OpenConnectionAsync();
        var initializer = new DatabaseInitializer(_db);
        await initializer.EnsureCreatedAsync();
        _sut = new BudgetService(_db);
        _categoryService = new CategoryService(_db);
    }

    public async Task DisposeAsync() => await _db.DisposeAsync();

    [Fact]
    public async Task AddAsync_AddsBudget()
    {
        var budget = await _sut.AddAsync(
            "Monthly Expenses", 1500m, BudgetTimespan.Monthly,
            rollover: false, alertEnabled: true, alertThresholdPercent: 80,
            categoryIds: []);

        budget.Id.Should().BeGreaterThan(0);
        budget.Name.Should().Be("Monthly Expenses");
        budget.SpendingLimit.Should().Be(1500m);
        budget.Timespan.Should().Be(BudgetTimespan.Monthly);
        budget.Rollover.Should().BeFalse();
        budget.AlertEnabled.Should().BeTrue();
        budget.AlertThresholdPercent.Should().Be(80);
    }

    [Fact]
    public async Task AddAsync_AssignsCategoryIds()
    {
        var categories = await _categoryService.GetAllAsync();
        var cat1 = categories[0];
        var cat2 = categories[1];

        await _sut.AddAsync(
            "Food Budget", 500m, BudgetTimespan.Monthly,
            rollover: false, alertEnabled: false, alertThresholdPercent: null,
            categoryIds: [cat1.Id, cat2.Id]);

        var all = await _sut.GetAllAsync();
        var budget = all.First(b => b.Name == "Food Budget");
        budget.BudgetCategories.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllBudgets()
    {
        await _sut.AddAsync("Budget A", 100m, BudgetTimespan.Weekly,
            false, false, null, []);
        await _sut.AddAsync("Budget B", 200m, BudgetTimespan.Yearly,
            false, false, null, []);

        var all = await _sut.GetAllAsync();

        all.Count.Should().BeGreaterThanOrEqualTo(2);
        all.Should().Contain(b => b.Name == "Budget A");
        all.Should().Contain(b => b.Name == "Budget B");
    }

    [Fact]
    public async Task GetAllAsync_IncludesCategoryNames()
    {
        var categories = await _categoryService.GetAllAsync();
        var cat = categories[0];

        await _sut.AddAsync("Single Category Budget", 300m, BudgetTimespan.Monthly,
            false, false, null, [cat.Id]);

        var all = await _sut.GetAllAsync();
        var budget = all.First(b => b.Name == "Single Category Budget");

        budget.BudgetCategories.Should().HaveCount(1);
        budget.BudgetCategories.First().Category.Name.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task UpdateAsync_UpdatesScalarFields()
    {
        var budget = await _sut.AddAsync("Original Name", 100m, BudgetTimespan.Weekly,
            false, false, null, []);

        await _sut.UpdateAsync(budget.Id, "Updated Name", 999m, BudgetTimespan.Yearly,
            rollover: true, alertEnabled: true, alertThresholdPercent: 90, categoryIds: []);

        var all = await _sut.GetAllAsync();
        var updated = all.First(b => b.Id == budget.Id);
        updated.Name.Should().Be("Updated Name");
        updated.SpendingLimit.Should().Be(999m);
        updated.Timespan.Should().Be(BudgetTimespan.Yearly);
        updated.Rollover.Should().BeTrue();
        updated.AlertEnabled.Should().BeTrue();
        updated.AlertThresholdPercent.Should().Be(90);
    }

    [Fact]
    public async Task UpdateAsync_ReplacesCategoryAssignments()
    {
        var categories = await _categoryService.GetAllAsync();
        var cat1 = categories[0];
        var cat2 = categories[1];
        var cat3 = categories[2];

        var budget = await _sut.AddAsync("Budget With Cats", 400m, BudgetTimespan.Monthly,
            false, false, null, [cat1.Id, cat2.Id]);

        await _sut.UpdateAsync(budget.Id, "Budget With Cats", 400m, BudgetTimespan.Monthly,
            false, false, null, [cat3.Id]);

        var all = await _sut.GetAllAsync();
        var updated = all.First(b => b.Id == budget.Id);
        updated.BudgetCategories.Should().HaveCount(1);
        updated.BudgetCategories.First().CategoryId.Should().Be(cat3.Id);
    }

    [Fact]
    public async Task DeleteAsync_RemovesBudget()
    {
        var budget = await _sut.AddAsync("To Delete", 100m, BudgetTimespan.Monthly,
            false, false, null, []);

        await _sut.DeleteAsync(budget.Id);

        var all = await _sut.GetAllAsync();
        all.Should().NotContain(b => b.Id == budget.Id);
    }

    [Fact]
    public async Task UpdateAsync_ThrowsWhenNotFound()
    {
        var act = async () => await _sut.UpdateAsync(9999, "X", 0m, BudgetTimespan.Monthly,
            false, false, null, []);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task DeleteAsync_ThrowsWhenNotFound()
    {
        var act = async () => await _sut.DeleteAsync(9999);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
