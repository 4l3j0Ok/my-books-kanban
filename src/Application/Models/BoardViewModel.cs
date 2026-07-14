using MyBooksKanban.Domain.Enums;

namespace MyBooksKanban.Application.Models;

/// <summary>
/// Vista agregada del tablero: una colección de <see cref="BookSummary"/>
/// por cada estado de lectura.
/// </summary>
public sealed class BoardViewModel
{
    public IReadOnlyDictionary<ReadingStatus, IReadOnlyList<BookSummary>> Columns { get; init; }
        = new Dictionary<ReadingStatus, IReadOnlyList<BookSummary>>();

    public static IReadOnlyList<ReadingStatus> AllStatuses { get; } =
        new[] { ReadingStatus.ToRead, ReadingStatus.Reading, ReadingStatus.Read };
}
