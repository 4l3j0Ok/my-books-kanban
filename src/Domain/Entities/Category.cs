using System.ComponentModel.DataAnnotations;

namespace MyBooksKanban.Domain.Entities;

/// <summary>
/// Categoría temática asignable a un libro. Una categoría agrupa N libros.
/// </summary>
public class Category
{
    public int Id { get; set; }

    [Required]
    [StringLength(60, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    public ICollection<Book> Books { get; set; } = new List<Book>();
}
