using System;
using System.Globalization;
using Autodesk.DesignScript.Runtime;

namespace Interlude.Model;

/// <summary>
/// A colour, as a plain value.
///
/// Interlude defines its own colour type rather than using <c>System.Windows.Media.Color</c> so
/// that the model, the condition engine and the JSON schema stay free of WPF, and rather than
/// using <c>DSCore.Color</c> so that the package keeps its zero-dependency promise. Conversion
/// to a WPF colour happens once, in the renderer.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
public readonly record struct RgbColor
{
    /// <summary>Fully transparent black; also the value of <c>default</c>.</summary>
    public static readonly RgbColor Transparent = new(0, 0, 0, 0);

    public static readonly RgbColor Black = new(0, 0, 0);

    public static readonly RgbColor White = new(255, 255, 255);

    public RgbColor(byte red, byte green, byte blue, byte alpha = 255)
    {
        Red = red;
        Green = green;
        Blue = blue;
        Alpha = alpha;
    }

    public byte Red { get; init; }

    public byte Green { get; init; }

    public byte Blue { get; init; }

    /// <summary>255 is fully opaque.</summary>
    public byte Alpha { get; init; }

    /// <summary>
    /// Parses <c>#RGB</c>, <c>#RRGGBB</c>, <c>#AARRGGBB</c> or <c>#RRGGBBAA</c>, with or without
    /// the leading hash. Eight-digit forms are read as <c>#AARRGGBB</c> to match WPF and Revit;
    /// pass <paramref name="alphaLast"/> for the CSS ordering.
    /// </summary>
    public static bool TryParse(string? text, out RgbColor color, bool alphaLast = false)
    {
        color = default;

        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        string hex = text!.Trim().TrimStart('#');

        // #RGB shorthand expands each digit ("#0af" -> "#00aaff").
        if (hex.Length == 3 || hex.Length == 4)
        {
            char[] expanded = new char[hex.Length * 2];
            for (int i = 0; i < hex.Length; i++)
            {
                expanded[i * 2] = hex[i];
                expanded[(i * 2) + 1] = hex[i];
            }

            hex = new string(expanded);
        }

        if (hex.Length != 6 && hex.Length != 8)
        {
            return false;
        }

        if (!uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint packed))
        {
            return false;
        }

        if (hex.Length == 6)
        {
            color = new RgbColor(
                (byte)((packed >> 16) & 0xFF),
                (byte)((packed >> 8) & 0xFF),
                (byte)(packed & 0xFF));
            return true;
        }

        color = alphaLast
            ? new RgbColor(
                (byte)((packed >> 24) & 0xFF),
                (byte)((packed >> 16) & 0xFF),
                (byte)((packed >> 8) & 0xFF),
                (byte)(packed & 0xFF))
            : new RgbColor(
                (byte)((packed >> 16) & 0xFF),
                (byte)((packed >> 8) & 0xFF),
                (byte)(packed & 0xFF),
                (byte)((packed >> 24) & 0xFF));
        return true;
    }

    /// <summary>Parses a hex colour, throwing when the text is not a colour.</summary>
    public static RgbColor Parse(string text)
        => TryParse(text, out RgbColor color)
            ? color
            : throw new FormatException($"'{text}' is not a recognised colour. Use #RGB, #RRGGBB or #AARRGGBB.");

    /// <summary>Blends this colour over an opaque background, producing an opaque result.</summary>
    public RgbColor Over(RgbColor background)
    {
        if (Alpha == 255)
        {
            return this;
        }

        double a = Alpha / 255d;
        return new RgbColor(
            (byte)Math.Round((Red * a) + (background.Red * (1 - a))),
            (byte)Math.Round((Green * a) + (background.Green * (1 - a))),
            (byte)Math.Round((Blue * a) + (background.Blue * (1 - a))));
    }

    /// <summary>
    /// Perceived brightness, 0 (black) to 1 (white). Used to pick readable foreground text
    /// over an arbitrary accent colour.
    /// </summary>
    public double Luminance => ((0.299 * Red) + (0.587 * Green) + (0.114 * Blue)) / 255d;

    /// <summary>Formats as <c>#RRGGBB</c>, or <c>#AARRGGBB</c> when not fully opaque.</summary>
    public string ToHex()
        => Alpha == 255
            ? string.Format(CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}", Red, Green, Blue)
            : string.Format(CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}{3:X2}", Alpha, Red, Green, Blue);

    public override string ToString() => ToHex();
}
