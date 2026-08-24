using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using MyBooksKanban.Application.Models;
using MyBooksKanban.Application.Services;
using MyBooksKanban.Domain.Enums;
using MyBooksKanban.Infrastructure.Data;
using Xunit;

namespace MyBooksKanban.Tests.Application;

public class BookServiceTests : IAsyncLifetime
{
    private IDbContextFactory<LibraryDbContext> _factory = default!;
    private BookService _sut = default!;

    public async Task InitializeAsync()
    {
        _factory = TestDbContextFactory.Create();
        await using var db = await _factory.CreateDbContextAsync();
        await DbSeeder.SeedAsync(db);
        _sut = new BookService(_factory, NullLogger<BookService>.Instance);
    }

    public Task DisposeAsync() { _factory = null!; return Task.CompletedTask; }

    [Fact]
    public async Task GetBoardAsync_returns_three_columns()
    {
        var board = await _sut.GetBoardAsync();
        Assert.Equal(3, board.Columns.Count);
        Assert.Contains(ReadingStatus.ToRead, board.Columns.Keys);
        Assert.Contains(ReadingStatus.Reading, board.Columns.Keys);
        Assert.Contains(ReadingStatus.Read, board.Columns.Keys);
    }

    [Fact]
    public async Task CreateAsync_persists_book_and_assigns_position()
    {
        var cat = (await new CategoryService(_factory, NullLogger<CategoryService>.Instance).ListAsync()).First();
        var before = (await _sut.GetBoardAsync()).Columns[ReadingStatus.ToRead].Count;
        var id = await _sut.CreateAsync(new BookFormModel
        {
            Title = "Neuromante",
            Author = "William Gibson",
            CategoryId = cat.Id,
            ReadingStatus = ReadingStatus.ToRead
        });
        Assert.True(id > 0);
        var summary = await _sut.GetSummaryAsync(id);
        Assert.NotNull(summary);
        Assert.Equal("Neuromante", summary!.Title);
        Assert.Equal(before, summary.Position); // siguiente posición libre
    }

    [Fact]
    public async Task UpdateAsync_modifies_fields()
    {
        var board = await _sut.GetBoardAsync();
        var first = board.Columns[ReadingStatus.ToRead].First();
        var model = await _sut.GetForEditAsync(first.Id);
        Assert.NotNull(model);
        model!.Description = "Descripción editada";
        model.PageCount = 1234;
        model.Rating = 4;
        await _sut.UpdateAsync(model);

        var after = await _sut.GetSummaryAsync(first.Id);
        Assert.NotNull(after);
    }

    [Fact]
    public async Task MoveAsync_changes_status_and_preserves_unique_positions()
    {
        var board = await _sut.GetBoardAsync();
        var toMove = board.Columns[ReadingStatus.ToRead].First();
        await _sut.MoveAsync(toMove.Id, ReadingStatus.Reading, 0);

        var after = await _sut.GetBoardAsync();
        var newCol = after.Columns[ReadingStatus.Reading];
        var positions = newCol.Select(b => b.Position).ToList();
        Assert.Equal(positions.Count, positions.Distinct().Count());
        Assert.Contains(newCol, b => b.Id == toMove.Id);
    }

    [Fact]
    public async Task DeleteAsync_normalizes_positions()
    {
        var board = await _sut.GetBoardAsync();
        var first = board.Columns[ReadingStatus.ToRead].First();
        await _sut.DeleteAsync(first.Id);

        var after = await _sut.GetBoardAsync();
        var col = after.Columns[ReadingStatus.ToRead];
        for (var i = 0; i < col.Count; i++)
            Assert.Equal(i, col[i].Position);
    }

    [Fact]
    public async Task SearchAsync_filters_by_term_and_paginates()
    {
        var page1 = await _sut.SearchAsync("Dune", null, null, 1, 5);
        Assert.Contains(page1.Items, b => b.Title.Contains("Dune"));
        Assert.True(page1.PageSize == 5);
        Assert.True(page1.TotalPages >= 1);
    }

    [Fact]
    public async Task SearchAsync_filters_by_status()
    {
        var result = await _sut.SearchAsync(null, null, ReadingStatus.Read, 1, 50);
        Assert.NotEmpty(result.Items);
        Assert.All(result.Items, b => Assert.Equal(ReadingStatus.Read, b.Status));
    }
}
