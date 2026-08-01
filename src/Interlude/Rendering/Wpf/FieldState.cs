using System.Windows;
using Autodesk.DesignScript.Runtime;

namespace Interlude.Rendering.Wpf;

/// <summary>
/// Attached properties the themes trigger on.
///
/// Rather than the renderer reaching into each control to repaint a border when validation
/// fails — which would mean knowing every control's visual tree — it sets a flag here and the
/// XAML style decides what "invalid" looks like. Restyling error states is then a theme change,
/// not a code change.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
internal static class FieldState
{
    /// <summary>
    /// Set on a control whose value is currently failing a rule.
    ///
    /// Inherited down the tree on purpose: several controls are composites — a numeric field is
    /// a border wrapping a text box and two spinner buttons — and the piece that draws the error
    /// outline is not the piece the renderer holds a reference to. Inheritance lets the renderer
    /// flag the control and the theme decide which part of it turns red.
    /// </summary>
    public static readonly DependencyProperty HasErrorProperty =
        DependencyProperty.RegisterAttached(
            "HasError",
            typeof(bool),
            typeof(FieldState),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));

    /// <summary>Set on a control the user must answer.</summary>
    public static readonly DependencyProperty IsRequiredProperty =
        DependencyProperty.RegisterAttached(
            "IsRequired",
            typeof(bool),
            typeof(FieldState),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));

    /// <summary>Placeholder text drawn while a text box is empty.</summary>
    public static readonly DependencyProperty PlaceholderProperty =
        DependencyProperty.RegisterAttached(
            "Placeholder",
            typeof(string),
            typeof(FieldState),
            new FrameworkPropertyMetadata(string.Empty));

    public static void SetHasError(DependencyObject element, bool value)
        => element?.SetValue(HasErrorProperty, value);

    public static bool GetHasError(DependencyObject element)
        => element is not null && (bool)element.GetValue(HasErrorProperty);

    public static void SetIsRequired(DependencyObject element, bool value)
        => element?.SetValue(IsRequiredProperty, value);

    public static bool GetIsRequired(DependencyObject element)
        => element is not null && (bool)element.GetValue(IsRequiredProperty);

    public static void SetPlaceholder(DependencyObject element, string value)
        => element?.SetValue(PlaceholderProperty, value);

    public static string GetPlaceholder(DependencyObject element)
        => element is null ? string.Empty : (string)element.GetValue(PlaceholderProperty);
}
