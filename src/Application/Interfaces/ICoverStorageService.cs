using Microsoft.AspNetCore.Components.Forms;
using MyBooksKanban.Application.Models;

namespace MyBooksKanban.Application.Interfaces;

public interface ICoverStorageService
{
    /// <summary>
    /// Valida el archivo elegido, lo lee a memoria y calcula su color dominante.
    /// Lanza <see cref="Services.ValidationException"/> si el formato o el tamaño no son admisibles.
    /// </summary>
    Task<CoverUpload> ReadAsync(IBrowserFile file, CancellationToken ct = default);

    /// <summary>
    /// Escribe la portada en disco y borra la anterior. Devuelve la ruta pública
    /// relativa a wwwroot, p. ej. <c>/uploads/covers/ab12.jpg</c>.
    /// </summary>
    Task<string> SaveAsync(CoverUpload upload, string? previousRelativePath, CancellationToken ct = default);

    Task DeleteAsync(string? relativePath, CancellationToken ct = default);

    /// <summary>
    /// Calcula el color dominante de una portada ya almacenada. Lo usan el relleno
    /// de libros antiguos y el botón "usar el color de la portada". Devuelve
    /// <c>null</c> si el archivo no existe o no se puede decodificar.
    /// </summary>
    Task<string?> GetDominantColorAsync(string? relativePath, CancellationToken ct = default);
}
