using MyBooksKanban.Application.Colors;
using MyBooksKanban.Application.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace MyBooksKanban.Tests.Application;

public class DominantColorExtractorTests
{
    private readonly DominantColorExtractor _sut = new();

    [Fact]
    public async Task Returns_the_exact_color_of_a_solid_image()
    {
        await using var png = CreatePng(64, 64, new Rgba32(0xC0, 0x39, 0x2B));

        var hex = await _sut.ExtractAsync(png);

        Assert.Equal("#c0392b", hex);
    }

    [Fact]
    public async Task Prefers_the_saturated_accent_over_a_dominant_white_background()
    {
        // Portada típica: fondo blanco mayoritario con un bloque de color.
        // El color útil para el lomo es el bloque, no el blanco.
        await using var png = CreatePng(100, 100, new Rgba32(255, 255, 255),
            image => FillRect(image, 10, 10, 30, 30, new Rgba32(0xD0, 0x21, 0x1C)));

        var hex = await _sut.ExtractAsync(png);

        Assert.True(SpinePalette.TryParseHex(hex, out var rgb), $"Hex inválido: {hex}");
        Assert.True(rgb.R > 170, $"Se esperaba un rojo dominante, se obtuvo {hex}");
        Assert.True(rgb.G < 90 && rgb.B < 90, $"Se esperaba un rojo dominante, se obtuvo {hex}");
    }

    [Fact]
    public async Task Still_returns_a_color_for_a_fully_grayscale_image()
    {
        await using var png = CreatePng(48, 48, new Rgba32(0x80, 0x80, 0x80));

        var hex = await _sut.ExtractAsync(png);

        Assert.Equal("#808080", hex);
    }

    [Fact]
    public async Task Returns_null_when_every_pixel_is_transparent()
    {
        await using var png = CreatePng(32, 32, new Rgba32(0, 0, 0, 0));

        Assert.Null(await _sut.ExtractAsync(png));
    }

    [Fact]
    public async Task Returns_null_for_a_file_that_is_not_an_image()
    {
        await using var garbage = new MemoryStream("no soy una imagen"u8.ToArray());

        Assert.Null(await _sut.ExtractAsync(garbage));
    }

    [Fact]
    public async Task Handles_images_larger_than_the_sample_size()
    {
        await using var png = CreatePng(1200, 1800, new Rgba32(0x1E, 0x5F, 0x9A));

        var hex = await _sut.ExtractAsync(png);

        Assert.Equal("#1e5f9a", hex);
    }

    private static void FillRect(Image<Rgba32> image, int x, int y, int width, int height, Rgba32 color)
    {
        image.ProcessPixelRows(accessor =>
        {
            for (var row = y; row < y + height; row++)
            {
                var span = accessor.GetRowSpan(row);
                for (var col = x; col < x + width; col++) span[col] = color;
            }
        });
    }

    private static MemoryStream CreatePng(int width, int height, Rgba32 fill, Action<Image<Rgba32>>? paint = null)
    {
        using var image = new Image<Rgba32>(width, height, fill);
        paint?.Invoke(image);

        var stream = new MemoryStream();
        image.Save(stream, new PngEncoder());
        stream.Position = 0;
        return stream;
    }
}
