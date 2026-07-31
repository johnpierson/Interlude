using System;
using System.Collections.Generic;
using Autodesk.DesignScript.Runtime;
using Interlude.Model;
using Interlude.Rendering.Wpf.Controls;

namespace Interlude.Rendering.Wpf;

/// <summary>
/// Maps element types to the renderers that draw them.
///
/// Resolution walks up the type hierarchy, so a package that subclasses an existing element
/// inherits its renderer for free, and an element with no renderer at all falls back to a
/// visible placeholder rather than taking the dialog down. A form that shows one unfamiliar
/// control is recoverable; a form that throws is not.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
public sealed class ControlRendererRegistry
{
    private readonly Dictionary<Type, IControlRenderer> _renderers = new();
    private readonly FallbackRenderer _fallback = new();

    /// <summary>An empty registry. Use <see cref="CreateDefault"/> for the built-in catalogue.</summary>
    public ControlRendererRegistry()
    {
    }

    /// <summary>Every element type with a registered renderer.</summary>
    public IReadOnlyCollection<Type> RegisteredTypes => _renderers.Keys;

    /// <summary>Builds a registry containing every control Interlude ships.</summary>
    public static ControlRendererRegistry CreateDefault()
    {
        ControlRendererRegistry registry = new();

        // Inputs.
        registry.Register(new TextBoxRenderer());
        registry.Register(new PasswordRenderer());
        registry.Register(new NumericRenderer());
        registry.Register(new IntegerRenderer());
        registry.Register(new SliderRenderer());
        registry.Register(new DropdownRenderer());
        registry.Register(new RadioGroupRenderer());
        registry.Register(new CheckBoxRenderer());
        registry.Register(new ToggleRenderer());
        registry.Register(new ListSelectionRenderer());
        registry.Register(new TreeSelectionRenderer());
        registry.Register(new DatePickerRenderer());
        registry.Register(new ColorPickerRenderer());
        registry.Register(new FilePickerRenderer());
        registry.Register(new FolderPickerRenderer());

        // Display.
        registry.Register(new LabelRenderer());
        registry.Register(new MarkdownRenderer());
        registry.Register(new ImageRenderer());
        registry.Register(new SeparatorRenderer());
        registry.Register(new SpacerRenderer());
        registry.Register(new ProgressRenderer());
        registry.Register(new ButtonRenderer());

        // Containers.
        registry.Register(new VStackRenderer());
        registry.Register(new HStackRenderer());
        registry.Register(new GridRenderer());
        registry.Register(new GroupBoxRenderer());
        registry.Register(new TabsRenderer());
        registry.Register(new TabPageRenderer());
        registry.Register(new ExpanderRenderer());
        registry.Register(new CardRenderer());
        registry.Register(new ScrollRenderer());
        registry.Register(new DockRenderer());
        registry.Register(new SplitViewRenderer());

        return registry;
    }

    /// <summary>Registers a renderer, replacing any previous one for the same element type.</summary>
    public ControlRendererRegistry Register(IControlRenderer renderer)
    {
        if (renderer is null)
        {
            throw new ArgumentNullException(nameof(renderer));
        }

        _renderers[renderer.ElementType] = renderer;
        return this;
    }

    /// <summary>Finds the renderer for an element, falling back to a placeholder.</summary>
    public IControlRenderer Resolve(FormElement element)
    {
        if (element is null)
        {
            throw new ArgumentNullException(nameof(element));
        }

        for (Type? type = element.GetType(); type is not null; type = type.BaseType)
        {
            if (_renderers.TryGetValue(type, out IControlRenderer? renderer))
            {
                return renderer;
            }
        }

        return _fallback;
    }

    /// <summary>Whether a renderer is registered for an element, ignoring the fallback.</summary>
    public bool CanRender(FormElement element)
    {
        for (Type? type = element?.GetType(); type is not null; type = type.BaseType)
        {
            if (_renderers.ContainsKey(type))
            {
                return true;
            }
        }

        return false;
    }
}
