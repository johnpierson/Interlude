using Autodesk.DesignScript.Runtime;
using Interlude.Model;

namespace Interlude.Theming;

/// <summary>Which palette a form uses.</summary>
[IsVisibleInDynamoLibrary(false)]
public enum AppearanceMode
{
    /// <summary>Follow the Windows app theme setting.</summary>
    Auto,

    Light,

    Dark,
}

/// <summary>How tightly a form packs its controls.</summary>
[IsVisibleInDynamoLibrary(false)]
public enum ThemeDensity
{
    /// <summary>Tight spacing, for long forms on small screens.</summary>
    Compact,

    /// <summary>The default.</summary>
    Comfortable,

    /// <summary>Generous spacing.</summary>
    Spacious,
}

/// <summary>
/// The colour set for one mode. These are pure values: the WPF layer turns them into brushes,
/// and it does so inside the form window's own resource dictionary — never the host
/// application's, because Interlude is a guest in Revit's and Dynamo's process.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
public sealed record ThemePalette
{
    /// <summary>The stock light palette.</summary>
    public static readonly ThemePalette Light = new()
    {
        Background = RgbColor.Parse("#FFFFFF"),
        Surface = RgbColor.Parse("#F6F7F9"),
        SurfaceAlt = RgbColor.Parse("#EDEFF3"),
        Border = RgbColor.Parse("#D6DAE1"),
        BorderStrong = RgbColor.Parse("#B4BAC5"),
        Foreground = RgbColor.Parse("#1B1F24"),
        ForegroundMuted = RgbColor.Parse("#5C6673"),
        ForegroundDisabled = RgbColor.Parse("#9AA3AF"),
        ControlBackground = RgbColor.Parse("#FFFFFF"),
        ControlBackgroundHover = RgbColor.Parse("#F1F3F6"),
        ControlBackgroundDisabled = RgbColor.Parse("#F0F1F3"),
        Accent = RgbColor.Parse("#2F6FEB"),
        AccentHover = RgbColor.Parse("#2A62D0"),
        AccentForeground = RgbColor.Parse("#FFFFFF"),
        Error = RgbColor.Parse("#C42B1C"),
        Warning = RgbColor.Parse("#9A6700"),
        Success = RgbColor.Parse("#1A7F37"),
        Shadow = new RgbColor(0, 0, 0, 40),
    };

    /// <summary>The stock dark palette, tuned to sit comfortably over Revit's dark theme.</summary>
    public static readonly ThemePalette Dark = new()
    {
        Background = RgbColor.Parse("#1E2126"),
        Surface = RgbColor.Parse("#262A31"),
        SurfaceAlt = RgbColor.Parse("#2E333B"),
        Border = RgbColor.Parse("#3B4149"),
        BorderStrong = RgbColor.Parse("#525A65"),
        Foreground = RgbColor.Parse("#E8EAED"),
        ForegroundMuted = RgbColor.Parse("#A2AAB5"),
        ForegroundDisabled = RgbColor.Parse("#6B7480"),
        ControlBackground = RgbColor.Parse("#2B3038"),
        ControlBackgroundHover = RgbColor.Parse("#343A44"),
        ControlBackgroundDisabled = RgbColor.Parse("#272B31"),
        Accent = RgbColor.Parse("#4C8DFF"),
        AccentHover = RgbColor.Parse("#679EFF"),
        AccentForeground = RgbColor.Parse("#0E1116"),
        Error = RgbColor.Parse("#FF6B5E"),
        Warning = RgbColor.Parse("#E3B341"),
        Success = RgbColor.Parse("#3FB950"),
        Shadow = new RgbColor(0, 0, 0, 120),
    };

    /// <summary>The window's own backdrop.</summary>
    public RgbColor Background { get; init; }

    /// <summary>Panels and cards raised above the backdrop.</summary>
    public RgbColor Surface { get; init; }

    /// <summary>Secondary panels, such as alternating rows or a tab strip.</summary>
    public RgbColor SurfaceAlt { get; init; }

    public RgbColor Border { get; init; }

    /// <summary>Borders that need to read as an edge, such as a focused control.</summary>
    public RgbColor BorderStrong { get; init; }

    public RgbColor Foreground { get; init; }

    /// <summary>Help text, descriptions and other secondary copy.</summary>
    public RgbColor ForegroundMuted { get; init; }

    public RgbColor ForegroundDisabled { get; init; }

