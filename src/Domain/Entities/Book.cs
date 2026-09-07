using System.ComponentModel.DataAnnotations;
using MyBooksKanban.Domain.Enums;

namespace MyBooksKanban.Domain.Entities;

/// <summary>
/// Libro de la biblioteca. Se representa como una tarjeta con forma de lomo
/// en una columna del Kanban según su <see cref="ReadingStatus"/>.
/// </summary>
public class Book
{
    public int Id { get; set; }

    [Required]
    [StringLength(150, MinimumLength = 1)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(120, MinimumLength = 1)]
    public string Author { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Description { get; set; }

    /// <summary>Ruta pública de la portada (relativa a wwwroot).</summary>
    [StringLength(300)]
    public string? CoverPath { get; set; }

    /// <summary>
    /// Color del lomo en hexadecimal (<c>#RRGGBB</c>). Se propone al elegir portada,
    /// tomando el color dominante de la imagen, y el usuario puede cambiarlo: lo que
    /// quede aquí manda. Es <c>null</c> mientras nadie lo haya fijado, y entonces la
    /// UI recurre al color derivado de la categoría.
    /// </summary>
    [StringLength(7)]
    public string? SpineColor { get; set; }

    public ReadingStatus ReadingStatus { get; set; } = ReadingStatus.ToRead;

    /// <summary>Posición ordinal dentro de la columna (orden estable).</summary>
    public int Position { get; set; }

    [Range(1, 20000)]
    public int? PageCount { get; set; }

    [Range(0, 20000)]
    public int? CurrentPage { get; set; }

    [Range(1, 5)]
    public int? Rating { get; set; }

    [StringLength(20)]
    public string? Isbn { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? FinishedAt { get; set; }

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
