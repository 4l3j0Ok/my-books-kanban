using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyBooksKanban.Application.Interfaces;
using MyBooksKanban.Domain.Entities;
using MyBooksKanban.Infrastructure.Data;

namespace MyBooksKanban.Application.Services;

public class CategoryService : ICategoryService
{
    private readonly IDbContextFactory<LibraryDbContext> _dbFactory;
    private readonly ILogger<CategoryService> _logger;

    public CategoryService(IDbContextFactory<LibraryDbContext> dbFactory, ILogger<CategoryService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<CategoryListItem>> ListAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.Categories.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryListItem(c.Id, c.Name, c.Books.Count))
            .ToListAsync(ct);
    }

    public async Task<int> CreateAsync(string name, CancellationToken ct = default)
    {
        var clean = (name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(clean))
            throw new ValidationException("El nombre de la categoría es obligatorio.");

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var exists = await db.Categories.AnyAsync(c => c.Name.ToLower() == clean.ToLower(), ct);
        if (exists) throw new ValidationException("Ya existe una categoría con ese nombre.");

        var category = new Category { Name = clean };
        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);
        _logger.LogInformation("Categoría creada: {Name} (Id={Id})", clean, category.Id);
        return category.Id;
    }

    public async Task UpdateAsync(int id, string name, CancellationToken ct = default)
    {
        var clean = (name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(clean))
            throw new ValidationException("El nombre de la categoría es obligatorio.");

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new InvalidOperationException($"Categoría {id} no encontrada.");

        category.Name = clean;
        await db.SaveChangesAsync(ct);
    }

    public async Task<CategoryDeletionResult> DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (category is null) return CategoryDeletionResult.NotFound;

        var inUse = await db.Books.AnyAsync(b => b.CategoryId == id, ct);
        if (inUse) return CategoryDeletionResult.InUse;

        db.Categories.Remove(category);
        await db.SaveChangesAsync(ct);
        _logger.LogInformation("Categoría eliminada (Id={Id})", id);
        return CategoryDeletionResult.Deleted;
    }
}
