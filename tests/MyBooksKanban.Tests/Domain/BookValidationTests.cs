using System.ComponentModel.DataAnnotations;
using MyBooksKanban.Application.Models;
using MyBooksKanban.Application.Validators;
using MyBooksKanban.Domain.Entities;
using MyBooksKanban.Domain.Enums;
using Xunit;

namespace MyBooksKanban.Tests.Domain;

public class BookValidationTests
{
    [Fact]
    public void Book_title_is_required()
    {
        var book = new Book { Author = "Anónimo", Title = "" };
        var ctx = new ValidationContext(book);
        var results = new List<ValidationResult>();
        var valid = Validator.TryValidateObject(book, ctx, results, true);
        Assert.False(valid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(Book.Title)));
    }

    [Fact]
    public void Book_author_is_required()
    {
        var book = new Book { Title = "Dune", Author = "" };
        var ctx = new ValidationContext(book);
        var results = new List<ValidationResult>();
        var valid = Validator.TryValidateObject(book, ctx, results, true);
        Assert.False(valid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(Book.Author)));
    }

    [Fact]
    public void Book_rating_must_be_between_1_and_5()
    {
        var book = new Book { Title = "x", Author = "y", Rating = 7 };
        var ctx = new ValidationContext(book);
        var results = new List<ValidationResult>();
        var valid = Validator.TryValidateObject(book, ctx, results, true);
        Assert.False(valid);
    }

    [Theory]
    [InlineData(-1, 100)]
    [InlineData(50, 10)]
    [InlineData(101, 100)]
    public void Business_rules_current_page_must_not_exceed_or_be_negative(int current, int total)
    {
        var model = new BookFormModel
        {
            Title = "x", Author = "y", CategoryId = 1,
            PageCount = total, CurrentPage = current
        };
        var errors = BookFormValidator.ValidateBusinessRules(model);
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void Business_rules_finished_at_cannot_precede_started_at()
    {
        var model = new BookFormModel
        {
            Title = "x", Author = "y", CategoryId = 1,
            StartedAt = new DateTime(2025, 1, 10),
            FinishedAt = new DateTime(2025, 1, 1)
        };
        var errors = BookFormValidator.ValidateBusinessRules(model);
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void ReadingStatus_enum_values()
    {
        Assert.Equal(0, (int)ReadingStatus.ToRead);
        Assert.Equal(1, (int)ReadingStatus.Reading);
        Assert.Equal(2, (int)ReadingStatus.Read);
    }
}
