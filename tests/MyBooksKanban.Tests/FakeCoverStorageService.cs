using Microsoft.AspNetCore.Components.Forms;
using MyBooksKanban.Application.Interfaces;
using MyBooksKanban.Application.Models;

namespace MyBooksKanban.Tests;

/// <summary>
/// Doble de prueba del almacenamiento de portadas: no toca disco ni decodifica
/// imágenes, sólo devuelve los colores que el test decida.
/// </summary>
public sealed class FakeCoverStorageService : ICoverStorageService
{
    /// <summary>Color que devolverá <see cref="ReadAsync"/> para la portada recién elegida.</summary>
    public string? ColorOfPickedCover { get; set; }

    /// <summary>Color que devolverá <see cref="GetDominantColorAsync"/> para una portada ya guardada.</summary>
    public string? ColorOfStoredCover { get; set; }

    /// <summary>Error a lanzar desde <see cref="ReadAsync"/>, para probar el camino de validación.</summary>
    public Exception? ReadError { get; set; }

    public CoverUpload? Saved { get; private set; }

    public Task<CoverUpload> ReadAsync(IBrowserFile file, CancellationToken ct = default)
    {
        if (ReadError is not null) throw ReadError;

        return Task.FromResult(new CoverUpload(
            file.Name, file.ContentType, Array.Empty<byte>(), ColorOfPickedCover));
    }

    public Task<string> SaveAsync(CoverUpload upload, string? previousRelativePath, CancellationToken ct = default)
    {
        Saved = upload;
        return Task.FromResult($"/uploads/covers/{upload.FileName}");
    }

    public Task DeleteAsync(string? relativePath, CancellationToken ct = default) => Task.CompletedTask;

    public Task<string?> GetDominantColorAsync(string? relativePath, CancellationToken ct = default) =>
        Task.FromResult(string.IsNullOrWhiteSpace(relativePath) ? null : ColorOfStoredCover);
}
