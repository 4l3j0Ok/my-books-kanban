using System.ComponentModel.DataAnnotations;
using MyBooksKanban.Domain.Enums;

namespace MyBooksKanban.Application.Models;

/// <summary>
/// Modelo de formulario. Se separa de la entidad para evitar arrastrar el grafo
/// de relaciones al binder y para validar de forma específica de UI.
/// </summary>
public class BookFormModel
{
    /// <summary>
    /// Tope de páginas admitido. Vive aquí porque las anotaciones del formulario,
    /// el servicio y el paginador del detalle deben compartir exactamente el mismo límite.
    /// </summary>
    public const int MaxTrackablePage = 20_000;

    public int? Id { get; set; }

    [Required(ErrorMessage = "El título es obligatorio.")]
    [StringLength(150, MinimumLength = 1, ErrorMessage = "El título debe tener entre 1 y 150 caracteres.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "El autor es obligatorio.")]
    [StringLength(120, MinimumLength = 1, ErrorMessage = "El autor debe tener entre 1 y 120 caracteres.")]
    public string Author { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "La descripción no puede superar los 2000 caracteres.")]
    public string? Description { get; set; }

    public string? CoverPath { get; set; }

    /// <summary>
    /// Color del lomo (<c>#RRGGBB</c>). Se propone solo al elegir portada, a partir
    /// del color dominante de la imagen, y el usuario puede cambiarlo. En
    /// <c>null</c> el lomo toma el color de la categoría.
    /// </summary>
    [RegularExpression("^#[0-9a-fA-F]{6}$", ErrorMessage = "El color del lomo debe ser hexadecimal (#RRGGBB).")]
    public string? SpineColor { get; set; }

    [Required(ErrorMessage = "Selecciona un estado de lectura.")]
    public ReadingStatus ReadingStatus { get; set; } = ReadingStatus.ToRead;

    [Required(ErrorMessage = "Selecciona una categoría.")]
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona una categoría válida.")]
    public int CategoryId { get; set; }

    [Range(1, MaxTrackablePage, ErrorMessage = "El total de páginas debe estar entre 1 y 20000.")]
    public int? PageCount { get; set; }

    [Range(0, MaxTrackablePage, ErrorMessage = "La página actual debe estar entre 0 y 20000.")]
    public int? CurrentPage { get; set; }

    [Range(1, 5, ErrorMessage = "La calificación debe estar entre 1 y 5.")]
    public int? Rating { get; set; }

    [StringLength(20, ErrorMessage = "El ISBN no puede superar los 20 caracteres.")]
    public string? Isbn { get; set; }

    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }

    /// <summary>
    /// Portada recién elegida, ya leída a memoria. Se escribe en disco al guardar.
    /// No se persiste en el modelo de dominio.
    /// </summary>
    public CoverUpload? CoverUpload { get; set; }
}
