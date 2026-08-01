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

/// <summary>
/// Which built-in pair of palettes a theme starts from.
///
/// This is a name rather than a copy on purpose. A theme that carried the palettes themselves
/// would serialise every one of the eighteen colours in both modes — three hundred lines of JSON
/// in front of a two-field form — and a form checked into a repository would show a palette diff
/// every time the built-in colours were tuned.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
public enum ThemePreset
{
    /// <summary>Conventional light and dark: hairline outlines, soft greys, one blue accent.</summary>
    Classic,

    /// <summary>Neubrutalist: heavy outlines, flat loud colour, hard shadows.</summary>
    Neubrutalist,
}

/// <summary>The shape of a control's corners.</summary>
[IsVisibleInDynamoLibrary(false)]
public enum ControlShape
{
    /// <summary>Corners rounded by the theme's corner radius.</summary>
    Rounded,

    /// <summary>Fully rounded ends, so a control reads as a pill or a capsule.</summary>
    Pill,

    /// <summary>Square corners.</summary>
    Square,
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

    /// <summary>
    /// The neubrutalist light palette: warm paper, pure black ink, and one loud accent.
    ///
    /// Every line in this style is the same black, which is the point — neubrutalism gets its
    /// structure from outline and offset rather than from a hierarchy of greys, so
    /// <see cref="Border"/> and <see cref="BorderStrong"/> deliberately match.
    /// </summary>
    public static readonly ThemePalette Neo = new()
    {
        Background = RgbColor.Parse("#FFF4E0"),
        Surface = RgbColor.Parse("#FFFFFF"),
        SurfaceAlt = RgbColor.Parse("#FFE55C"),
        Border = RgbColor.Parse("#000000"),
        BorderStrong = RgbColor.Parse("#000000"),
        Foreground = RgbColor.Parse("#000000"),
        ForegroundMuted = RgbColor.Parse("#4A4238"),
        ForegroundDisabled = RgbColor.Parse("#9A9186"),
        ControlBackground = RgbColor.Parse("#FFFFFF"),
        ControlBackgroundHover = RgbColor.Parse("#FFE55C"),
        ControlBackgroundDisabled = RgbColor.Parse("#E8E2D6"),
        Accent = RgbColor.Parse("#FF5C8A"),
        AccentHover = RgbColor.Parse("#FF87A8"),
        AccentForeground = RgbColor.Parse("#000000"),
        Error = RgbColor.Parse("#E5383B"),
        Warning = RgbColor.Parse("#FF8A00"),
        Success = RgbColor.Parse("#00B36B"),
        Shadow = new RgbColor(0, 0, 0, 255),
    };

