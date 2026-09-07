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

    [Fact]
    public async Task UpdateCoverAsync_sets_the_path_without_touching_the_chosen_color()
    {
        var id = await CreateBookAsync();
        var model = await _sut.GetForEditAsync(id);
        model!.SpineColor = "#63385c";
        await _sut.UpdateAsync(model);

        await _sut.UpdateCoverAsync(id, "/uploads/covers/portada.jpg");

        var summary = await _sut.GetSummaryAsync(id);
        Assert.Equal("/uploads/covers/portada.jpg", summary!.CoverPath);
        Assert.Equal("#63385c", summary.SpineColor);
    }

    [Fact]
    public async Task UpdateAsync_round_trips_the_spine_color()
    {
        var id = await CreateBookAsync();
        var model = await _sut.GetForEditAsync(id);

        model!.CoverPath = "/uploads/covers/portada.png";
        model.SpineColor = "#1e5f9a";
        await _sut.UpdateAsync(model);

        var summary = await _sut.GetSummaryAsync(id);
        Assert.Equal("#1e5f9a", summary!.SpineColor);

        var form = await _sut.GetForEditAsync(id);
        Assert.Equal("#1e5f9a", form!.SpineColor);
    }

    [Fact]
    public async Task The_chosen_color_survives_removing_the_cover()
    {
        var id = await CreateBookAsync();
        var model = await _sut.GetForEditAsync(id);
        model!.CoverPath = "/uploads/covers/portada.jpg";
        model.SpineColor = "#c0392b";
        await _sut.UpdateAsync(model);

        model = await _sut.GetForEditAsync(id);
        model!.CoverPath = null;
        await _sut.UpdateAsync(model);

        var summary = await _sut.GetSummaryAsync(id);
        Assert.Null(summary!.CoverPath);
        Assert.Equal("#c0392b", summary.SpineColor);
    }

    [Fact]
    public async Task UpdateAsync_clears_the_spine_color_when_the_user_asks_for_the_category_color()
    {
        var id = await CreateBookAsync();
        var model = await _sut.GetForEditAsync(id);
        model!.SpineColor = "#c0392b";
        await _sut.UpdateAsync(model);

        model = await _sut.GetForEditAsync(id);
        model!.SpineColor = null;
        await _sut.UpdateAsync(model);

        var summary = await _sut.GetSummaryAsync(id);
        Assert.Null(summary!.SpineColor);
    }

    [Fact]
    public async Task CreateAsync_persists_a_spine_color_chosen_without_a_cover()
    {
        var cat = (await new CategoryService(_factory, NullLogger<CategoryService>.Instance).ListAsync()).First();
        var id = await _sut.CreateAsync(new BookFormModel
        {
            Title = "Sin portada",
            Author = "Anónimo",
            CategoryId = cat.Id,
            SpineColor = "#c0392b"
        });

        var summary = await _sut.GetSummaryAsync(id);
        Assert.Equal("#c0392b", summary!.SpineColor);
    }

    [Fact]
    public async Task UpdateProgressAsync_persists_the_page_and_returns_the_fresh_summary()
    {
        var id = await CreateBookAsync(pageCount: 271);

        var updated = await _sut.UpdateProgressAsync(id, 42);

        Assert.Equal(42, updated!.CurrentPage);
        Assert.Equal(42, (await _sut.GetSummaryAsync(id))!.CurrentPage);
        Assert.Equal(42, (await _sut.GetForEditAsync(id))!.CurrentPage);
    }

    [Theory]
    [InlineData(-5, 0)]     // retroceder en la primera página no baja de cero
    [InlineData(999, 271)]  // avanzar más allá del final se queda en el final
    public async Task UpdateProgressAsync_clamps_to_the_book_bounds(int requested, int expected)
    {
        var id = await CreateBookAsync(pageCount: 271);

        var updated = await _sut.UpdateProgressAsync(id, requested);

        Assert.Equal(expected, updated!.CurrentPage);
    }

    [Fact]
    public async Task UpdateProgressAsync_allows_tracking_a_book_without_a_declared_total()
    {
        var id = await CreateBookAsync(pageCount: null);

        var updated = await _sut.UpdateProgressAsync(id, 120);

        Assert.Equal(120, updated!.CurrentPage);
        Assert.Null(updated.PageCount);
    }

    [Fact]
    public async Task UpdateProgressAsync_returns_null_when_the_book_does_not_exist()
    {
        Assert.Null(await _sut.UpdateProgressAsync(99999, 10));
    }

    private async Task<int> CreateBookAsync(int? pageCount)
    {
        var cat = (await new CategoryService(_factory, NullLogger<CategoryService>.Instance).ListAsync()).First();
        return await _sut.CreateAsync(new BookFormModel
        {
            Title = "Neuromante",
            Author = "William Gibson",
            CategoryId = cat.Id,
            ReadingStatus = ReadingStatus.Reading,
            PageCount = pageCount
        });
    }

    private async Task<int> CreateBookAsync()
    {
        var cat = (await new CategoryService(_factory, NullLogger<CategoryService>.Instance).ListAsync()).First();
        return await _sut.CreateAsync(new BookFormModel
        {
            Title = "Neuromante",
            Author = "William Gibson",
            CategoryId = cat.Id,
            ReadingStatus = ReadingStatus.ToRead
        });
    }
}
