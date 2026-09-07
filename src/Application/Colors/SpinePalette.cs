using System.Globalization;

namespace MyBooksKanban.Application.Colors;

/// <summary>
/// Utilidades de color para el lomo del libro. Todo el código es puro y determinista
/// para poder testearlo sin infraestructura.
/// </summary>
public static class SpinePalette
{
    /// <summary>Color usado cuando no hay portada ni categoría.</summary>
    public const string FallbackAccent = "#8a7050";

    /// <summary>Tinta clara para lomos oscuros.</summary>
    public const string LightInk = "#ffffff";

    /// <summary>Tinta oscura para lomos claros (deriva del token <c>--ink</c>).</summary>
    public const string DarkInk = "#221c12";

    /// <summary>Halo sutil detrás del texto claro.</summary>
    public const string LightInkHalo = "rgba(0, 0, 0, 0.35)";

    /// <summary>Halo sutil detrás del texto oscuro.</summary>
    public const string DarkInkHalo = "rgba(255, 255, 255, 0.45)";

    /// <summary>
    /// El lomo lleva una textura de cuero con <c>mix-blend-mode: multiply</c> que
    /// oscurece el fondo real respecto al accent. Se estima ese oscurecimiento
    /// antes de decidir la tinta para no elegir texto oscuro sobre un lomo que
    /// acabará siendo medio-oscuro en pantalla.
    /// </summary>
    private const double TextureDarkening = 0.82;

    /// <summary>
    /// Banda de luminosidad en la que un lomo se lee bien. El color dominante crudo
    /// se guarda tal cual en la base de datos; sólo la presentación lo trae a esta
    /// banda, conservando tono y saturación. Sin esto, una portada mayoritariamente
    /// negra (muy comunes) daría un lomo negro indistinguible del de al lado.
    /// </summary>
    private const double MinLightness = 0.28;

    private const double MaxLightness = 0.70;

    private static readonly double DarkInkLuminance = RelativeLuminance((0x22, 0x1c, 0x12));

    /// <summary>
    /// Resuelve accent/ink/halo del lomo. Si el libro tiene un color dominante
    /// calculado desde la portada se usa ése; si no, se deriva del nombre de la
    /// categoría para que cada categoría mantenga un color estable.
    /// </summary>
    public static (string Accent, string Ink, string InkHalo) Resolve(string? dominantColor, string? categoryName)
    {
        var accent = TryParseHex(dominantColor, out var rgb)
            ? ClampLightness(rgb)
            : FromCategory(categoryName);

        var ink = InkFor(Darken(accent, TextureDarkening));
        var halo = ink == DarkInk ? DarkInkHalo : LightInkHalo;

        return (accent, ink, halo);
    }

    /// <summary>
    /// Lleva el color a la banda de luminosidad legible sin tocar tono ni saturación.
    /// Devuelve el color intacto si ya está dentro de la banda.
    /// </summary>
    public static string ClampLightness((byte R, byte G, byte B) rgb)
    {
        var (hue, saturation, lightness) = ToHsl(rgb);
        var clamped = Math.Clamp(lightness, MinLightness, MaxLightness);

        return Math.Abs(clamped - lightness) < 1e-9
            ? ToHex(rgb.R, rgb.G, rgb.B)
            : HslToHex(hue, saturation, clamped);
    }

    private static string Darken(string hex, double factor)
    {
        if (!TryParseHex(hex, out var rgb)) return hex;

        return ToHex(
            (byte)Math.Clamp(Math.Round(rgb.R * factor), 0, 255),
            (byte)Math.Clamp(Math.Round(rgb.G * factor), 0, 255),
            (byte)Math.Clamp(Math.Round(rgb.B * factor), 0, 255));
    }

    private static (double Hue, double Saturation, double Lightness) ToHsl((byte R, byte G, byte B) rgb)
    {
        var r = rgb.R / 255.0;
        var g = rgb.G / 255.0;
        var b = rgb.B / 255.0;

        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var lightness = (max + min) / 2.0;
        var chroma = max - min;

        if (chroma <= 1e-9) return (0, 0, lightness);

        var denominator = 1 - Math.Abs(2 * lightness - 1);
        var saturation = denominator <= 1e-9 ? 0 : Math.Clamp(chroma / denominator, 0, 1);

        var hue = 60 * (max == r
            ? ((g - b) / chroma + 6) % 6
            : max == g
                ? (b - r) / chroma + 2
                : (r - g) / chroma + 4);

        return (hue, saturation, lightness);
    }

