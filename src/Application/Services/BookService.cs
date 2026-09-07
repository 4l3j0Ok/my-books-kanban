using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MyBooksKanban.Application.Interfaces;
using MyBooksKanban.Application.Models;
using MyBooksKanban.Application.Validators;
using MyBooksKanban.Domain.Entities;
using MyBooksKanban.Domain.Enums;
using MyBooksKanban.Infrastructure.Data;

namespace MyBooksKanban.Application.Services;

public class BookService : IBookService
{
    private readonly IDbContextFactory<LibraryDbContext> _dbFactory;
    private readonly ILogger<BookService> _logger;

    public BookService(IDbContextFactory<LibraryDbContext> dbFactory, ILogger<BookService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task<BoardViewModel> GetBoardAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var books = await db.Books.AsNoTracking()
            .Include(b => b.Category)
            .OrderBy(b => b.ReadingStatus).ThenBy(b => b.Position)
            .ToListAsync(ct);

        var grouped = books
            .GroupBy(b => b.ReadingStatus)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<BookSummary>)g.Select(ToSummary).ToList());

        foreach (var status in BoardViewModel.AllStatuses)
            grouped.TryAdd(status, Array.Empty<BookSummary>());

        return new BoardViewModel { Columns = grouped };
    }

    public async Task<BookFormModel?> GetForEditAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var book = await db.Books.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id, ct);
        return book is null ? null : ToFormModel(book);
    }

    public async Task<BookSummary?> GetSummaryAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var book = await db.Books.AsNoTracking()
            .Include(b => b.Category)
            .FirstOrDefaultAsync(b => b.Id == id, ct);
        return book is null ? null : ToSummary(book);
    }

    public async Task<int> CreateAsync(BookFormModel model, CancellationToken ct = default)
    {
        Validate(model);
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var book = ToEntity(model);
        book.Position = await GetNextPositionAsync(db, book.ReadingStatus, ct);
        book.CreatedAt = DateTime.UtcNow;
        book.UpdatedAt = DateTime.UtcNow;
        db.Books.Add(book);
        await db.SaveChangesAsync(ct);
        _logger.LogInformation("Libro creado: {Title} (Id={Id})", book.Title, book.Id);
        return book.Id;
    }

    public async Task UpdateAsync(BookFormModel model, CancellationToken ct = default)
    {
        if (model.Id is null) throw new InvalidOperationException("Id requerido para actualizar.");
        Validate(model);

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var book = await db.Books.FirstOrDefaultAsync(b => b.Id == model.Id, ct)
            ?? throw new InvalidOperationException($"Libro {model.Id} no encontrado.");

        book.Title = model.Title.Trim();
        book.Author = model.Author.Trim();
        book.Description = model.Description;
        book.CoverPath = model.CoverPath;
        book.SpineColor = model.SpineColor;
        book.ReadingStatus = model.ReadingStatus;
        book.CategoryId = model.CategoryId;
        book.PageCount = model.PageCount;
        book.CurrentPage = model.CurrentPage;
        book.Rating = model.Rating;
        book.Isbn = model.Isbn;
        book.StartedAt = model.StartedAt;
        book.FinishedAt = model.FinishedAt;
        book.UpdatedAt = DateTime.UtcNow;

        if (book.ReadingStatus == ReadingStatus.Read && book.FinishedAt is null)
            book.FinishedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        _logger.LogInformation("Libro actualizado: {Title} (Id={Id})", book.Title, book.Id);
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var book = await db.Books.FirstOrDefaultAsync(b => b.Id == id, ct);
        if (book is null) return;

        var status = book.ReadingStatus;
        db.Books.Remove(book);
        await db.SaveChangesAsync(ct);
        await NormalizePositionsAsync(db, status, ct);
        _logger.LogInformation("Libro eliminado (Id={Id})", id);
    }

    public async Task<PageResult<BookSummary>> SearchAsync(
        string? term,
        int? categoryId,
        ReadingStatus? status,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;

        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var query = db.Books.AsNoTracking().Include(b => b.Category).AsQueryable();

        if (!string.IsNullOrWhiteSpace(term))
        {
            var lowered = term.Trim().ToLower();
            query = query.Where(b =>
                b.Title.ToLower().Contains(lowered) ||
                b.Author.ToLower().Contains(lowered));
        }

        if (categoryId.HasValue) query = query.Where(b => b.CategoryId == categoryId.Value);
        if (status.HasValue) query = query.Where(b => b.ReadingStatus == status.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(b => b.ReadingStatus).ThenBy(b => b.Position).ThenBy(b => b.Title)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(b => ToSummary(b))
            .ToListAsync(ct);

        return new PageResult<BookSummary>(items, page, pageSize, total);
    }

    public async Task MoveAsync(int bookId, ReadingStatus targetStatus, int? targetIndex, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var book = await db.Books.FirstOrDefaultAsync(b => b.Id == bookId, ct)
            ?? throw new InvalidOperationException($"Libro {bookId} no encontrado.");

        var origin = book.ReadingStatus;
        book.ReadingStatus = targetStatus;
        book.UpdatedAt = DateTime.UtcNow;

        var destinationBooks = await db.Books
            .Where(b => b.ReadingStatus == targetStatus && b.Id != bookId)
            .OrderBy(b => b.Position)
            .ToListAsync(ct);

        var insertAt = Math.Clamp(targetIndex ?? destinationBooks.Count, 0, destinationBooks.Count);
        destinationBooks.Insert(insertAt, book);

        for (var i = 0; i < destinationBooks.Count; i++)
            destinationBooks[i].Position = i;

        if (origin != targetStatus)
        {
            var originBooks = await db.Books
                .Where(b => b.ReadingStatus == origin && b.Id != bookId)
                .OrderBy(b => b.Position)
                .ToListAsync(ct);
            for (var i = 0; i < originBooks.Count; i++)
                originBooks[i].Position = i;
        }

        await db.SaveChangesAsync(ct);
        _logger.LogInformation("Libro movido: {Id} {From} -> {To} pos {Pos}",
            bookId, origin, targetStatus, insertAt);
    }

    public async Task<BookSummary?> UpdateProgressAsync(int bookId, int page, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var book = await db.Books
            .Include(b => b.Category)
            .FirstOrDefaultAsync(b => b.Id == bookId, ct);
        if (book is null) return null;

        // El tope real es el total del libro; sin total, el límite de validación.
        var target = Math.Clamp(page, 0, book.PageCount ?? BookFormModel.MaxTrackablePage);
        if (book.CurrentPage == target) return ToSummary(book);

        book.CurrentPage = target;
        book.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        _logger.LogInformation("Progreso actualizado: Id={Id} página {Page}", bookId, target);

        return ToSummary(book);
    }

    public async Task UpdateCoverAsync(int bookId, string relativePath, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var book = await db.Books.FirstOrDefaultAsync(b => b.Id == bookId, ct)
            ?? throw new InvalidOperationException($"Libro {bookId} no encontrado.");
        book.CoverPath = relativePath;
        book.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    private static async Task<int> GetNextPositionAsync(LibraryDbContext db, ReadingStatus status, CancellationToken ct)
    {
        var max = await db.Books.Where(b => b.ReadingStatus == status).Select(b => (int?)b.Position).MaxAsync(ct);
        return (max ?? -1) + 1;
    }

    private static async Task NormalizePositionsAsync(LibraryDbContext db, ReadingStatus status, CancellationToken ct)
    {
        var books = await db.Books.Where(b => b.ReadingStatus == status)
            .OrderBy(b => b.Position).ToListAsync(ct);
        for (var i = 0; i < books.Count; i++) books[i].Position = i;
        await db.SaveChangesAsync(ct);
    }

    private static void Validate(BookFormModel model)
    {
        var errors = BookFormValidator.ValidateBusinessRules(model);
        if (errors.Count > 0)
            throw new ValidationException(string.Join(" ", errors));
    }

    private static BookSummary ToSummary(Book b) =>
        new(b.Id, b.Title, b.Author, b.CoverPath, b.SpineColor, b.ReadingStatus, b.Position,
            b.CurrentPage, b.PageCount, b.Rating, b.Category?.Name ?? string.Empty);

    private static BookFormModel ToFormModel(Book b) => new()
    {
        Id = b.Id,
        Title = b.Title,
        Author = b.Author,
        Description = b.Description,
        CoverPath = b.CoverPath,
        SpineColor = b.SpineColor,
        ReadingStatus = b.ReadingStatus,
        CategoryId = b.CategoryId,
        PageCount = b.PageCount,
        CurrentPage = b.CurrentPage,
        Rating = b.Rating,
        Isbn = b.Isbn,
        StartedAt = b.StartedAt,
        FinishedAt = b.FinishedAt
    };

    private static Book ToEntity(BookFormModel m) => new()
    {
        Title = m.Title.Trim(),
        Author = m.Author.Trim(),
        Description = m.Description,
        CoverPath = m.CoverPath,
        SpineColor = m.SpineColor,
        ReadingStatus = m.ReadingStatus,
        CategoryId = m.CategoryId,
        PageCount = m.PageCount,
        CurrentPage = m.CurrentPage,
        Rating = m.Rating,
        Isbn = m.Isbn,
        StartedAt = m.StartedAt,
        FinishedAt = m.FinishedAt
    };
}

public class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
}
