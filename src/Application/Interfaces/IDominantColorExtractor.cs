namespace MyBooksKanban.Application.Interfaces;

/// <summary>
/// Extrae el color dominante de una imagen de portada para pintar el lomo del libro.
/// </summary>
public interface IDominantColorExtractor
{
    /// <summary>
    /// Devuelve el color dominante en formato <c>#RRGGBB</c>, o <c>null</c> si la
    /// imagen no se puede decodificar.
    /// </summary>
    /// <param name="imageStream">Stream con la imagen codificada. No se cierra.</param>
    Task<string?> ExtractAsync(Stream imageStream, CancellationToken ct = default);
}
