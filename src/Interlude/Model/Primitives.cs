using Autodesk.DesignScript.Runtime;

namespace Interlude.Model;

/// <summary>Four-sided spacing, in device-independent pixels.</summary>
[IsVisibleInDynamoLibrary(false)]
public readonly record struct Edges
{
    /// <summary>No spacing on any side.</summary>
    public static readonly Edges Zero = default;

    public Edges(double left, double top, double right, double bottom)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }

    public double Left { get; init; }

    public double Top { get; init; }

    public double Right { get; init; }

    public double Bottom { get; init; }

    /// <summary>The same spacing on all four sides.</summary>
    public static Edges Uniform(double amount) => new(amount, amount, amount, amount);

    /// <summary>Horizontal and vertical spacing.</summary>
    public static Edges Symmetric(double horizontal, double vertical)
        => new(horizontal, vertical, horizontal, vertical);
}

/// <summary>Horizontal placement of an element inside the space it is given.</summary>
[IsVisibleInDynamoLibrary(false)]
public enum HorizontalPlacement
{
    Stretch,
    Left,
    Center,
    Right,
}

/// <summary>Vertical placement of an element inside the space it is given.</summary>
[IsVisibleInDynamoLibrary(false)]
public enum VerticalPlacement
{
    Stretch,
    Top,
    Center,
    Bottom,
}

/// <summary>Layout direction for stacks, radio groups and separators.</summary>
[IsVisibleInDynamoLibrary(false)]
public enum LayoutOrientation
{
    Vertical,
    Horizontal,
}

/// <summary>Which edge of a dock container an element attaches to.</summary>
[IsVisibleInDynamoLibrary(false)]
public enum DockSide
{
    Left,
    Top,
    Right,
    Bottom,
}

/// <summary>Text weight, kept independent of WPF's open-ended <c>FontWeight</c> struct.</summary>
[IsVisibleInDynamoLibrary(false)]
public enum TextWeight
{
    Normal,
    Medium,
    SemiBold,
    Bold,
}

/// <summary>How an image fills the box it is given.</summary>
[IsVisibleInDynamoLibrary(false)]
public enum ImageFit
{
    /// <summary>Scale to fit, preserving aspect ratio.</summary>
    Contain,

    /// <summary>Scale to fill, preserving aspect ratio and cropping the overflow.</summary>
    Cover,

    /// <summary>Stretch to fill, ignoring aspect ratio.</summary>
    Fill,

    /// <summary>Draw at natural size.</summary>
    None,
}

/// <summary>What pressing a button does.</summary>
[IsVisibleInDynamoLibrary(false)]
public enum ButtonAction
{
    /// <summary>Validates and closes the form as submitted.</summary>
    Submit,

    /// <summary>Closes the form as cancelled, returning defaults.</summary>
    Cancel,

    /// <summary>Closes the form as submitted and reports the button's tag as the clicked button.</summary>
    SubmitWithTag,

    /// <summary>Opens <see cref="ButtonElement.Url"/> in the default browser; the form stays open.</summary>
    OpenUrl,

    /// <summary>Resets every input to its default value; the form stays open.</summary>
    Reset,
}

/// <summary>How one row or column of a grid is sized.</summary>
[IsVisibleInDynamoLibrary(false)]
public enum GridTrackKind
{
    /// <summary>Sized to content.</summary>
    Auto,

    /// <summary>A fixed number of pixels.</summary>
    Pixel,

    /// <summary>A share of the leftover space.</summary>
    Star,
}

/// <summary>One row or column definition of a <see cref="GridElement"/>.</summary>
[IsVisibleInDynamoLibrary(false)]
public readonly record struct GridTrack
{
    /// <summary>A track sized to its content.</summary>
    public static readonly GridTrack Auto = new(GridTrackKind.Auto, 0d);

    /// <summary>A track taking one share of the leftover space.</summary>
    public static readonly GridTrack Star = new(GridTrackKind.Star, 1d);

    public GridTrack(GridTrackKind kind, double value)
    {
        Kind = kind;
        Value = value;
    }

    public GridTrackKind Kind { get; init; }

    /// <summary>Pixels for <see cref="GridTrackKind.Pixel"/>, share count for <see cref="GridTrackKind.Star"/>.</summary>
    public double Value { get; init; }

    /// <summary>A track of a fixed pixel width or height.</summary>
    public static GridTrack Pixels(double amount) => new(GridTrackKind.Pixel, amount);

    /// <summary>A track taking <paramref name="shares"/> shares of the leftover space.</summary>
    public static GridTrack Stars(double shares) => new(GridTrackKind.Star, shares);

    /// <summary>
    /// Parses the compact grid syntax used by the node API: <c>auto</c>, <c>*</c>, <c>2*</c>
    /// or a plain number of pixels. Unrecognised text falls back to <see cref="Auto"/>.
    /// </summary>
    public static GridTrack Parse(string? text)
    {
        string token = (text ?? string.Empty).Trim();

        if (token.Length == 0 || token.Equals("auto", System.StringComparison.OrdinalIgnoreCase))
        {
            return Auto;
        }

        if (token.EndsWith("*", System.StringComparison.Ordinal))
        {
            string sharePart = token.Substring(0, token.Length - 1);
            if (sharePart.Length == 0)
            {
                return Star;
            }

            return double.TryParse(sharePart, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double shares)
                ? Stars(shares)
                : Star;
        }

        return double.TryParse(token, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out double pixels)
            ? Pixels(pixels)
            : Auto;
    }
}
