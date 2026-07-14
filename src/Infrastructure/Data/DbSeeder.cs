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

        var books = new[]
        {
            new Book { Title = "Dune", Author = "Frank Herbert", ReadingStatus = ReadingStatus.Reading, Position = 0, CategoryId = sciFi.Id, PageCount = 412, CurrentPage = 88, Rating = 5 },
            new Book { Title = "El problema de los tres cuerpos", Author = "Cixin Liu", ReadingStatus = ReadingStatus.Reading, Position = 1, CategoryId = sciFi.Id, PageCount = 400, CurrentPage = 210 },
            new Book { Title = "Sapiens", Author = "Yuval Noah Harari", ReadingStatus = ReadingStatus.Read, Position = 0, CategoryId = essay.Id, PageCount = 496, CurrentPage = 496, Rating = 5, StartedAt = DateTime.UtcNow.AddMonths(-4), FinishedAt = DateTime.UtcNow.AddMonths(-2) },
            new Book { Title = "Pensar rápido, pensar despacio", Author = "Daniel Kahneman", ReadingStatus = ReadingStatus.Read, Position = 1, CategoryId = essay.Id, PageCount = 499, CurrentPage = 499, Rating = 4 },
            new Book { Title = "Los pilares de la Tierra", Author = "Ken Follett", ReadingStatus = ReadingStatus.ToRead, Position = 0, CategoryId = historic.Id, PageCount = 1040 },
            new Book { Title = "El nombre del viento", Author = "Patrick Rothfuss", ReadingStatus = ReadingStatus.ToRead, Position = 1, CategoryId = sciFi.Id, PageCount = 880 },
            new Book { Title = "Atomic Habits", Author = "James Clear", ReadingStatus = ReadingStatus.ToRead, Position = 2, CategoryId = personal.Id, PageCount = 320 },
            new Book { Title = "El poder de los hábitos", Author = "Charles Duhigg", ReadingStatus = ReadingStatus.Read, Position = 2, CategoryId = personal.Id, PageCount = 408, CurrentPage = 408, Rating = 4 }
        };
        db.Books.AddRange(books);
        await db.SaveChangesAsync(ct);
    }
}
