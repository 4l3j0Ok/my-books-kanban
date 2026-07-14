using Microsoft.AspNetCore.Components.Forms;

namespace MyBooksKanban.Application.Interfaces;

public interface ICoverStorageService
{
    /// <summary>Devuelve la ruta pública (relativa a wwwroot) o null si no se guardó.</summary>
    Task<string?> SaveAsync(IBrowserFile file, string? previousRelativePath, CancellationToken ct = default);

    Task DeleteAsync(string? relativePath, CancellationToken ct = default);
}