    public RgbColor ControlBackground { get; init; }

    public RgbColor ControlBackgroundHover { get; init; }

    public RgbColor ControlBackgroundDisabled { get; init; }

    public RgbColor Accent { get; init; }

    public RgbColor AccentHover { get; init; }

    /// <summary>Text drawn on top of <see cref="Accent"/>.</summary>
    public RgbColor AccentForeground { get; init; }

    public RgbColor Error { get; init; }

    public RgbColor Warning { get; init; }

    public RgbColor Success { get; init; }

    public RgbColor Shadow { get; init; }

    /// <summary>
    /// Recolours the palette around a new accent, picking black or white accent text by
    /// contrast so a branded accent stays readable without the author choosing both.
    /// </summary>
    public ThemePalette WithAccent(RgbColor accent)
    {
        RgbColor hover = accent.Luminance > 0.5
            ? Darken(accent, 0.12)
            : Lighten(accent, 0.12);

        return this with
        {
            Accent = accent,
            AccentHover = hover,
            AccentForeground = accent.Luminance > 0.55 ? RgbColor.Parse("#101418") : RgbColor.White,
        };
    }

    private static RgbColor Lighten(RgbColor color, double amount) => new(
        (byte)(color.Red + ((255 - color.Red) * amount)),
        (byte)(color.Green + ((255 - color.Green) * amount)),
        (byte)(color.Blue + ((255 - color.Blue) * amount)),
        color.Alpha);

    private static RgbColor Darken(RgbColor color, double amount) => new(
        (byte)(color.Red * (1 - amount)),
        (byte)(color.Green * (1 - amount)),
        (byte)(color.Blue * (1 - amount)),
        color.Alpha);
}

/// <summary>
/// Everything about how a form looks, as data. Nothing here touches WPF, which keeps themes
/// serializable, diffable and testable alongside the form itself.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
public sealed record ThemeDefinition
{
    /// <summary>The stock theme: follow the system light/dark setting at comfortable density.</summary>
    public static readonly ThemeDefinition Default = new();

    public AppearanceMode Mode { get; init; } = AppearanceMode.Auto;

    /// <summary>Replaces the accent colour in whichever palette is active.</summary>
    public RgbColor? Accent { get; init; }

    /// <summary>Replaces the stock light palette outright.</summary>
    public ThemePalette? LightPalette { get; init; }

    /// <summary>Replaces the stock dark palette outright.</summary>
    public ThemePalette? DarkPalette { get; init; }

    public double CornerRadius { get; init; } = 4d;

    public double FontSize { get; init; } = 13d;

    /// <summary>Null uses the host's UI font.</summary>
    public string? FontFamily { get; init; }

    public ThemeDensity Density { get; init; } = ThemeDensity.Comfortable;

    /// <summary>Suppresses transitions, for users who ask the OS for reduced motion.</summary>
    public bool ReducedMotion { get; init; }

    /// <summary>
    /// Width of the label column beside each input. Set to 0 to stack labels above their controls,
    /// which is the better shape for narrow forms.
    /// </summary>
    public double LabelWidth { get; init; } = 130d;

    /// <summary>Resolves the palette for a mode, applying any accent or palette overrides.</summary>
    /// <param name="systemPrefersDark">
    /// What <see cref="AppearanceMode.Auto"/> should resolve to. The caller supplies this because
    /// reading the Windows theme is a host concern, not a model concern.
    /// </param>
    public ThemePalette ResolvePalette(bool systemPrefersDark)
    {
        bool useDark = Mode switch
        {
            AppearanceMode.Dark => true,
            AppearanceMode.Light => false,
            _ => systemPrefersDark,
        };

        ThemePalette palette = useDark
            ? DarkPalette ?? ThemePalette.Dark
            : LightPalette ?? ThemePalette.Light;

        return Accent.HasValue ? palette.WithAccent(Accent.Value) : palette;
    }

    /// <summary>The spacing scale implied by <see cref="Density"/>, in pixels.</summary>
    public double BaseSpacing => Density switch
    {
        ThemeDensity.Compact => 4d,
        ThemeDensity.Spacious => 12d,
        _ => 8d,
    };

    /// <summary>The minimum height of an interactive control, in pixels.</summary>
    public double ControlHeight => Density switch
    {
        ThemeDensity.Compact => 24d,
        ThemeDensity.Spacious => 34d,
        _ => 28d,
    };
}
