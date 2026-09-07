using MyBooksKanban.Domain.Enums;

namespace MyBooksKanban.Application.Models;

/// <summary>Proyección ligera de un libro para las tarjetas del Kanban.</summary>
public sealed record BookSummary(
    int Id,
    string Title,
    string Author,
    string? CoverPath,
    string? SpineColor,
    ReadingStatus Status,
    int Position,
    int? CurrentPage,
    int? PageCount,
    int? Rating,
    string CategoryName);
