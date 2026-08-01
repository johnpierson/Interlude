using System;
using System.Windows;
using Autodesk.DesignScript.Runtime;
using Interlude.Model;
using Interlude.Runtime;

namespace Interlude.Rendering.Wpf;

/// <summary>
/// Builds and drives the WPF control for one kind of element.
///
/// This is the extension point that keeps the renderer core closed for modification: adding a
/// control means adding an element record and one of these, then registering the pair. Nothing
/// in the window, the layout code or the state plumbing changes.
///
/// The contract each implementation must honour is small but strict: wire exactly one thing —
/// the control's change event to <see cref="RenderContext.ReportValue"/> — and never wire one
/// control to another. All cross-field behaviour belongs to <see cref="FormSession"/>.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
internal interface IControlRenderer
{
    /// <summary>The element type this renderer handles.</summary>
    Type ElementType { get; }

    /// <summary>
    /// Whether the standard field chrome — label column, help text, error line, required marker —
    /// should be drawn around the control. Containers and display elements say no and take
    /// responsibility for their own presentation.
    /// </summary>
    bool UsesFieldChrome { get; }

    /// <summary>Creates the control.</summary>
    FrameworkElement Build(FormElement element, RenderContext context);

    /// <summary>Applies enablement and any other per-state presentation to an existing control.</summary>
    void ApplyState(FrameworkElement control, ElementRuntimeState state);

    /// <summary>Reads the control's current value in the shape the model expects.</summary>
    object? ReadValue(FrameworkElement control);

    /// <summary>Pushes a value into the control, without raising a change back to the session.</summary>
    void WriteValue(FrameworkElement control, object? value);
}

/// <summary>
/// Base class that takes care of the casting and the default behaviour, so a concrete renderer
/// is usually one <c>Build</c> method and two one-liners.
/// </summary>
/// <typeparam name="TElement">The element type handled.</typeparam>
[IsVisibleInDynamoLibrary(false)]
internal abstract class ControlRenderer<TElement> : IControlRenderer
    where TElement : FormElement
{
    /// <inheritdoc />
    public Type ElementType => typeof(TElement);

    /// <inheritdoc />
    public virtual bool UsesFieldChrome => true;

    /// <inheritdoc />
    public FrameworkElement Build(FormElement element, RenderContext context)
    {
        if (element is not TElement typed)
        {
            throw new ArgumentException(
                $"{GetType().Name} renders {typeof(TElement).Name}, not {element?.GetType().Name ?? "null"}.",
                nameof(element));
        }

        return BuildCore(typed, context);
    }

    /// <inheritdoc />
    public virtual void ApplyState(FrameworkElement control, ElementRuntimeState state)
        => control.IsEnabled = state.IsEnabled;

    /// <inheritdoc />
    public virtual object? ReadValue(FrameworkElement control) => null;

    /// <inheritdoc />
    public virtual void WriteValue(FrameworkElement control, object? value)
    {
    }

    /// <summary>Creates the control for a strongly-typed element.</summary>
    protected abstract FrameworkElement BuildCore(TElement element, RenderContext context);
}
