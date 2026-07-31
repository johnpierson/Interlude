using Autodesk.DesignScript.Runtime;

namespace Interlude.Theming;

/// <summary>
/// The resource keys the XAML themes look up.
///
/// The XAML consumes every one of these as a <c>DynamicResource</c>, and the theme applier
/// injects them into the form window's own resource dictionary. That combination is what makes
/// switching between light and dark a dictionary swap rather than a rebuild — and, far more
/// importantly, it is what keeps Interlude out of <c>Application.Current.Resources</c>. We are a
/// guest inside Revit's and Dynamo's process; restyling their application is not ours to do.
///
/// These are plain strings, with no reference to WPF, so the pure layers can name a resource
/// without depending on the presentation stack.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
public static class ThemeKeys
{
    private const string Prefix = "Interlude.";

    // Brushes.
    public const string Background = Prefix + "Background";
    public const string Surface = Prefix + "Surface";
    public const string SurfaceAlt = Prefix + "SurfaceAlt";
    public const string Border = Prefix + "Border";
    public const string BorderStrong = Prefix + "BorderStrong";
    public const string Foreground = Prefix + "Foreground";
    public const string ForegroundMuted = Prefix + "ForegroundMuted";
    public const string ForegroundDisabled = Prefix + "ForegroundDisabled";
    public const string ControlBackground = Prefix + "ControlBackground";
    public const string ControlBackgroundHover = Prefix + "ControlBackgroundHover";
    public const string ControlBackgroundDisabled = Prefix + "ControlBackgroundDisabled";
    public const string Accent = Prefix + "Accent";
    public const string AccentHover = Prefix + "AccentHover";
    public const string AccentForeground = Prefix + "AccentForeground";
    public const string Error = Prefix + "Error";
    public const string Warning = Prefix + "Warning";
    public const string Success = Prefix + "Success";
    public const string ShadowColor = Prefix + "ShadowColor";

    // Metrics.
    public const string CornerRadius = Prefix + "CornerRadius";
    public const string CornerRadiusValue = Prefix + "CornerRadiusValue";
    public const string FontSize = Prefix + "FontSize";
    public const string FontSizeSmall = Prefix + "FontSizeSmall";
    public const string FontSizeHeading = Prefix + "FontSizeHeading";
    public const string FontFamily = Prefix + "FontFamily";
    public const string Spacing = Prefix + "Spacing";
    public const string SpacingSmall = Prefix + "SpacingSmall";
    public const string SpacingLarge = Prefix + "SpacingLarge";
    public const string ControlHeight = Prefix + "ControlHeight";
    public const string ControlPadding = Prefix + "ControlPadding";

    /// <summary>Zero when transitions are switched off, which is how reduced motion is honoured.</summary>
    public const string TransitionDuration = Prefix + "TransitionDuration";
}
