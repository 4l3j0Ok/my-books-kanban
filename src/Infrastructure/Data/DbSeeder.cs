using Microsoft.EntityFrameworkCore;
using MyBooksKanban.Domain.Entities;
using MyBooksKanban.Domain.Enums;

namespace MyBooksKanban.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(LibraryDbContext db, CancellationToken ct = default)
    {
        await db.Database.MigrateAsync(ct);

        if (await db.Categories.AnyAsync(ct))
            return;

        var categories = new[]
        {
            new Category { Name = "Ciencia ficción" },
            new Category { Name = "Ensayo" },
            new Category { Name = "Novela histórica" },
            new Category { Name = "Desarrollo personal" }
        };
        db.Categories.AddRange(categories);
        await db.SaveChangesAsync(ct);

        var sciFi = categories[0];
        var essay = categories[1];
        var historic = categories[2];
        var personal = categories[3];

    }
}
