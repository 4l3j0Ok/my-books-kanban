using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MyBooksKanban.Application.Interfaces;
using MyBooksKanban.Application.Models;
using MyBooksKanban.Components.Books;
using MyBooksKanban.Domain.Enums;
using Xunit;
using DomainEnums = MyBooksKanban.Domain.Enums;

namespace MyBooksKanban.Tests.Components;

public class BookFormTests : TestContext
{
    private static readonly IReadOnlyList<CategoryListItem> Categories = new[]
    {
        new CategoryListItem(1, "Ensayo", 0),
        new CategoryListItem(2, "Ciencia ficción", 0)
    };

    [Fact]
    public void Renders_required_field_errors_when_empty()
    {
        var model = new BookFormModel { CategoryId = 0 };
        var cut = RenderComponent<BookForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Categories, Categories));

        var form = cut.Find("form");
        form.Submit();

        // Debe aparecer al menos un mensaje de validación (ValidationMessage
        // emite <li> con texto). Buscamos cualquier nodo con role="alert".
        cut.WaitForState(() => cut.FindAll("[role='alert']").Count > 0);
    }

    [Fact]
    public void Submit_invokes_OnSubmit_when_valid()
    {
        var model = new BookFormModel
        {
            Title = "Dune",
            Author = "Frank Herbert",
            CategoryId = 1,
            ReadingStatus = DomainEnums.ReadingStatus.Reading
        };
        BookFormModel? received = null;
        var cut = RenderComponent<BookForm>(parameters => parameters
            .Add(p => p.Model, model)
            .Add(p => p.Categories, Categories)
            .Add(p => p.OnSubmit, EventCallback.Factory.Create<BookFormModel>(this, m => { received = m; })));

        cut.Find("form").Submit();
        cut.WaitForState(() => received is not null);
        Assert.Equal("Dune", received!.Title);
    }
}
