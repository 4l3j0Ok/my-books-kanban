using Bunit;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using MyBooksKanban.Application.Colors;
using MyBooksKanban.Application.Interfaces;
using MyBooksKanban.Application.Models;
using MyBooksKanban.Components.Books;
using Xunit;

namespace MyBooksKanban.Tests.Components;

/// <summary>Elección del color del lomo desde el formulario, en alta y en edición.</summary>
public class BookFormColorTests : TestContext
{
    private static readonly IReadOnlyList<CategoryListItem> Categories = new[]
    {
        new CategoryListItem(1, "Ensayo", 0),
        new CategoryListItem(2, "Ciencia ficción", 0)
    };

    private readonly FakeCoverStorageService _covers = new();

    public BookFormColorTests()
    {
        Services.AddSingleton<ICoverStorageService>(_covers);
    }

    [Fact]
    public void Picking_a_binding_cloth_sets_the_color_and_the_preview()
    {
        var model = NewBook();
        var cut = Render(model);

        cut.Find("[aria-label='Burdeos']").Click();

        Assert.Equal("#7a2e2b", model.SpineColor);
        Assert.Contains("--spine-accent: #7a2e2b", PreviewStyle(cut));
    }

    [Fact]
    public void A_new_book_starts_with_the_category_color_and_no_choice_of_its_own()
    {
        var model = NewBook();
        var cut = Render(model);

        Assert.Null(model.SpineColor);
        Assert.Contains($"--spine-accent: {SpinePalette.FromCategory("Ensayo")}", PreviewStyle(cut));
    }

    [Fact]
    public void Choosing_a_cover_proposes_its_dominant_color()
    {
        _covers.ColorOfPickedCover = "#1e6388";
        var model = NewBook();
        var cut = Render(model);

        UploadCover(cut);

        Assert.Equal("#1e6388", model.SpineColor);
        Assert.Contains("--spine-accent: #1e6388", PreviewStyle(cut));
    }

    [Fact]
    public void What_the_user_picks_after_the_cover_wins()
    {
        _covers.ColorOfPickedCover = "#1e6388";
        var model = NewBook();
        var cut = Render(model);

        UploadCover(cut);
        cut.Find("[aria-label='Ocre']").Click();

        Assert.Equal("#a9772c", model.SpineColor);
    }

    [Fact]
    public void The_cover_color_can_be_restored_after_picking_another_one()
    {
        _covers.ColorOfPickedCover = "#1e6388";
        var model = NewBook();
        var cut = Render(model);

        UploadCover(cut);
        cut.Find("[aria-label='Ocre']").Click();
        cut.Find("button:contains('Usar el color de la portada')").Click();

        Assert.Equal("#1e6388", model.SpineColor);
    }

    [Fact]
    public void The_color_can_be_handed_back_to_the_category()
    {
        var model = NewBook();
        var cut = Render(model);

        cut.Find("[aria-label='Ocre']").Click();
        cut.Find("button:contains('Volver al de la categoría')").Click();

        Assert.Null(model.SpineColor);
        Assert.Contains($"--spine-accent: {SpinePalette.FromCategory("Ensayo")}", PreviewStyle(cut));
    }

    [Fact]
    public void Editing_a_book_keeps_its_stored_color()
    {
        var model = NewBook();
        model.Id = 7;
        model.CoverPath = "/uploads/covers/x.jpg";
        model.SpineColor = "#63385c";
        _covers.ColorOfStoredCover = "#1e6388";

        var cut = Render(model);

        Assert.Contains("--spine-accent: #63385c", PreviewStyle(cut));
        // La portada guardada sigue ofreciendo su color por si quiere volver a él.
        cut.Find("button:contains('Usar el color de la portada')").Click();
        Assert.Equal("#1e6388", model.SpineColor);
    }

    [Fact]
    public void A_rejected_cover_shows_the_reason_and_leaves_the_color_alone()
    {
        _covers.ReadError = new MyBooksKanban.Application.Services.ValidationException(
            "Formato no soportado. Usa JPG, PNG o WebP.");
        var model = NewBook();
        model.SpineColor = "#7a2e2b";
        var cut = Render(model);

        UploadCover(cut);

        Assert.Null(model.CoverUpload);
        Assert.Equal("#7a2e2b", model.SpineColor);
        Assert.Contains("Formato no soportado", cut.Markup);
    }

    [Fact]
    public void The_chosen_color_travels_with_the_submitted_model()
    {
        var model = NewBook();
        BookFormModel? submitted = null;
        var cut = RenderComponent<BookForm>(p => p
            .Add(c => c.Model, model)
            .Add(c => c.Categories, Categories)
            .Add(c => c.OnSubmit, (BookFormModel m) => submitted = m));

        cut.Find("[aria-label='Verde inglés']").Click();
        cut.Find("form").Submit();

        cut.WaitForState(() => submitted is not null);
        Assert.Equal("#35604f", submitted!.SpineColor);
    }

    private static BookFormModel NewBook() => new()
    {
        Title = "Neuromante",
        Author = "William Gibson",
        CategoryId = 1
    };

    private IRenderedComponent<BookForm> Render(BookFormModel model) =>
        RenderComponent<BookForm>(p => p
            .Add(c => c.Model, model)
            .Add(c => c.Categories, Categories));

    private static void UploadCover(IRenderedComponent<BookForm> cut) =>
        cut.FindComponent<InputFile>()
            .UploadFiles(InputFileContent.CreateFromText("imagen", "portada.png"));

    private static string PreviewStyle(IRenderedComponent<BookForm> cut) =>
        cut.Find(".spine-face").ParentElement!.GetAttribute("style") ?? string.Empty;
}
