using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Autodesk.DesignScript.Runtime;
using Interlude.Theming;
using Microsoft.Win32;

namespace Interlude.Rendering.Wpf;

/// <summary>
/// Turns a <see cref="ThemeDefinition"/> into WPF resources.
///
/// The hard rule this class exists to enforce: resources go into the form window's own
/// <c>Resources</c> and never into <c>Application.Current.Resources</c>. Interlude runs inside
/// Revit and inside Dynamo — someone else's application, with someone else's styling — and
/// writing to the application dictionary would restyle their UI from underneath them. Every
/// brush below is scoped to one window and dies with it.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
internal static class WpfThemeApplier
{
    private const string ThemeDictionaryUri = "pack://application:,,,/Interlude;component/Themes/Interlude.xaml";

    /// <summary>
    /// Applies a theme to one element's resource scope and returns the palette that was used.
    /// </summary>
    public static ThemePalette Apply(FrameworkElement scope, ThemeDefinition theme)
    {
        if (scope is null)
        {
            throw new ArgumentNullException(nameof(scope));
        }

        theme ??= ThemeDefinition.Default;

        ThemePalette palette = theme.ResolvePalette(SystemPrefersDark());

        WriteValues(scope.Resources, theme, palette);
        MergeControlStyles(scope.Resources);

        scope.SetValue(Control.BackgroundProperty, palette.Background.ToBrush());
        scope.SetValue(Control.ForegroundProperty, palette.Foreground.ToBrush());

        return palette;
    }

    /// <summary>
    /// Swaps the palette on an already-themed scope. Because every style consumes these keys as
    /// dynamic resources, this repaints the whole window without rebuilding a single control.
    /// </summary>
    public static ThemePalette Retheme(FrameworkElement scope, ThemeDefinition theme, bool useDark)
    {
        ThemePalette palette = (theme ?? ThemeDefinition.Default)
            .ResolvePalette(useDark);

        WriteValues(scope.Resources, theme ?? ThemeDefinition.Default, palette);
        return palette;
    }

    /// <summary>
    /// Whether Windows is set to a dark app theme.
    ///
    /// Read from the registry because there is no framework API for it on every supported
    /// target, and defaults to light when the value is missing or unreadable — a wrong guess
    /// towards light is far less jarring than a black dialog on a white desktop.
    /// </summary>
    public static bool SystemPrefersDark()
    {
        try
        {
            object? value = Registry.GetValue(
                @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                "AppsUseLightTheme",
                null);

            return value is int light && light == 0;
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or System.IO.IOException)
        {
            return false;
        }
    }

    private static void WriteValues(ResourceDictionary resources, ThemeDefinition theme, ThemePalette palette)
    {
        Set(resources, ThemeKeys.Background, palette.Background.ToBrush());
        Set(resources, ThemeKeys.Surface, palette.Surface.ToBrush());
        Set(resources, ThemeKeys.SurfaceAlt, palette.SurfaceAlt.ToBrush());
        Set(resources, ThemeKeys.Border, palette.Border.ToBrush());
        Set(resources, ThemeKeys.BorderStrong, palette.BorderStrong.ToBrush());
        Set(resources, ThemeKeys.Foreground, palette.Foreground.ToBrush());
        Set(resources, ThemeKeys.ForegroundMuted, palette.ForegroundMuted.ToBrush());
        Set(resources, ThemeKeys.ForegroundDisabled, palette.ForegroundDisabled.ToBrush());
        Set(resources, ThemeKeys.ControlBackground, palette.ControlBackground.ToBrush());
        Set(resources, ThemeKeys.ControlBackgroundHover, palette.ControlBackgroundHover.ToBrush());
        Set(resources, ThemeKeys.ControlBackgroundDisabled, palette.ControlBackgroundDisabled.ToBrush());
        Set(resources, ThemeKeys.Accent, palette.Accent.ToBrush());
        Set(resources, ThemeKeys.AccentHover, palette.AccentHover.ToBrush());
        Set(resources, ThemeKeys.AccentForeground, palette.AccentForeground.ToBrush());
        Set(resources, ThemeKeys.Error, palette.Error.ToBrush());
        Set(resources, ThemeKeys.Warning, palette.Warning.ToBrush());
        Set(resources, ThemeKeys.Success, palette.Success.ToBrush());
        Set(resources, ThemeKeys.ShadowColor, palette.Shadow.ToColor());

        double radius = theme.EffectiveCornerRadius;
        Set(resources, ThemeKeys.CornerRadius, new CornerRadius(radius));
        Set(resources, ThemeKeys.CornerRadiusValue, radius);
        Set(resources, ThemeKeys.FontSize, theme.FontSize);
        Set(resources, ThemeKeys.FontSizeSmall, Math.Max(9d, theme.FontSize - 2d));
        Set(resources, ThemeKeys.FontSizeHeading, theme.FontSize * 1.35d);
        Set(resources, ThemeKeys.FontFamily, ResolveFontFamily(theme.FontFamily));
        Set(resources, ThemeKeys.Spacing, theme.BaseSpacing);
        Set(resources, ThemeKeys.SpacingSmall, theme.BaseSpacing / 2d);
        Set(resources, ThemeKeys.SpacingLarge, theme.BaseSpacing * 2d);
        Set(resources, ThemeKeys.ControlHeight, theme.ControlHeight);
        Set(resources, ThemeKeys.ControlPadding, new Thickness(theme.BaseSpacing, 2d, theme.BaseSpacing, 2d));

        Set(resources, ThemeKeys.TransitionDuration,
            new Duration(TimeSpan.FromMilliseconds(theme.ReducedMotion ? 0d : 120d)));
    }

    private static void MergeControlStyles(ResourceDictionary resources)
    {
        Uri uri = new(ThemeDictionaryUri, UriKind.Absolute);

        foreach (ResourceDictionary existing in resources.MergedDictionaries)
        {
            if (existing.Source == uri)
            {
                return;
            }
        }

        resources.MergedDictionaries.Add(new ResourceDictionary { Source = uri });
    }

    /// <summary>
    /// Interlude's own font, embedded in this assembly.
    ///
    /// The trailing fallbacks matter: if the embedded font ever fails to resolve — an unusual
    /// hosting arrangement, a trimmed assembly — WPF walks the list rather than dropping to a
    /// serif default that would make every form look broken.
    /// </summary>
    private const string EmbeddedFont = "./Fonts/#Comic Neue, Segoe UI Variable Text, Segoe UI, Tahoma";

    private static readonly Uri FontBaseUri = new("pack://application:,,,/Interlude;component/");

    private static FontFamily ResolveFontFamily(string? requested)
    {
        if (!string.IsNullOrWhiteSpace(requested))
        {
            try
            {
                return new FontFamily(requested);
            }
            catch (ArgumentException)
            {
                // A font name nobody has is not worth refusing to show the form over.
            }
        }

        try
        {
            // The base-URI overload is what makes "./Fonts/#Comic Neue" resolve against this
            // assembly's resources rather than against the installed font collection.
            return new FontFamily(FontBaseUri, EmbeddedFont);
        }
        catch (Exception ex) when (ex is ArgumentException or UriFormatException)
        {
            return new FontFamily("Segoe UI Variable Text, Segoe UI, Tahoma");
        }
    }

    private static void Set(ResourceDictionary resources, string key, object value)
        => resources[key] = value;
}
