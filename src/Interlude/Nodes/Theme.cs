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
    /// The default theme: follow the Windows light or dark setting, at comfortable spacing.
    /// </summary>
    /// <returns name="theme">The theme.</returns>
    /// <search>theme,default,system,auto</search>
    public static ThemeDefinition System() => ThemeDefinition.Default;

    /// <summary>
    /// A light theme.
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
    /// A dark theme, tuned to sit comfortably over Revit's dark interface.
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
    /// A theme built from scratch.
    /// </summary>
    /// <param name="mode">Auto, Light or Dark. Auto follows the Windows setting.</param>
    /// <param name="accent">Accent colour as hex. Empty keeps the palette's own accent.</param>
    /// <param name="density">Compact, Comfortable or Spacious.</param>
    /// <param name="cornerRadius">How rounded controls are, in pixels.</param>
    /// <param name="fontSize">Base text size, in pixels.</param>
    /// <param name="fontFamily">Font name. Empty uses the host's interface font.</param>
    /// <param name="labelWidth">Width of the label column. Zero stacks labels above their fields.</param>
    /// <param name="reducedMotion">Switch off transitions.</param>
    /// <returns name="theme">The theme.</returns>
    /// <search>theme,custom,style,brand,accent,font,density</search>
    public static ThemeDefinition Create(
        string mode = "Auto",
        string accent = "",
        string density = "Comfortable",
        double cornerRadius = 4,
        double fontSize = 13,
        string fontFamily = "",
        double labelWidth = 130,
        bool reducedMotion = false)
        => new()
        {
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
        ThemePalette light = Adjust(source.LightPalette ?? ThemePalette.Light);
        ThemePalette dark = Adjust(source.DarkPalette ?? ThemePalette.Dark);

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
