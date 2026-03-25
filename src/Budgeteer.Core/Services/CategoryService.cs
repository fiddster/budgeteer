using Budgeteer.Core.Data;
using Budgeteer.Core.Domain;
using Microsoft.EntityFrameworkCore;

namespace Budgeteer.Core.Services;

public class CategoryService(BudgeteerDbContext db) : ICategoryService
{
    public async Task<IReadOnlyList<Category>> GetAllAsync() =>
        await db.Categories.OrderBy(c => c.Name).ToListAsync();

    public async Task<Category> AddAsync(string name)
    {
        var category = new Category { Name = name };
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        return category;
    }

    public async Task RenameAsync(int id, string newName)
    {
        var category = await db.Categories.FindAsync(id)
            ?? throw new InvalidOperationException($"Category {id} not found.");
        category.Name = newName;
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var category = await db.Categories.FindAsync(id)
            ?? throw new InvalidOperationException($"Category {id} not found.");
        db.Categories.Remove(category);
        await db.SaveChangesAsync();
    }
}
