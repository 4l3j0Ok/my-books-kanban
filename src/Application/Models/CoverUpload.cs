namespace MyBooksKanban.Application.Models;

/// <summary>
/// Portada elegida en el formulario, ya leída a memoria y analizada.
///
/// El archivo se lee una sola vez, al seleccionarlo: así el formulario puede
/// proponer el color del lomo al instante y, al guardar, escribir en disco sin
/// volver a transferir la imagen por el circuito de Blazor Server.
/// </summary>
/// <param name="FileName">Nombre original, del que se conserva la extensión.</param>
/// <param name="ContentType">MIME declarado por el navegador.</param>
/// <param name="Content">Bytes de la imagen.</param>
/// <param name="DominantColor">Color dominante en <c>#RRGGBB</c>, o <c>null</c> si no se pudo calcular.</param>
public sealed record CoverUpload(
    string FileName,
    string ContentType,
    byte[] Content,
    string? DominantColor);
