using Microsoft.EntityFrameworkCore;
using MyBooksKanban.Infrastructure.Data;

namespace MyBooksKanban.Tests;

/// <summary>Helper para tests: crea un factory apuntando a un SQLite temporal único.</summary>
public static class TestDbContextFactory
{
    public static IDbContextFactory<LibraryDbContext> Create()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"mbk-test-{Guid.NewGuid():N}.db");
        var connection = $"Data Source={tempFile}";
        var options = new DbContextOptionsBuilder<LibraryDbContext>()
            .UseSqlite(connection)
            .Options;
        return new LibraryDbContextFactory(options);
    }
}
