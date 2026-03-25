using Budgeteer.Core.Domain;

namespace Budgeteer.Core.Services;

public interface ICategoryService
{
    Task<IReadOnlyList<Category>> GetAllAsync();
    Task<Category> AddAsync(string name);
    Task RenameAsync(int id, string newName);
    Task DeleteAsync(int id);
}
