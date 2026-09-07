using Bunit;
using MyBooksKanban.Application.Models;
using MyBooksKanban.Components.Books;
using MyBooksKanban.Domain.Enums;
using Xunit;

namespace MyBooksKanban.Tests.Components;

/// <summary>
/// El paginador de la ficha: avanzar, retroceder y escribir la página directamente.
/// El componente sólo propone la página; persistirla es cosa del padre.
/// </summary>
public class BookCoverDetailTests : TestContext
{
    [Fact]
    public void Advancing_asks_for_the_next_page()
    {
        int? asked = null;
        var cut = Render(Book(currentPage: 41, pageCount: 271), page => asked = page);

        cut.Find("button[aria-label='Avanzar una página']").Click();

        Assert.Equal(42, asked);
    }

    [Fact]
    public void Rewinding_asks_for_the_previous_page()
    {
        int? asked = null;
        var cut = Render(Book(currentPage: 41, pageCount: 271), page => asked = page);

        cut.Find("button[aria-label='Retroceder una página']").Click();

        Assert.Equal(40, asked);
    }

    [Fact]
    public void Typing_a_page_asks_for_that_page()
    {
        int? asked = null;
        var cut = Render(Book(currentPage: 41, pageCount: 271), page => asked = page);

        cut.Find(".catalogue-pager-input").Change("150");

        Assert.Equal(150, asked);
    }

    [Fact]
    public void A_page_beyond_the_end_is_clamped_to_the_last_one()
    {
        int? asked = null;
        var cut = Render(Book(currentPage: 41, pageCount: 271), page => asked = page);

        cut.Find(".catalogue-pager-input").Change("9999");

        Assert.Equal(271, asked);
    }

    [Fact]
    public void The_ends_of_the_book_are_dead_ends()
    {
        var start = Render(Book(currentPage: 0, pageCount: 271), _ => { });
        Assert.True(start.Find("button[aria-label='Retroceder una página']").HasAttribute("disabled"));

        var end = Render(Book(currentPage: 271, pageCount: 271), _ => { });
        Assert.True(end.Find("button[aria-label='Avanzar una página']").HasAttribute("disabled"));
    }

    [Fact]
    public void A_book_without_a_declared_total_still_tracks_pages()
    {
        int? asked = null;
        var cut = Render(Book(currentPage: null, pageCount: null), page => asked = page);

        Assert.Empty(cut.FindAll(".catalogue-progress-track"));

        cut.Find("button[aria-label='Avanzar una página']").Click();

        Assert.Equal(1, asked);
    }

    [Fact]
    public void The_bar_reflects_the_page_read()
    {
        var cut = Render(Book(currentPage: 68, pageCount: 272), _ => { });

        Assert.Contains("width:25%", cut.Find(".catalogue-progress-bar").GetAttribute("style"));
    }

    private IRenderedComponent<BookCoverDetail> Render(BookSummary book, Action<int> onProgressChange) =>
        RenderComponent<BookCoverDetail>(p => p
            .Add(c => c.Book, book)
            .Add(c => c.OnProgressChange, onProgressChange));

    private static BookSummary Book(int? currentPage, int? pageCount) => new(
        Id: 1,
        Title: "Neuromante",
        Author: "William Gibson",
        CoverPath: null,
        SpineColor: null,
        Status: ReadingStatus.Reading,
        Position: 0,
        CurrentPage: currentPage,
        PageCount: pageCount,
        Rating: null,
        CategoryName: "Ciencia ficción");
}