    /// <summary>Color estable derivado del nombre de la categoría.</summary>
    public static string FromCategory(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return FallbackAccent;

        var hash = 0;
        foreach (var ch in name) hash = (hash * 31 + ch) & 0xFFFFFF;
        return HslToHex(hash % 360, 0.38, 0.42);
    }

    /// <summary>
    /// Elige la tinta con mayor contraste WCAG sobre <paramref name="backgroundHex"/>.
    /// </summary>
    public static string InkFor(string? backgroundHex)
    {
        if (!TryParseHex(backgroundHex, out var rgb)) return LightInk;

        var luminance = RelativeLuminance(rgb);
        var contrastWithLight = 1.05 / (luminance + 0.05);
        var contrastWithDark = (luminance + 0.05) / (DarkInkLuminance + 0.05);

        return contrastWithDark > contrastWithLight ? DarkInk : LightInk;
    }

    /// <summary>Parsea <c>#RGB</c>, <c>#RRGGBB</c> o sus variantes sin almohadilla.</summary>
    public static bool TryParseHex(string? value, out (byte R, byte G, byte B) rgb)
    {
        rgb = default;
        if (string.IsNullOrWhiteSpace(value)) return false;

        var span = value.AsSpan().Trim();
        if (span.Length > 0 && span[0] == '#') span = span[1..];

        if (span.Length == 3)
        {
            if (!TryHexNibble(span[0], out var r) ||
                !TryHexNibble(span[1], out var g) ||
                !TryHexNibble(span[2], out var b)) return false;

            rgb = ((byte)(r * 17), (byte)(g * 17), (byte)(b * 17));
            return true;
        }

        if (span.Length != 6) return false;

        if (!byte.TryParse(span[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rr) ||
            !byte.TryParse(span.Slice(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var gg) ||
            !byte.TryParse(span.Slice(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var bb))
            return false;

        rgb = (rr, gg, bb);
        return true;
    }

    public static string ToHex(byte r, byte g, byte b) =>
        string.Create(7, (r, g, b), static (span, c) =>
        {
            span[0] = '#';
            c.r.TryFormat(span[1..3], out _, "x2", CultureInfo.InvariantCulture);
            c.g.TryFormat(span[3..5], out _, "x2", CultureInfo.InvariantCulture);
            c.b.TryFormat(span[5..7], out _, "x2", CultureInfo.InvariantCulture);
        });

    private static bool TryHexNibble(char c, out int value)
    {
        value = c switch
        {
            >= '0' and <= '9' => c - '0',
            >= 'a' and <= 'f' => c - 'a' + 10,
            >= 'A' and <= 'F' => c - 'A' + 10,
            _ => -1
        };
        return value >= 0;
    }

    private static double RelativeLuminance((byte R, byte G, byte B) rgb) =>
        0.2126 * Linearize(rgb.R) + 0.7152 * Linearize(rgb.G) + 0.0722 * Linearize(rgb.B);

    private static double Linearize(byte channel)
    {
        var s = channel / 255.0;
        return s <= 0.04045 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
    }

    private static string HslToHex(double hueDegrees, double saturation, double lightness)
    {
        var c = (1 - Math.Abs(2 * lightness - 1)) * saturation;
        var h = hueDegrees / 60.0;
        var x = c * (1 - Math.Abs(h % 2 - 1));
        var m = lightness - c / 2;

        var (r, g, b) = h switch
        {
            < 1 => (c, x, 0d),
            < 2 => (x, c, 0d),
            < 3 => (0d, c, x),
            < 4 => (0d, x, c),
            < 5 => (x, 0d, c),
            _ => (c, 0d, x)
        };

        return ToHex(ToByte(r + m), ToByte(g + m), ToByte(b + m));
    }

    private static byte ToByte(double value) => (byte)Math.Clamp(Math.Round(value * 255), 0, 255);
}
