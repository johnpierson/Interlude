using System;
using Autodesk.DesignScript.Runtime;
using Interlude.Model;
using Interlude.Theming;

namespace Interlude;

/// <summary>
/// How a form looks. Feed the result into <c>Form.Show</c>'s theme port.
///
/// A theme is applied to the form's own window and nowhere else. Interlude runs inside Revit and
/// inside Dynamo, and restyling a host application from a package would be an unwelcome surprise
/// no matter how good the styling was.
/// </summary>
public class Theme
{
    private Theme()
    {
    }

    /// <summary>
    /// The default look, but following the Windows light or dark setting instead of staying light.
    ///
    /// A form with nothing on its theme port is light, because the default palette is designed
    /// around cream and black and the inverted one is a different design rather than the same one
    /// dimmed. This node is for a graph that would rather match whatever the machine is set to.
    /// </summary>
    /// <returns name="theme">The theme.</returns>
    /// <search>theme,default,system,auto,follow,windows</search>
    public static ThemeDefinition System()
        => ThemeDefinition.Default with { Mode = AppearanceMode.Auto };

    /// <summary>
    /// A light theme — the conventional one: hairline outlines, rounded corners, no shadows.
    ///
    /// The way out of the neubrutalist default, and the right choice for a form that should look
    /// like part of the software around it rather than like a thing of its own. Corporate
    /// deployments usually want this.
    ///
    /// Give an <c>accent</c> to brand it. The text drawn on that accent is chosen automatically by
    /// contrast, so a bright colour still reads.
    /// </summary>
    /// <param name="accent">Accent colour as hex, such as "#2F6FEB". Empty keeps the default.</param>
    /// <returns name="theme">The theme.</returns>
    /// <search>theme,light,bright,day</search>
    public static ThemeDefinition Light(string accent = "")
        => new()
        {
            Mode = AppearanceMode.Light,
            Accent = NodeSupport.OptionalColor(NodeSupport.OrNull(accent)),
        };

    /// <summary>
    /// A dark theme, tuned to sit comfortably over Revit's dark interface. Conventional, like
    /// <c>Theme.Light</c>: nothing here is loud.
    /// </summary>
    /// <param name="accent">Accent colour as hex, such as "#4C8DFF". Empty keeps the default.</param>
    /// <returns name="theme">The theme.</returns>
    /// <search>theme,dark,night,revit</search>
    public static ThemeDefinition Dark(string accent = "")
        => new()
        {
            Mode = AppearanceMode.Dark,
            Accent = NodeSupport.OptionalColor(NodeSupport.OrNull(accent)),
        };

    /// <summary>
    /// Neubrutalism: heavy black outlines, square corners, solid unblurred shadows offset down and
    /// to the right, loud flat colour, and type set hard. This is what a form looks like when
    /// nobody supplies a theme.
    ///
    /// The style is deliberately undesigned-looking — it borrows from brutalist architecture the
    /// idea that structure should be visible rather than smoothed over. Every edge is drawn, every
    /// control sits on its own shadow, and buttons drop onto that shadow when pressed. There is no
    /// gradient, no blur and no soft grey anywhere in it.
    /// </summary>
    /// <param name="dark">Ink on paper, or the whole thing inverted.</param>
    /// <param name="accent">
    /// Overrides the loud colour used for buttons and selection, as hex. Empty keeps the preset's
    /// own — hot pink in light, acid lime in dark.
    /// </param>
    /// <returns name="theme">The theme.</returns>
    /// <search>neubrutalism,neubrutalist,brutal,brutalist,neo,bold,loud,fun,memphis,shadow</search>
    public static ThemeDefinition Neubrutalism(bool dark = false, string accent = "")
        => ThemeDefinition.Default with
        {
            Mode = dark ? AppearanceMode.Dark : AppearanceMode.Light,
            Accent = NodeSupport.OptionalColor(NodeSupport.OrNull(accent)),
        };

