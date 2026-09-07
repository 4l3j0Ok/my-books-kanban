using MyBooksKanban.Application.Models;
using MyBooksKanban.Domain.Enums;

namespace MyBooksKanban.Application.Interfaces;

public interface IBookService
{
    Task<BoardViewModel> GetBoardAsync(CancellationToken ct = default);

    Task<BookFormModel?> GetForEditAsync(int id, CancellationToken ct = default);

    Task<BookSummary?> GetSummaryAsync(int id, CancellationToken ct = default);

    Task<int> CreateAsync(BookFormModel model, CancellationToken ct = default);

    Task UpdateAsync(BookFormModel model, CancellationToken ct = default);

    Task DeleteAsync(int id, CancellationToken ct = default);

    Task<PageResult<BookSummary>> SearchAsync(
        string? term,
        int? categoryId,
        ReadingStatus? status,
        int page,
        int pageSize,
        CancellationToken ct = default);

    Task MoveAsync(int bookId, ReadingStatus targetStatus, int? targetIndex, CancellationToken ct = default);

    /// <summary>
    /// Fija la página actual del libro. <paramref name="page"/> se recorta al rango
    /// [0, total de páginas] —avanzar en la última página o retroceder en la primera
    /// no es un error, simplemente no mueve nada—. Devuelve <c>null</c> si el libro no existe.
    /// </summary>
    Task<BookSummary?> UpdateProgressAsync(int bookId, int page, CancellationToken ct = default);

    Task UpdateCoverAsync(int bookId, string relativePath, CancellationToken ct = default);
}
