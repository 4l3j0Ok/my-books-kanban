using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using MyBooksKanban.Application.Interfaces;
using MyBooksKanban.Application.Models;

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
    private readonly IDominantColorExtractor _colorExtractor;

    public CoverStorageService(
        IWebHostEnvironment env,
        ILogger<CoverStorageService> logger,
        IDominantColorExtractor colorExtractor)
    {
        _env = env;
        _logger = logger;
        _colorExtractor = colorExtractor;
    }

    public async Task<CoverUpload> ReadAsync(IBrowserFile file, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(file);

        var ext = Path.GetExtension(file.Name);
        if (!AllowedExtensions.Contains(ext))
            throw new ValidationException("Formato no soportado. Usa JPG, PNG o WebP.");
        if (!AllowedMimeTypes.Contains(file.ContentType))
            throw new ValidationException("Tipo de archivo no permitido.");
        if (file.Size > MaxBytes)
            throw new ValidationException("La portada supera el tamaño máximo permitido (4 MB).");

        using var buffer = new MemoryStream();
        await using (var source = file.OpenReadStream(MaxBytes, ct))
        {
            await source.CopyToAsync(buffer, ct);
        }

        buffer.Position = 0;
        var dominantColor = await ExtractDominantColorAsync(buffer, ct);

        return new CoverUpload(file.Name, file.ContentType, buffer.ToArray(), dominantColor);
    }

    public async Task<string> SaveAsync(CoverUpload upload, string? previousRelativePath, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(upload);

        var coversRoot = GetCoversRoot();
        Directory.CreateDirectory(coversRoot);

        var ext = Path.GetExtension(upload.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid():N}{ext}";
        var absolutePath = Path.Combine(coversRoot, fileName);

        await File.WriteAllBytesAsync(absolutePath, upload.Content, ct);

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

    public async Task<string?> GetDominantColorAsync(string? relativePath, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return null;

        var absolutePath = Path.Combine(GetCoversRoot(), Path.GetFileName(relativePath));
        if (!File.Exists(absolutePath)) return null;

        await using var stream = File.OpenRead(absolutePath);
        return await ExtractDominantColorAsync(stream, ct);
    }

    private async Task<string?> ExtractDominantColorAsync(Stream stream, CancellationToken ct)
    {
        try
        {
            return await _colorExtractor.ExtractAsync(stream, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Un color no calculable no debe impedir guardar la portada: el
            // formulario deja el color que ya hubiera y el usuario puede elegirlo.
            _logger.LogWarning(ex, "No se pudo calcular el color dominante de la portada");
            return null;
        }
    }

    private string GetCoversRoot()
    {
        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        return Path.Combine(webRoot, "uploads", "covers");
    }
}
