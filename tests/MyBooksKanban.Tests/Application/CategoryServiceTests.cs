using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MyBooksKanban.Application.Services;
using MyBooksKanban.Infrastructure.Data;
using Xunit;

namespace MyBooksKanban.Tests.Application;

public class CategoryServiceTests : IAsyncLifetime
{
    private IDbContextFactory<LibraryDbContext> _factory = default!;
    private CategoryService _sut = default!;

    public async Task InitializeAsync()
    {
        _factory = TestDbContextFactory.Create();
        await using var db = await _factory.CreateDbContextAsync();
        await DbSeeder.SeedAsync(db);
        _sut = new CategoryService(_factory, NullLogger<CategoryService>.Instance);
    }

    public Task DisposeAsync() { _factory = null!; return Task.CompletedTask; }

    [Fact]
    public async Task ListAsync_returns_seeded_categories()
    {
        var items = await _sut.ListAsync();
        Assert.Contains(items, c => c.Name == "Ciencia ficción");
    }

    [Fact]
    public async Task CreateAsync_adds_category()
    {
        var id = await _sut.CreateAsync("Historia");
        Assert.True(id > 0);
        var list = await _sut.ListAsync();
        Assert.Contains(list, c => c.Name == "Historia");
    }

    [Fact]
    public async Task DeleteAsync_blocks_when_in_use()
    {
        var list = await _sut.ListAsync();
        var inUse = list.First(c => c.BookCount > 0);
        var result = await _sut.DeleteAsync(inUse.Id);
        Assert.Equal(MyBooksKanban.Application.Interfaces.CategoryDeletionResult.InUse, result);
    }

    [Fact]
    public async Task DeleteAsync_removes_unused_category()
    {
        var id = await _sut.CreateAsync("Temporal");
        var result = await _sut.DeleteAsync(id);
        Assert.Equal(MyBooksKanban.Application.Interfaces.CategoryDeletionResult.Deleted, result);
    }
}