    /// <summary>
    /// A monochrome theme: black, white and grey, pill-shaped controls, and small spaced capitals
    /// for section headings.
    ///
    /// Removing colour forces the layout to carry the design, which is why this style reads as
    /// deliberate rather than unfinished. Errors keep a red, though — an error nobody can pick out
    /// from ordinary text is a usability bug, and no amount of restraint is worth that.
    /// </summary>
    /// <param name="dark">Ink on paper, or paper on ink.</param>
    /// <param name="accent">
    /// Overrides the ink used for buttons and selection, as hex. Empty keeps it monochrome.
    /// </param>
    /// <returns name="theme">The theme.</returns>
    /// <search>mono,monochrome,black,white,minimal,swiss,pill</search>
    public static ThemeDefinition Mono(bool dark = false, string accent = "")
        => new()
        {
            Mode = dark ? AppearanceMode.Dark : AppearanceMode.Light,
            LightPalette = MonoPalette(dark: false),
            DarkPalette = MonoPalette(dark: true),
            Accent = NodeSupport.OptionalColor(NodeSupport.OrNull(accent)),
            Shape = ControlShape.Pill,
            UppercaseHeaders = true,
            HeaderTracking = 0.08d,
            Density = ThemeDensity.Comfortable,
            FontSize = 13d,
        };

    private static ThemePalette MonoPalette(bool dark) => dark
        ? ThemePalette.Dark with
        {
            Background = RgbColor.Parse("#15161A"),
            Surface = RgbColor.Parse("#1D1F24"),
            SurfaceAlt = RgbColor.Parse("#262930"),
            Border = RgbColor.Parse("#33373F"),
            BorderStrong = RgbColor.Parse("#5A606B"),
            Foreground = RgbColor.Parse("#F2F3F5"),
            ForegroundMuted = RgbColor.Parse("#9BA1AC"),
            ForegroundDisabled = RgbColor.Parse("#5A606B"),
            ControlBackground = RgbColor.Parse("#22252B"),
            ControlBackgroundHover = RgbColor.Parse("#2C3037"),
            ControlBackgroundDisabled = RgbColor.Parse("#1C1E23"),
            Accent = RgbColor.Parse("#F2F3F5"),
            AccentHover = RgbColor.Parse("#FFFFFF"),
            AccentForeground = RgbColor.Parse("#15161A"),
            Error = RgbColor.Parse("#FF7A6E"),
        }
        : ThemePalette.Light with
        {
            Background = RgbColor.Parse("#FFFFFF"),
            Surface = RgbColor.Parse("#F7F7F8"),
            SurfaceAlt = RgbColor.Parse("#EEEEF1"),
            Border = RgbColor.Parse("#D9D9DE"),
            BorderStrong = RgbColor.Parse("#9A9AA4"),
            Foreground = RgbColor.Parse("#16161A"),
            ForegroundMuted = RgbColor.Parse("#6B6B75"),
            ForegroundDisabled = RgbColor.Parse("#A9A9B2"),
            ControlBackground = RgbColor.Parse("#FFFFFF"),
            ControlBackgroundHover = RgbColor.Parse("#F2F2F4"),
            ControlBackgroundDisabled = RgbColor.Parse("#F0F0F2"),
            Accent = RgbColor.Parse("#16161A"),
            AccentHover = RgbColor.Parse("#33333A"),
            AccentForeground = RgbColor.Parse("#FFFFFF"),
            Error = RgbColor.Parse("#B3261E"),
        };

