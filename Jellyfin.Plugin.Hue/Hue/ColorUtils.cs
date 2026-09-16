using System;
using System.Globalization;
using Jellyfin.Plugin.Hue.Hue.Models;

namespace Jellyfin.Plugin.Hue.Hue;

/// <summary>
/// Utility methods for color conversions (Hex / RGB to Philips Hue CIE 1931 XY).
/// </summary>
public static class ColorUtils
{
    /// <summary>
    /// Converts a Hex color code (e.g. #FFB366 or FFB366) to Hue CIE 1931 XY coordinates.
    /// </summary>
    /// <param name="hex">The hex color string.</param>
    /// <returns>A <see cref="HueXyPoint"/> containing x and y coordinates.</returns>
    public static HueXyPoint HexToXy(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
        {
            // Default warm white
            return new HueXyPoint { X = 0.4578, Y = 0.41 };
        }

        var cleaned = hex.Trim().TrimStart('#');
        if (cleaned.Length != 6)
        {
            return new HueXyPoint { X = 0.4578, Y = 0.41 };
        }

        try
        {
            var r = Convert.ToInt32(cleaned.Substring(0, 2), 16);
            var g = Convert.ToInt32(cleaned.Substring(2, 2), 16);
            var b = Convert.ToInt32(cleaned.Substring(4, 2), 16);

            return RgbToXy(r, g, b);
        }
        catch (Exception ex) when (ex is FormatException or OverflowException or ArgumentOutOfRangeException)
        {
            return new HueXyPoint { X = 0.4578, Y = 0.41 };
        }
    }

    /// <summary>
    /// Converts RGB 0-255 components to Hue CIE 1931 XY coordinates.
    /// </summary>
    /// <param name="red">Red component (0-255).</param>
    /// <param name="green">Green component (0-255).</param>
    /// <param name="blue">Blue component (0-255).</param>
    /// <returns>A <see cref="HueXyPoint"/> representing the color.</returns>
    public static HueXyPoint RgbToXy(int red, int green, int blue)
    {
        // Gamma correction
        var r = (red > 0.04045) ? Math.Pow(((red / 255.0) + 0.055) / (1.0 + 0.055), 2.4) : (red / 255.0 / 12.92);
        var g = (green > 0.04045) ? Math.Pow(((green / 255.0) + 0.055) / (1.0 + 0.055), 2.4) : (green / 255.0 / 12.92);
        var b = (blue > 0.04045) ? Math.Pow(((blue / 255.0) + 0.055) / (1.0 + 0.055), 2.4) : (blue / 255.0 / 12.92);

        // Wide RGB D65 conversion
        var x = (r * 0.664511) + (g * 0.154324) + (b * 0.162028);
        var y = (r * 0.283881) + (g * 0.668433) + (b * 0.047685);
        var z = (r * 0.000088) + (g * 0.072310) + (b * 0.986039);

        var sum = x + y + z;
        if (Math.Abs(sum) < 0.000001)
        {
            return new HueXyPoint { X = 0.3127, Y = 0.3290 }; // Standard D65 white
        }

        var cx = Math.Round(x / sum, 4);
        var cy = Math.Round(y / sum, 4);

        return new HueXyPoint { X = cx, Y = cy };
    }

    /// <summary>
    /// Converts a hex color string to Hue color temperature in Mirek (153 - 500),
    /// or null if the color is not a white temperature.
    /// </summary>
    /// <param name="hex">The hex color string.</param>
    /// <returns>The Mirek value (153-500) or null.</returns>
    public static int? HexToMirek(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
        {
            return 370; // Default 2700K
        }

        var cleaned = hex.Trim().ToUpperInvariant();
        if (!cleaned.StartsWith('#'))
        {
            cleaned = "#" + cleaned;
        }

        return cleaned switch
        {
            "#FF932C" => 454, // ~2200K Bougie / Amber
            "#FFAF66" or "#FFB366" => 370, // ~2700K Warm
            "#FFE4B5" => 333, // ~3000K Soft
            "#FFF1E0" => 250, // ~4000K Neutral
            "#C8E0FF" => 153, // ~6500K Cool
            "#FFFFFF" => 250, // ~4000K White
            _ => CalculateMirekFromRgb(cleaned),
        };
    }

    private static int? CalculateMirekFromRgb(string hex)
    {
        if (hex.Length != 7)
        {
            return null;
        }

        if (!int.TryParse(hex.AsSpan(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var r) ||
            !int.TryParse(hex.AsSpan(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var g) ||
            !int.TryParse(hex.AsSpan(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var b))
        {
            return null;
        }

        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var delta = max - min;

        double sat = max == 0 ? 0 : (double)delta / max;
        if (sat > 0.85)
        {
            return null;
        }

        double ratio = (double)(b + 1) / (r + 1);
        var mirek = (int)Math.Round(500 - ((ratio - 0.2) / 1.3 * (500 - 153)));
        return Math.Clamp(mirek, 153, 500);
    }
}
