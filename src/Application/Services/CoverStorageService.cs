using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using MyBooksKanban.Application.Interfaces;

namespace MyBooksKanban.Application.Services;

public class CoverStorageService : ICoverStorageService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".jpg", ".jpeg", ".png", ".webp" };

    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
        { "image/jpeg", "image/png", "image/webp" };

    private const long MaxBytes = 4 * 1024 * 1024; // 4 MB
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<CoverStorageService> _logger;

    public CoverStorageService(IWebHostEnvironment env, ILogger<CoverStorageService> logger)
    {
        _env = env;
        _logger = logger;
    }

    public async Task<string?> SaveAsync(IBrowserFile file, string? previousRelativePath, CancellationToken ct = default)
    {
        if (file is null) return null;

        var ext = Path.GetExtension(file.Name);
        if (!AllowedExtensions.Contains(ext))
            throw new ValidationException("Formato no soportado. Usa JPG, PNG o WebP.");
        if (!AllowedMimeTypes.Contains(file.ContentType))
            throw new ValidationException("Tipo de archivo no permitido.");
        if (file.Size > MaxBytes)
            throw new ValidationException("La portada supera el tamaño máximo permitido (4 MB).");

        var coversRoot = GetCoversRoot();
        Directory.CreateDirectory(coversRoot);

        var fileName = $"{Guid.NewGuid():N}{ext.ToLowerInvariant()}";
        var absolutePath = Path.Combine(coversRoot, fileName);

        await using (var stream = File.Create(absolutePath))
        {
            await file.OpenReadStream(MaxBytes, ct).CopyToAsync(stream, ct);
        }

        var publicPath = $"/uploads/covers/{fileName}";
        _logger.LogInformation("Portada guardada: {Path}", publicPath);

        if (!string.IsNullOrWhiteSpace(previousRelativePath))
            await DeleteAsync(previousRelativePath, ct);

        return publicPath;
    }

    public Task DeleteAsync(string? relativePath, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return Task.CompletedTask;

        var coversRoot = GetCoversRoot();
        var fileName = Path.GetFileName(relativePath);
        var absolutePath = Path.Combine(coversRoot, fileName);

        if (File.Exists(absolutePath))
        {
            try
            {
                File.Delete(absolutePath);
                _logger.LogInformation("Portada eliminada: {Path}", relativePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo eliminar la portada {Path}", relativePath);
            }
        }

        return Task.CompletedTask;
    }

    private string GetCoversRoot()
    {
        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        return Path.Combine(webRoot, "uploads", "covers");
    }
}
