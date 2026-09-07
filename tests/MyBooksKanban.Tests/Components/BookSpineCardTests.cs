using Bunit;
using MyBooksKanban.Application.Colors;
using MyBooksKanban.Application.Models;
using MyBooksKanban.Components.Books;
using MyBooksKanban.Domain.Enums;
using Xunit;

namespace MyBooksKanban.Tests.Components;

public class BookSpineCardTests : TestContext
{
    [Fact]
    public void Paints_the_spine_with_the_cover_dominant_color()
    {
        var cut = RenderComponent<BookSpineCard>(p => p
            .Add(c => c.Book, Book(coverPath: "/uploads/covers/x.jpg", spineColor: "#1e5f9a")));

        var style = cut.Find("article").GetAttribute("style");

        Assert.Contains("--spine-accent: #1e5f9a", style);
        Assert.Contains($"--spine-ink: {SpinePalette.LightInk}", style);
    }

    [Fact]
    public void Uses_dark_ink_when_the_cover_color_is_light()
    {
        var cut = RenderComponent<BookSpineCard>(p => p
            .Add(c => c.Book, Book(coverPath: "/uploads/covers/x.jpg", spineColor: "#f4e7c3")));

        var style = cut.Find("article").GetAttribute("style");

        Assert.Contains($"--spine-ink: {SpinePalette.DarkInk}", style);
    }

    [Fact]
    public void Falls_back_to_the_category_color_when_there_is_no_cover()
    {
        var cut = RenderComponent<BookSpineCard>(p => p
            .Add(c => c.Book, Book(coverPath: null, spineColor: null)));

        var style = cut.Find("article").GetAttribute("style");

        Assert.Contains($"--spine-accent: {SpinePalette.FromCategory("Ciencia ficción")}", style);
    }

    private static BookSummary Book(string? coverPath, string? spineColor) => new(
        Id: 1,
        Title: "Neuromante",
        Author: "William Gibson",
        CoverPath: coverPath,
        SpineColor: spineColor,
        Status: ReadingStatus.ToRead,
        Position: 0,
        CurrentPage: null,
        PageCount: 271,
        Rating: 4,
        CategoryName: "Ciencia ficción");
}
