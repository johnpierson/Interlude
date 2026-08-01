using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Windows;
using Autodesk.DesignScript.Runtime;
using Interlude.Model;
using Interlude.Runtime;
using Interlude.Theming;

namespace Interlude.Rendering.Wpf;

/// <summary>A button asking the form to do something.</summary>
[IsVisibleInDynamoLibrary(false)]
internal sealed class FormActionEventArgs : EventArgs
{
    internal FormActionEventArgs(ButtonAction action, string tag, string? url)
    {
        Action = action;
        Tag = tag;
        Url = url;
    }

    public ButtonAction Action { get; }

    /// <summary>Reported back to the graph as <c>buttonClicked</c>.</summary>
    public string Tag { get; }

    public string? Url { get; }
}

/// <summary>
/// What a control renderer is handed while it builds: the session it reports to, the resolved
/// theme, and the way to build nested children.
///
/// Note what is <em>not</em> here: any way for one control to find another. That omission is the
/// design. A control's entire outward contract is "tell the session my value changed", and every
/// consequence of that change comes back as a state batch.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
internal sealed class RenderContext
{
    private readonly Dictionary<FormElement, ElementView> _views = new(ReferenceComparer.Instance);

    internal RenderContext(
        FormSession session,
        ThemeDefinition theme,
        ThemePalette palette,
        ControlRendererRegistry registry)
    {
        Session = session;
        Theme = theme;
        Palette = palette;
        Registry = registry;
    }

    /// <summary>Raised when a button in the form body asks the window to act.</summary>
    public event EventHandler<FormActionEventArgs>? ActionRequested;

    /// <summary>The live state of the form.</summary>
    public FormSession Session { get; }

    /// <summary>The theme in force, as data.</summary>
    public ThemeDefinition Theme { get; }

    /// <summary>The resolved palette for the active light or dark mode.</summary>
    public ThemePalette Palette { get; }

    /// <summary>The renderer catalogue.</summary>
    public ControlRendererRegistry Registry { get; }

    /// <summary>Every element rendered so far, by element.</summary>
    public IReadOnlyDictionary<FormElement, ElementView> Views => _views;

    /// <summary>The theme's base spacing, in pixels.</summary>
    public double Spacing => Theme.BaseSpacing;

    /// <summary>The theme's minimum control height, in pixels.</summary>
    public double ControlHeight => Theme.ControlHeight;

    /// <summary>
    /// True while state is being pushed into controls. Renderers check this before reporting a
    /// change, which is what stops a value written by the session from bouncing straight back
    /// as a user edit.
    /// </summary>
    public bool IsApplyingState { get; internal set; }

    /// <summary>
    /// Builds an element and everything under it, wrapping inputs in the standard field chrome
    /// and registering the result. Container renderers call this for each child.
    /// </summary>
    public FrameworkElement BuildChild(FormElement element)
    {
        if (element is null)
        {
            throw new ArgumentNullException(nameof(element));
        }

        IControlRenderer renderer = Registry.Resolve(element);
        FrameworkElement control = renderer.Build(element, this);

        control.ApplyStyle(element.Style);

        if (!string.IsNullOrWhiteSpace(element.Tooltip))
        {
            control.ToolTip = element.Tooltip;
        }

        FrameworkElement root = control;
        FieldChrome.Result chrome = default;

        if (renderer.UsesFieldChrome)
        {
            chrome = FieldChrome.Wrap(element, control, this);
            root = chrome.Root;
        }

        ElementView view = new(element, root, control, renderer)
        {
            ErrorText = chrome.ErrorText,
            RequiredMarker = chrome.RequiredMarker,
        };

        // The margin belongs to the outermost visual, or a hidden field would still contribute
        // its own spacing to the stack around it.
        if (element.Style?.Margin is not null && !ReferenceEquals(root, control))
        {
            root.Margin = element.Style.Margin.Value.ToThickness();
            control.Margin = new Thickness(0);
        }

        _views[element] = view;
        return root;
    }

    /// <summary>Builds a list of children in order.</summary>
    public IReadOnlyList<FrameworkElement> BuildChildren(IEnumerable<FormElement>? children)
    {
        List<FrameworkElement> built = new();

        if (children is null)
        {
            return built;
        }

        foreach (FormElement child in children)
        {
            built.Add(BuildChild(child));
        }

        return built;
    }

    /// <summary>
    /// Reports a user edit. Silently ignored while state is being applied, which is the one
    /// piece of ceremony every control renderer has to remember — and the reason it is one call
    /// rather than a flag each renderer manages itself.
    /// </summary>
    public void ReportValue(FormElement element, object? value)
    {
        if (IsApplyingState || element is null || string.IsNullOrEmpty(element.Key))
        {
            return;
        }

        Session.SetValue(element.Key, value);
    }

    /// <summary>Asks the window to submit, cancel, reset, or open a link.</summary>
    public void RequestAction(ButtonAction action, string tag, string? url = null)
        => ActionRequested?.Invoke(this, new FormActionEventArgs(action, tag ?? string.Empty, url));

    /// <summary>Looks up the view for an element, or null when it has not been built.</summary>
    public ElementView? FindView(FormElement element)
        => element is not null && _views.TryGetValue(element, out ElementView? view) ? view : null;

    /// <summary>Element identity, not element equality: two identical spacers are two controls.</summary>
    private sealed class ReferenceComparer : IEqualityComparer<FormElement>
    {
        internal static readonly ReferenceComparer Instance = new();

        public bool Equals(FormElement? x, FormElement? y) => ReferenceEquals(x, y);

        public int GetHashCode(FormElement obj) => RuntimeHelpers.GetHashCode(obj);
    }
}
