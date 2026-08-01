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

    /// <summary>
    /// The corner radius for things a few pixels across — a tick box, a progress cell. Clamped,
    /// because the full radius belongs to a control's height: a pill theme would otherwise round a
    /// sixteen-pixel tick box into a circle, and a check box that looks like a radio button is a
    /// bug however deliberate the styling was.
    /// </summary>
    public const string SmallCornerRadius = Prefix + "SmallCornerRadius";
    public const string FontSize = Prefix + "FontSize";
    public const string FontSizeSmall = Prefix + "FontSizeSmall";
    public const string FontSizeHeading = Prefix + "FontSizeHeading";
    public const string FontFamily = Prefix + "FontFamily";
    public const string Spacing = Prefix + "Spacing";
    public const string SpacingSmall = Prefix + "SpacingSmall";
    public const string SpacingLarge = Prefix + "SpacingLarge";
    public const string ControlHeight = Prefix + "ControlHeight";
    public const string ControlPadding = Prefix + "ControlPadding";

    /// <summary>A uniform <c>Thickness</c> of <see cref="ThemeDefinition.BorderWidth"/>.</summary>
    public const string BorderThickness = Prefix + "BorderThickness";

    /// <summary>The same width as a number, for the templates that stroke rather than border.</summary>
    public const string BorderWidthValue = Prefix + "BorderWidthValue";

    /// <summary>A bottom-only edge: the divider under a group header, and the tab strip.</summary>
    public const string EdgeThickness = Prefix + "EdgeThickness";

    /// <summary>The bar under the selected tab, never thinner than two pixels.</summary>
    public const string UnderlineThickness = Prefix + "UnderlineThickness";

    // Weights. Both move together with ThemeDefinition.HeavyText.
    public const string HeadingFontWeight = Prefix + "HeadingFontWeight";
    public const string LabelFontWeight = Prefix + "LabelFontWeight";

    /// <summary>
    /// The hard offset shadow behind controls. Written only when
    /// <see cref="ThemeDefinition.ShadowOffset"/> is positive: an unresolved <c>DynamicResource</c>
    /// leaves <c>Effect</c> at its default of null, which is exactly "no shadow" and costs no
    /// render layer.
    /// </summary>
    public const string ControlShadow = Prefix + "ControlShadow";

    /// <summary>
    /// The shadow a card gets when it asks for one. Always present, because <c>Layout.Card</c>'s
    /// shadow is a property of the card rather than of the theme — hard when the theme offsets
    /// shadows, soft otherwise.
    /// </summary>
    public const string CardShadow = Prefix + "CardShadow";

    /// <summary>
    /// The translation a button takes on while pressed, so it appears to move down onto its own
    /// shadow. Identity when the theme has no shadow offset.
    /// </summary>
    public const string PressTransform = Prefix + "PressTransform";

    /// <summary>Zero when transitions are switched off, which is how reduced motion is honoured.</summary>
    public const string TransitionDuration = Prefix + "TransitionDuration";
}
