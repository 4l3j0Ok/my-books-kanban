using MyBooksKanban.Application.Models;

namespace MyBooksKanban.Application.Validators;

/// <summary>
/// Reglas de negocio que no encajan naturalmente en DataAnnotations.
/// Se exponen como un único método para que el formulario y los tests
/// compartan exactamente las mismas reglas.
/// </summary>
public static class BookFormValidator
{
    public static IReadOnlyList<string> ValidateBusinessRules(BookFormModel model)
    {
        var errors = new List<string>();

        if (model.PageCount is { } total && total <= 0)
            errors.Add("El total de páginas debe ser mayor que cero.");

        if (model.CurrentPage is { } current && current < 0)
            errors.Add("La página actual no puede ser negativa.");

        if (model.PageCount is { } total2 && model.CurrentPage is { } cur && cur > total2)
            errors.Add("La página actual no puede superar el total de páginas.");

        if (model.StartedAt is { } started && model.FinishedAt is { } finished && finished < started)
            errors.Add("La fecha de fin no puede ser anterior a la fecha de inicio.");

        if (model.Rating is { } r && (r < 1 || r > 5))
            errors.Add("La calificación debe estar entre 1 y 5.");

        return errors;
    }
}
