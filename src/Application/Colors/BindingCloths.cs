namespace MyBooksKanban.Application.Colors;

/// <summary>Un color de encuadernación con su nombre.</summary>
/// <param name="Name">Nombre del color, tal y como se lee en la interfaz.</param>
/// <param name="Hex">Valor en <c>#RRGGBB</c>.</param>
public sealed record BindingCloth(string Name, string Hex);

/// <summary>
/// Paleta de telas de encuadernación que se ofrece al elegir el color del lomo.
///
/// Todos los tonos caen dentro de la banda de luminosidad legible de
/// <see cref="SpinePalette"/>, así que el lomo sale exactamente del color elegido
/// y ninguno queda ilegible.
/// </summary>
public static class BindingCloths
{
    public static IReadOnlyList<BindingCloth> All { get; } = new[]
    {
        new BindingCloth("Burdeos", "#7a2e2b"),
        new BindingCloth("Verde inglés", "#35604f"),
        new BindingCloth("Azul tinta", "#2a4c6e"),
        new BindingCloth("Ocre", "#a9772c"),
        new BindingCloth("Cuero", "#7a5230"),
        new BindingCloth("Ciruela", "#63385c"),
        new BindingCloth("Pizarra", "#4a5058"),
        new BindingCloth("Lino", "#c9b78d")
    };
}
