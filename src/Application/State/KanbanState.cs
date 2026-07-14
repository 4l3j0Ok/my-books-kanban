using MyBooksKanban.Domain.Enums;

namespace MyBooksKanban.Application.State;

/// <summary>
/// Estado compartido de la UI del Kanban. No contiene reglas de negocio:
/// solo aquello que la pantalla necesita recordar entre componentes.
/// La base de datos sigue siendo la fuente de verdad del dato.
/// </summary>
public class KanbanState
{
    public string? SearchTerm { get; private set; }
    public int? CategoryId { get; private set; }
    public ReadingStatus? Status { get; private set; }
    public int? SelectedBookId { get; private set; }
    public bool IsLoading { get; private set; }
    public string? Notification { get; private set; }
    public NotificationKind NotificationKind { get; private set; } = NotificationKind.Info;

    public event Action? Changed;

    public void SetSearch(string? term) { SearchTerm = term; Notify(); }
    public void SetCategory(int? id) { CategoryId = id; Notify(); }
    public void SetStatus(ReadingStatus? status) { Status = status; Notify(); }
    public void ClearFilters() { SearchTerm = null; CategoryId = null; Status = null; Notify(); }

    public void OpenBook(int bookId) { SelectedBookId = bookId; Notify(); }
    public void CloseBook() { SelectedBookId = null; Notify(); }

    public void SetLoading(bool value) { IsLoading = value; Notify(); }

    public void NotifyInfo(string message) { Notification = message; NotificationKind = NotificationKind.Info; Notify(); }
    public void NotifySuccess(string message) { Notification = message; NotificationKind = NotificationKind.Success; Notify(); }
    public void NotifyError(string message) { Notification = message; NotificationKind = NotificationKind.Error; Notify(); }
    public void ClearNotification() { Notification = null; Notify(); }

    private void Notify() => Changed?.Invoke();
}

public enum NotificationKind { Info, Success, Error }
