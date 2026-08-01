using System.Windows;
using System.Windows.Controls;
using Autodesk.DesignScript.Runtime;
using Interlude.Model;
using Interlude.Runtime;

namespace Interlude.Rendering.Wpf;

/// <summary>
/// The rendered form of one element: the control itself, the chrome drawn around it, and the
/// renderer that owns both. The window keeps one of these per element and does nothing else to
/// track the visual tree.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
internal sealed class ElementView
{
    internal ElementView(FormElement element, FrameworkElement root, FrameworkElement control, IControlRenderer renderer)
    {
        Element = element;
        Root = root;
        Control = control;
        Renderer = renderer;
    }

    /// <summary>The model element this view was built from.</summary>
    public FormElement Element { get; }

    /// <summary>
    /// The outermost visual for this element, including any label and error line. This is what
    /// gets collapsed when the element is hidden, so a hidden field takes up no space at all
    /// rather than leaving a labelled gap.
    /// </summary>
    public FrameworkElement Root { get; }

    /// <summary>The control the renderer built.</summary>
    public FrameworkElement Control { get; }

    /// <summary>The renderer that owns this control.</summary>
    public IControlRenderer Renderer { get; }

    /// <summary>The error line beneath the control, when the element has field chrome.</summary>
    internal TextBlock? ErrorText { get; set; }

    /// <summary>The asterisk beside the label, when the element has field chrome.</summary>
    internal UIElement? RequiredMarker { get; set; }

    /// <summary>Shows or hides the element, collapsing its space when hidden.</summary>
    public void ApplyVisibility(bool isVisible)
        => Root.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>Shows or hides the required marker.</summary>
    public void ApplyRequired(bool isRequired)
    {
        FieldState.SetIsRequired(Control, isRequired);

        if (RequiredMarker is not null)
        {
            RequiredMarker.Visibility = isRequired ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    /// <summary>
    /// Shows or hides the validation message. <paramref name="reveal"/> is false until the user
    /// has touched the field or tried to submit, so a form does not open covered in red.
    /// </summary>
    public void ApplyError(string? error, bool reveal)
    {
        bool show = reveal && !string.IsNullOrEmpty(error);

        FieldState.SetHasError(Control, show);

        if (ErrorText is null)
        {
            return;
        }

        ErrorText.Text = error ?? string.Empty;
        ErrorText.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>Applies every part of a state at once, used when the form is first shown.</summary>
    public void ApplyAll(ElementRuntimeState state, bool revealErrors)
    {
        ApplyVisibility(state.IsVisible);
        ApplyRequired(state.IsRequired);
        Renderer.ApplyState(Control, state);
        ApplyError(state.Error, revealErrors);
    }
}
