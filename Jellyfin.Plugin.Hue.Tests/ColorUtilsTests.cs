using System;
using Jellyfin.Plugin.Hue.Hue;
using Xunit;

namespace Jellyfin.Plugin.Hue.Tests;

public class ColorUtilsTests
{
    [Theory]
    [InlineData("#FF0000", 0.67, 0.32)] // Pure Red in XY space
    [InlineData("#00FF00", 0.21, 0.71)] // Pure Green in XY space
    [InlineData("#0000FF", 0.15, 0.06)] // Pure Blue in XY space
    [InlineData("#FFFFFF", 0.31, 0.33)] // White D65
    public void HexToXy_ShouldReturnExpectedCoordinates(string hex, double expectedXApprox, double expectedYApprox)
    {
        var xy = ColorUtils.HexToXy(hex);
        Assert.InRange(xy.X, expectedXApprox - 0.05, expectedXApprox + 0.05);
        Assert.InRange(xy.Y, expectedYApprox - 0.05, expectedYApprox + 0.05);
    }

    [Fact]
    public void HexToXy_NullOrInvalidHex_ReturnsWarmWhiteDefault()
    {
        var xyNull = ColorUtils.HexToXy(null);
        var xyInvalid = ColorUtils.HexToXy("not-a-hex");

        Assert.Equal(0.4578, xyNull.X);
        Assert.Equal(0.41, xyNull.Y);

        Assert.Equal(0.4578, xyInvalid.X);
        Assert.Equal(0.41, xyInvalid.Y);
    }

    [Theory]
    [InlineData("#FF932C", 454)]
    [InlineData("#FFAF66", 370)]
    [InlineData("#FFB366", 370)]
    [InlineData("#FFE4B5", 333)]
    [InlineData("#FFF1E0", 250)]
    [InlineData("#C8E0FF", 153)]
    [InlineData("#FFFFFF", 250)]
    public void HexToMirek_Presets_ReturnExpectedMirek(string hex, int expectedMirek)
    {
        var mirek = ColorUtils.HexToMirek(hex);
        Assert.NotNull(mirek);
        Assert.Equal(expectedMirek, mirek.Value);
    }

    [Fact]
    public void HexToMirek_NullOrEmpty_ReturnsDefaultWarmWhite()
    {
        Assert.Equal(370, ColorUtils.HexToMirek(null));
        Assert.Equal(370, ColorUtils.HexToMirek(string.Empty));
    }

    [Fact]
    public void HexToMirek_VibrantColor_ReturnsNull()
    {
        Assert.Null(ColorUtils.HexToMirek("#FF0000"));
        Assert.Null(ColorUtils.HexToMirek("#00FF00"));
        Assert.Null(ColorUtils.HexToMirek("#0000FF"));
    }
}

