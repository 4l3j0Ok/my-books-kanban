using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyBooksKanban.Application.Interfaces;
using MyBooksKanban.Infrastructure.Data;

namespace MyBooksKanban.Application.Services;

/// <summary>
/// Calcula el color dominante de las portadas que se subieron antes de que
/// existiera <c>Book.SpineColor</c>. Es idempotente: sólo toca libros con
/// portada y sin color, así que arrancar la app varias veces no repite trabajo.
/// </summary>
public static class SpineColorBackfill
{
    public static async Task<int> RunAsync(
        LibraryDbContext db,
        ICoverStorageService covers,
        ILogger logger,
        CancellationToken ct = default)
    {
        var pending = await db.Books
            .Where(b => b.CoverPath != null && b.SpineColor == null)
            .ToListAsync(ct);

        if (pending.Count == 0) return 0;

        var updated = 0;
        foreach (var book in pending)
        {
            var color = await covers.GetDominantColorAsync(book.CoverPath, ct);
            if (color is null) continue;

            book.SpineColor = color;
            updated++;
        }

        if (updated > 0)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Color de lomo calculado para {Count} portada(s) existente(s).", updated);
        }

        return updated;
    }
}
