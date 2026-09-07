using MyBooksKanban.Application.Colors;
using Xunit;

namespace MyBooksKanban.Tests.Application;

public class SpinePaletteTests
{
    [Fact]
    public void Resolve_uses_the_cover_dominant_color_when_present()
    {
        var (accent, _, _) = SpinePalette.Resolve("#C0392B", "Ciencia ficción");

        Assert.Equal("#c0392b", accent);
    }

    [Fact]
    public void Resolve_falls_back_to_the_category_color_when_there_is_no_cover()
    {
        var (accent, _, _) = SpinePalette.Resolve(null, "Ensayo");
        var (expected, _, _) = SpinePalette.Resolve("   ", "Ensayo");

        Assert.Equal(expected, accent);
        Assert.StartsWith("#", accent);
        Assert.Equal(7, accent.Length);
    }

    [Fact]
    public void Resolve_falls_back_to_the_category_color_when_the_stored_value_is_invalid()
    {
        var (accent, _, _) = SpinePalette.Resolve("no-es-un-color", "Ensayo");
        var (expected, _, _) = SpinePalette.Resolve(null, "Ensayo");

        Assert.Equal(expected, accent);
    }

    [Fact]
    public void Category_colors_are_stable_and_distinct()
    {
        Assert.Equal(SpinePalette.FromCategory("Ensayo"), SpinePalette.FromCategory("Ensayo"));
        Assert.NotEqual(SpinePalette.FromCategory("Ensayo"), SpinePalette.FromCategory("Novela histórica"));
    }

    [Fact]
    public void Category_color_falls_back_when_the_name_is_empty()
    {
        Assert.Equal(SpinePalette.FallbackAccent, SpinePalette.FromCategory(null));
        Assert.Equal(SpinePalette.FallbackAccent, SpinePalette.FromCategory("  "));
    }

    [Theory]
    [InlineData("#000000")]
    [InlineData("#1e5f9a")]
    [InlineData("#4a2c12")]
    public void Dark_covers_get_light_ink(string accent)
    {
        var (_, ink, halo) = SpinePalette.Resolve(accent, "Ensayo");

        Assert.Equal(SpinePalette.LightInk, ink);
        Assert.Equal(SpinePalette.LightInkHalo, halo);
    }

    [Theory]
    [InlineData("#ffffff")]
    [InlineData("#f4e7c3")]
    [InlineData("#ffd400")]
    public void Light_covers_get_dark_ink(string accent)
    {
        var (_, ink, halo) = SpinePalette.Resolve(accent, "Ensayo");

        Assert.Equal(SpinePalette.DarkInk, ink);
        Assert.Equal(SpinePalette.DarkInkHalo, halo);
    }

    [Theory]
    [InlineData("#abc", 0xAA, 0xBB, 0xCC)]
    [InlineData("abc", 0xAA, 0xBB, 0xCC)]
    [InlineData("#A1B2C3", 0xA1, 0xB2, 0xC3)]
    [InlineData(" a1b2c3 ", 0xA1, 0xB2, 0xC3)]
    public void TryParseHex_accepts_the_supported_notations(string input, int r, int g, int b)
    {
        Assert.True(SpinePalette.TryParseHex(input, out var rgb));
        Assert.Equal(((byte)r, (byte)g, (byte)b), rgb);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("#12345")]
    [InlineData("#gggggg")]
    [InlineData("rgb(1,2,3)")]
    public void TryParseHex_rejects_anything_else(string? input)
    {
        Assert.False(SpinePalette.TryParseHex(input, out _));
    }
}