    /// <summary>
    /// A theme built from scratch, with every knob exposed.
    ///
    /// The presets are combinations of these ports; when one of them is nearly right, this is how
    /// you get the rest of the way. Note that it starts from the *conventional* look — hairline
    /// outlines, rounded corners, no shadows — not from the neubrutalist default, so a theme built
    /// here is quiet unless you ask for otherwise.
    ///
    /// The ports worth understanding:
    ///
    /// <c>shape</c> is Rounded, Pill or Square, and **Pill ignores <c>cornerRadius</c>** — it
    /// derives the radius from the control height instead, because "fully rounded" depends on how
    /// tall a control is. <c>borderWidth</c> and <c>shadowOffset</c> are what the neubrutalist look
    /// is built from; the shadow is solid and unblurred, and zero switches it off.
    /// <c>labelWidth: 0</c> stacks labels above their fields, which is the better shape for a
    /// narrow form or long captions. <c>uppercaseHeaders</c> and <c>headerTracking</c> apply to
    /// headings only, never to body text, where letter spacing costs more in readability than it
    /// returns.
    ///
    /// Leave <c>fontFamily</c> empty to keep Interlude's own embedded font, which renders the same
    /// on every machine. A font named here but not installed falls back to whatever the host has.
    /// </summary>
    /// <param name="mode">Auto, Light or Dark. Auto follows the Windows setting.</param>
    /// <param name="accent">Accent colour as hex. Empty keeps the palette's own accent.</param>
    /// <param name="density">Compact, Comfortable or Spacious.</param>
    /// <param name="cornerRadius">How rounded controls are, in pixels.</param>
    /// <param name="fontSize">Base text size, in pixels.</param>
    /// <param name="fontFamily">Font name. Empty uses the host's interface font.</param>
    /// <param name="labelWidth">Width of the label column. Zero stacks labels above their fields.</param>
    /// <param name="reducedMotion">Switch off transitions.</param>
    /// <param name="shape">Rounded, Pill or Square. Pill ignores cornerRadius and uses the control height.</param>
    /// <param name="uppercaseHeaders">Render section and card headings as capitals.</param>
    /// <param name="headerTracking">Space between the letters of a heading, as a fraction of the font size.</param>
    /// <param name="borderWidth">How thick control outlines are, in pixels.</param>
    /// <param name="shadowOffset">
    /// How far a solid, unblurred shadow sits below and right of each control, in pixels. Zero is
    /// no shadow.
    /// </param>
    /// <param name="heavyText">Set labels, headings and buttons in a heavier weight.</param>
    /// <returns name="theme">The theme.</returns>
    /// <search>theme,custom,style,brand,accent,font,density,pill,shape,border,shadow</search>
    public static ThemeDefinition Create(
        string mode = "Auto",
        string accent = "",
        string density = "Comfortable",
        double cornerRadius = 4,
        double fontSize = 13,
        string fontFamily = "",
        double labelWidth = 130,
        bool reducedMotion = false,
        string shape = "Rounded",
        bool uppercaseHeaders = false,
        double headerTracking = 0,
        double borderWidth = 1,
        double shadowOffset = 0,
        bool heavyText = false)
        => new()
        {
            BorderWidth = Math.Max(0d, borderWidth),
            ShadowOffset = Math.Max(0d, shadowOffset),
            HeavyText = heavyText,
            Shape = Enum.TryParse(shape, ignoreCase: true, out ControlShape parsedShape)
                ? parsedShape
                : ControlShape.Rounded,
            UppercaseHeaders = uppercaseHeaders,
            HeaderTracking = Math.Max(0d, headerTracking),
            Mode = Enum.TryParse(mode, ignoreCase: true, out AppearanceMode parsedMode)
                ? parsedMode
                : AppearanceMode.Auto,
            Accent = NodeSupport.OptionalColor(NodeSupport.OrNull(accent)),
            Density = Enum.TryParse(density, ignoreCase: true, out ThemeDensity parsedDensity)
                ? parsedDensity
                : ThemeDensity.Comfortable,
            CornerRadius = Math.Max(0, cornerRadius),
            FontSize = fontSize <= 0 ? 13 : fontSize,
            FontFamily = NodeSupport.OrNull(fontFamily),
            LabelWidth = Math.Max(0, labelWidth),
            ReducedMotion = reducedMotion,
        };

    /// <summary>
    /// Replaces individual colours in a theme's palette. Every colour left empty keeps the value
    /// it already had.
    /// </summary>
    /// <param name="theme">The theme to adjust.</param>
    /// <param name="background">Window backdrop, as hex.</param>
    /// <param name="foreground">Main text colour, as hex.</param>
    /// <param name="surface">Panels and cards, as hex.</param>
    /// <param name="border">Control outlines, as hex.</param>
    /// <param name="error">Validation colour, as hex.</param>
    /// <returns name="theme">The adjusted theme.</returns>
    /// <search>theme,palette,colors,colours,brand,override</search>
    public static ThemeDefinition WithColors(
        ThemeDefinition theme,
        string background = "",
        string foreground = "",
        string surface = "",
        string border = "",
        string error = "")
    {
        ThemeDefinition source = theme ?? ThemeDefinition.Default;

        // Both palettes are adjusted, so the override survives a light/dark switch.
        ThemePalette light = Adjust(source.ResolveLightPalette());
        ThemePalette dark = Adjust(source.ResolveDarkPalette());

        return source with { LightPalette = light, DarkPalette = dark };

        ThemePalette Adjust(ThemePalette palette) => palette with
        {
            Background = NodeSupport.OptionalColor(NodeSupport.OrNull(background)) ?? palette.Background,
            Foreground = NodeSupport.OptionalColor(NodeSupport.OrNull(foreground)) ?? palette.Foreground,
            Surface = NodeSupport.OptionalColor(NodeSupport.OrNull(surface)) ?? palette.Surface,
            Border = NodeSupport.OptionalColor(NodeSupport.OrNull(border)) ?? palette.Border,
            Error = NodeSupport.OptionalColor(NodeSupport.OrNull(error)) ?? palette.Error,
        };
    }
}
