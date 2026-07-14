using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components.Forms;
using MyBooksKanban.Domain.Enums;

namespace MyBooksKanban.Application.Models;

/// <summary>
/// Modelo de formulario. Se separa de la entidad para evitar arrastrar el grafo
/// de relaciones al binder y para validar de forma específica de UI.
/// </summary>
public class BookFormModel
{
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

    [Required(ErrorMessage = "Selecciona un estado de lectura.")]
    public ReadingStatus ReadingStatus { get; set; } = ReadingStatus.ToRead;

    [Required(ErrorMessage = "Selecciona una categoría.")]
    [Range(1, int.MaxValue, ErrorMessage = "Selecciona una categoría válida.")]
    public int CategoryId { get; set; }

    [Range(1, 20000, ErrorMessage = "El total de páginas debe estar entre 1 y 20000.")]
    public int? PageCount { get; set; }

    [Range(0, 20000, ErrorMessage = "La página actual debe estar entre 0 y 20000.")]
    public int? CurrentPage { get; set; }

    [Range(1, 5, ErrorMessage = "La calificación debe estar entre 1 y 5.")]
    public int? Rating { get; set; }

    [StringLength(20, ErrorMessage = "El ISBN no puede superar los 20 caracteres.")]
    public string? Isbn { get; set; }

    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }

    /// <summary>Archivo de portada subido. No se persiste en el modelo de dominio.</summary>
    public IBrowserFile? CoverFile { get; set; }
}
