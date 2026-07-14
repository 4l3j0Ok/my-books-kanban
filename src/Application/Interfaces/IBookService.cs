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

    Task UpdateCoverAsync(int bookId, string relativePath, CancellationToken ct = default);
}
