using MyBooksKanban.Application.Models;

namespace MyBooksKanban.Application.Interfaces;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryListItem>> ListAsync(CancellationToken ct = default);
    Task<int> CreateAsync(string name, CancellationToken ct = default);
    Task UpdateAsync(int id, string name, CancellationToken ct = default);
    Task<CategoryDeletionResult> DeleteAsync(int id, CancellationToken ct = default);
}

public sealed record CategoryListItem(int Id, string Name, int BookCount);

public enum CategoryDeletionResult
{
    Deleted,
    NotFound,
    InUse
}
