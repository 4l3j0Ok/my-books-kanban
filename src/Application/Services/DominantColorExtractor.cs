using MyBooksKanban.Application.Colors;
using MyBooksKanban.Application.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace MyBooksKanban.Application.Services;

/// <summary>
/// Calcula el color dominante de una portada.
///
/// No devuelve el color literalmente más repetido: eso haría que cualquier portada
/// con mucho fondo negro o blanco (que son la mayoría) produjese un lomo apagado.
/// El algoritmo:
///
/// 1. Reescala la imagen a un tamaño pequeño (el color no necesita resolución).
/// 2. Clasifica cada píxel en una franja de tono (24 franjas de 15°), o en una
///    única franja acromática si su croma es despreciable. Agrupar por tono y no
///    por cubos RGB es lo que hace que un degradado —un cielo, una nebulosa— cuente
///    como un solo color en lugar de repartirse entre decenas de cubos y perder
///    frente a un fondo plano.
/// 3. Pondera cada píxel por saturación y por cercanía a una luminosidad media,
///    de modo que los casi negros y los casi blancos aportan poco.
/// 4. Promedia (con esos mismos pesos) los píxeles de la franja ganadora.
/// </summary>
public sealed class DominantColorExtractor : IDominantColorExtractor
{
    /// <summary>Lado máximo tras el reescalado. Suficiente para el color, barato de recorrer.</summary>
    private const int SampleSize = 96;

    /// <summary>Franjas de tono de 15°.</summary>
    private const int HueBins = 24;

    /// <summary>Índice de la franja que recoge grises, negros y blancos.</summary>
    private const int AchromaticBin = HueBins;

    private const int BinCount = HueBins + 1;

    /// <summary>Croma (max-min, en 0..1) por debajo del cual un píxel se considera gris.</summary>
    private const double ChromaThreshold = 0.12;

    /// <summary>Peso mínimo de un píxel gris, para que una portada monocroma siga dando color.</summary>
    private const double AchromaticFloor = 0.12;

    /// <summary>Anchura de la campana que premia las luminosidades medias.</summary>
    private const double LightnessSigma = 0.22;

    public async Task<string?> ExtractAsync(Stream imageStream, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(imageStream);

        try
        {
            using var image = await Image.LoadAsync<Rgba32>(imageStream, ct);
            return Extract(image);
        }
        catch (UnknownImageFormatException)
        {
            return null;
        }
        catch (InvalidImageContentException)
        {
            return null;
        }
        catch (ImageFormatException)
        {
            return null;
        }
    }

    private static string? Extract(Image<Rgba32> image)
    {
        if (image.Width == 0 || image.Height == 0) return null;

        if (image.Width > SampleSize || image.Height > SampleSize)
        {
            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(SampleSize, SampleSize),
                Mode = ResizeMode.Max,
                Sampler = KnownResamplers.Box
            }));
        }

        var weights = new double[BinCount];
        var sumR = new double[BinCount];
        var sumG = new double[BinCount];
        var sumB = new double[BinCount];

        image.ProcessPixelRows(accessor =>
        {
            for (var y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (var x = 0; x < row.Length; x++)
                {
                    ref var px = ref row[x];
                    if (px.A < 128) continue; // ignora zonas transparentes

                    var (bin, weight) = Classify(px.R, px.G, px.B);
                    if (weight <= 0) continue;

                    weights[bin] += weight;
                    sumR[bin] += px.R * weight;
                    sumG[bin] += px.G * weight;
                    sumB[bin] += px.B * weight;
                }
            }
        });

        var best = -1;
        var bestWeight = 0d;
        for (var i = 0; i < BinCount; i++)
        {
            if (weights[i] > bestWeight)
            {
                bestWeight = weights[i];
                best = i;
            }
        }

        if (best < 0) return null;

        var r = (byte)Math.Clamp(Math.Round(sumR[best] / bestWeight), 0, 255);
        var g = (byte)Math.Clamp(Math.Round(sumG[best] / bestWeight), 0, 255);
        var b = (byte)Math.Clamp(Math.Round(sumB[best] / bestWeight), 0, 255);

        return SpinePalette.ToHex(r, g, b);
    }

    /// <summary>
    /// Devuelve la franja a la que pertenece el píxel y cuánto pesa su voto.
    /// Los colores vivos y de luminosidad media pesan más que los casi blancos,
    /// casi negros o grises.
    /// </summary>
    private static (int Bin, double Weight) Classify(byte red, byte green, byte blue)
    {
        var r = red / 255.0;
        var g = green / 255.0;
        var b = blue / 255.0;

        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var lightness = (max + min) / 2.0;
        var chroma = max - min;

        var lightnessWeight = Gaussian(lightness - 0.5, LightnessSigma);

        if (chroma < ChromaThreshold)
            return (AchromaticBin, AchromaticFloor * lightnessWeight);

        var denominator = 1 - Math.Abs(2 * lightness - 1);
        var saturation = denominator <= 1e-6 ? 1 : Math.Clamp(chroma / denominator, 0, 1);
        var chromaWeight = AchromaticFloor + (1 - AchromaticFloor) * saturation;

        return (HueBin(r, g, b, max, chroma), chromaWeight * lightnessWeight);
    }

    private static int HueBin(double r, double g, double b, double max, double chroma)
    {
        var hue = 60 * (max == r
            ? ((g - b) / chroma + 6) % 6
            : max == g
                ? (b - r) / chroma + 2
                : (r - g) / chroma + 4);

        return Math.Clamp((int)(hue / (360.0 / HueBins)), 0, HueBins - 1);
    }

    private static double Gaussian(double delta, double sigma) =>
        Math.Exp(-(delta * delta) / (2 * sigma * sigma));
}
