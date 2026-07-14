using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MyBooksKanban.Infrastructure.Data;

/// <summary>
/// Factory usada por las herramientas de EF (migrations) en tiempo de diseño
/// y por la aplicación en runtime para crear contextos por operación.
/// </summary>
public class LibraryDbContextFactory : IDesignTimeDbContextFactory<LibraryDbContext>, IDbContextFactory<LibraryDbContext>
{
    private readonly DbContextOptions<LibraryDbContext>? _runtimeOptions;

    public LibraryDbContextFactory() { }

    public LibraryDbContextFactory(DbContextOptions<LibraryDbContext> options)
    {
        _runtimeOptions = options;
    }

    private DbContextOptions<LibraryDbContext> BuildOptions()
    {
        if (_runtimeOptions is not null) return _runtimeOptions;

        var connection = Environment.GetEnvironmentVariable("MY_BOOKS_KANBAN_CONNECTION")
            ?? "Data Source=my-books-kanban.db";

        var builder = new DbContextOptionsBuilder<LibraryDbContext>();
        builder.UseSqlite(connection);
        return builder.Options;
    }

    public LibraryDbContext CreateDbContext() => new(BuildOptions());

    LibraryDbContext IDesignTimeDbContextFactory<LibraryDbContext>.CreateDbContext(string[] args)
        => new(BuildOptions());
}