    /// <summary>
    /// The neubrutalist dark palette — the light one inverted rather than dimmed. Black borders
    /// and black shadows vanish on a dark backdrop, so both become bone white and the accent
    /// moves to a colour that survives being surrounded by it.
    /// </summary>
    public static readonly ThemePalette NeoDark = new()
    {
        Background = RgbColor.Parse("#121214"),
        Surface = RgbColor.Parse("#1E1E22"),
        SurfaceAlt = RgbColor.Parse("#2C2C33"),
        Border = RgbColor.Parse("#F2F2F0"),
        BorderStrong = RgbColor.Parse("#FFFFFF"),
        Foreground = RgbColor.Parse("#F7F7F5"),
        ForegroundMuted = RgbColor.Parse("#AFAFB8"),
        ForegroundDisabled = RgbColor.Parse("#6A6A72"),
        ControlBackground = RgbColor.Parse("#1E1E22"),
        ControlBackgroundHover = RgbColor.Parse("#3A3A44"),
        ControlBackgroundDisabled = RgbColor.Parse("#26262A"),
        Accent = RgbColor.Parse("#C7F464"),
        AccentHover = RgbColor.Parse("#D9FF8C"),
        AccentForeground = RgbColor.Parse("#121214"),
        Error = RgbColor.Parse("#FF6B6B"),
        Warning = RgbColor.Parse("#FFC145"),
        Success = RgbColor.Parse("#5CE1A0"),
        Shadow = new RgbColor(242, 242, 240, 255),
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
    /// <summary>
    /// The theme a form gets when nobody supplies one: neubrutalist, light, at comfortable density.
    ///
    /// A default that looks like nothing in particular is a decision too, and it is the wrong one
    /// for a package whose whole job is the dialog. This is the loud option on purpose. Every part
    /// of it is a property below, so <c>Theme.Light</c> and <c>Theme.Dark</c> — which take the
    /// property defaults rather than this — stay the quiet way out.
    ///
    /// Light rather than <see cref="AppearanceMode.Auto"/>: this palette is built around cream and
    /// black, and a form that flips to the inverted one because the machine happens to be set to
    /// dark is not the same design. <c>Theme.System</c> is the version that follows Windows.
    /// </summary>
    public static readonly ThemeDefinition Default = new()
    {
        Preset = ThemePreset.Neubrutalist,
        Mode = AppearanceMode.Light,
        Shape = ControlShape.Square,
        BorderWidth = 2d,
        ShadowOffset = 4d,
        HeavyText = true,
        UppercaseHeaders = true,
        HeaderTracking = 0.05d,
    };

    public AppearanceMode Mode { get; init; } = AppearanceMode.Auto;

    /// <summary>Which built-in pair of palettes this theme starts from.</summary>
    public ThemePreset Preset { get; init; } = ThemePreset.Classic;

    /// <summary>Replaces the accent colour in whichever palette is active.</summary>
    public RgbColor? Accent { get; init; }

    /// <summary>Replaces the preset's light palette outright.</summary>
    public ThemePalette? LightPalette { get; init; }

    /// <summary>Replaces the preset's dark palette outright.</summary>
    public ThemePalette? DarkPalette { get; init; }

    public double CornerRadius { get; init; } = 4d;

    /// <summary>
    /// Whether controls read as rounded rectangles, as pills, or as squares. A pill's radius is
    /// computed from <see cref="ControlHeight"/> rather than from <see cref="CornerRadius"/>,
    /// because "fully rounded" is a function of how tall the control is.
    /// </summary>
    public ControlShape Shape { get; init; } = ControlShape.Rounded;

    /// <summary>
    /// How thick every control outline is, in pixels. One is a hairline; two or three is the
    /// heavy outline neubrutalism is built on.
    /// </summary>
    public double BorderWidth { get; init; } = 1d;

    /// <summary>
    /// The distance a control's hard drop shadow is offset down and to the right, in pixels.
    /// Zero switches it off, which is what every theme but the neubrutalist one wants.
    ///
    /// This is a solid, unblurred shadow — the flat rectangle of colour sitting behind a control,
    /// not a soft glow. Cards asked for a shadow with <c>Layout.Card</c> keep getting a soft one
    /// when this is zero, so the two ideas do not collide.
    /// </summary>
    public double ShadowOffset { get; init; }

    /// <summary>
    /// Sets field labels, headings and buttons in a heavier weight. Neubrutalism reads as loud
    /// partly because the type is: thin captions beside three-pixel outlines look like a mistake.
    /// </summary>
    public bool HeavyText { get; init; }

    public double FontSize { get; init; } = 13d;

    /// <summary>
    /// Null uses Interlude's own font, which is embedded in the assembly and therefore present
    /// on every machine the package reaches. Name a font here to override it — but remember that
    /// a font named and not installed falls back to whatever the host happens to have.
    /// </summary>
    public string? FontFamily { get; init; }

    /// <summary>
    /// Renders section headers, card headers, tab captions and headings in capitals. Paired with
    /// <see cref="HeaderTracking"/> this is the "micro-label" treatment: small, spaced capitals
    /// that read as structure rather than as content.
    /// </summary>
    public bool UppercaseHeaders { get; init; }

    /// <summary>
    /// Extra space between the letters of a header, as a fraction of the font size. Zero is
    /// normal spacing; 0.1 is a comfortable amount for capitals.
    /// </summary>
    public double HeaderTracking { get; init; }

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

        ThemePalette palette = useDark ? ResolveDarkPalette() : ResolveLightPalette();

        return Accent.HasValue ? palette.WithAccent(Accent.Value) : palette;
    }

    /// <summary>The light palette this theme uses: its own if it has one, otherwise its preset's.</summary>
    public ThemePalette ResolveLightPalette() => LightPalette ?? PresetLight(Preset);

    /// <summary>The dark palette this theme uses: its own if it has one, otherwise its preset's.</summary>
    public ThemePalette ResolveDarkPalette() => DarkPalette ?? PresetDark(Preset);

    private static ThemePalette PresetLight(ThemePreset preset) => preset switch
    {
        ThemePreset.Neubrutalist => ThemePalette.Neo,
        _ => ThemePalette.Light,
    };

    private static ThemePalette PresetDark(ThemePreset preset) => preset switch
    {
        ThemePreset.Neubrutalist => ThemePalette.NeoDark,
        _ => ThemePalette.Dark,
    };

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

    /// <summary>
    /// The corner radius controls actually get, once <see cref="Shape"/> has had its say.
    ///
    /// A pill is half the control's height rather than some very large number left to the layout
    /// engine to clamp. Computing it means the shape is the same whatever WPF decides to do with
    /// an out-of-range radius, and it stays a true capsule at every density.
    /// </summary>
    public double EffectiveCornerRadius => Shape switch
    {
        ControlShape.Pill => ControlHeight / 2d,
        ControlShape.Square => 0d,
        _ => CornerRadius,
    };
}
