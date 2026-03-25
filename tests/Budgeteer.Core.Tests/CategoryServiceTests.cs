using Budgeteer.Core.Data;
using Budgeteer.Core.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Budgeteer.Core.Tests;

public class CategoryServiceTests : IAsyncLifetime
{
    private BudgeteerDbContext _db = null!;
    private CategoryService _sut = null!;

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<BudgeteerDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;
        _db = new BudgeteerDbContext(options);
        await _db.Database.OpenConnectionAsync();
        var initializer = new DatabaseInitializer(_db);
        await initializer.EnsureCreatedAsync();
        _sut = new CategoryService(_db);
    }

    public async Task DisposeAsync() => await _db.DisposeAsync();

    [Fact]
    public async Task EnsureCreated_SeedsDefaultCategories()
    {
        var categories = await _sut.GetAllAsync();
        categories.Should().HaveCount(14);
        categories.Select(c => c.Name).Should().Contain(["Groceries", "Dining", "Income", "Travel"]);
    }

    [Fact]
    public async Task AddAsync_AddsCategory()
    {
        var category = await _sut.AddAsync("Test Category");

        category.Id.Should().BeGreaterThan(0);
        category.Name.Should().Be("Test Category");

        var all = await _sut.GetAllAsync();
        all.Should().Contain(c => c.Name == "Test Category");
    }

    [Fact]
    public async Task RenameAsync_RenamesCategory()
    {
        var category = await _sut.AddAsync("Old Name");

        await _sut.RenameAsync(category.Id, "New Name");

        var all = await _sut.GetAllAsync();
        all.Should().Contain(c => c.Name == "New Name");
        all.Should().NotContain(c => c.Name == "Old Name");
    }

    [Fact]
    public async Task DeleteAsync_RemovesCategory()
    {
        var category = await _sut.AddAsync("To Delete");

        await _sut.DeleteAsync(category.Id);

        var all = await _sut.GetAllAsync();
        all.Should().NotContain(c => c.Id == category.Id);
    }

    [Fact]
    public async Task RenameAsync_ThrowsWhenNotFound()
    {
        var act = async () => await _sut.RenameAsync(9999, "X");
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task DeleteAsync_ThrowsWhenNotFound()
    {
        var act = async () => await _sut.DeleteAsync(9999);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
