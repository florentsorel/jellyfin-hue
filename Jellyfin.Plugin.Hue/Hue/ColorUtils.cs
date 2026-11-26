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

    /// <summary>
    /// Converts Hue CIE 1931 XY coordinates to a Hex color string (e.g. "#FFB366").
    /// </summary>
    /// <param name="xy">The XY coordinates.</param>
    /// <returns>A hex color string.</returns>
    public static string XyToHex(HueXyPoint? xy)
    {
        if (xy == null || xy.Y <= 0.0001)
        {
            return "#FFB366";
        }

        double x = xy.X;
        double y = xy.Y;
        double z = 1.0 - x - y;

        double bigY = 1.0;
        double bigX = (bigY / y) * x;
        double bigZ = (bigY / y) * z;

        // Wide RGB D65 reverse matrix
        double r = (bigX * 1.656492) - (bigY * 0.354851) - (bigZ * 0.255038);
        double g = (-bigX * 0.707196) + (bigY * 1.655397) + (bigZ * 0.036152);
        double b = (bigX * 0.051713) - (bigY * 0.121364) + (bigZ * 1.011530);

        r = Math.Max(0, r);
        g = Math.Max(0, g);
        b = Math.Max(0, b);

        // Normalize if any component exceeds 1.0
        double max = Math.Max(r, Math.Max(g, b));
        if (max > 1.0)
        {
            r /= max;
            g /= max;
            b /= max;
        }

        // Reverse gamma correction
        r = r <= 0.0031308 ? 12.92 * r : ((1.0 + 0.055) * Math.Pow(r, 1.0 / 2.4)) - 0.055;
        g = g <= 0.0031308 ? 12.92 * g : ((1.0 + 0.055) * Math.Pow(g, 1.0 / 2.4)) - 0.055;
        b = b <= 0.0031308 ? 12.92 * b : ((1.0 + 0.055) * Math.Pow(b, 1.0 / 2.4)) - 0.055;

        // Boost vibrancy for UI preview so colors aren't overly dim
        double maxGamma = Math.Max(r, Math.Max(g, b));
        if (maxGamma > 0.01 && maxGamma < 0.9)
        {
            double boost = 0.95 / maxGamma;
            r = Math.Min(1.0, r * boost);
            g = Math.Min(1.0, g * boost);
            b = Math.Min(1.0, b * boost);
        }

        int ri = Math.Clamp((int)Math.Round(r * 255), 0, 255);
        int gi = Math.Clamp((int)Math.Round(g * 255), 0, 255);
        int bi = Math.Clamp((int)Math.Round(b * 255), 0, 255);

        return $"#{ri:X2}{gi:X2}{bi:X2}";
    }

    /// <summary>
    /// Converts a Hue color temperature in Mirek (153 - 500) to an approximate Hex color string.
    /// </summary>
    /// <param name="mirek">The Mirek value.</param>
    /// <returns>A hex color string.</returns>
    public static string MirekToHex(int mirek)
    {
        int clamped = Math.Clamp(mirek, 153, 500);
        double kelvin = 1000000.0 / clamped;
        double temp = kelvin / 100.0;

        double r;
        double g;
        double b;

        if (temp <= 66)
        {
            r = 255;
            g = (99.4708025861 * Math.Log(temp)) - 161.1195681661;
            b = temp <= 19 ? 0 : (138.5177312231 * Math.Log(temp - 10)) - 305.0447927307;
        }
        else
        {
            r = 329.698727446 * Math.Pow(temp - 60, -0.1332047592);
            g = 288.1221695283 * Math.Pow(temp - 60, -0.0755148492);
            b = 255;
        }

        int ri = Math.Clamp((int)Math.Round(r), 0, 255);
        int gi = Math.Clamp((int)Math.Round(g), 0, 255);
        int bi = Math.Clamp((int)Math.Round(b), 0, 255);

        return $"#{ri:X2}{gi:X2}{bi:X2}";
    }
}
