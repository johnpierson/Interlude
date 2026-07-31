using System;
using Autodesk.DesignScript.Runtime;
using Interlude.Model;
using Interlude.Runtime;

namespace Interlude.Rendering.Wpf;

/// <summary>
/// The WPF implementation of <see cref="IFormRenderer"/>.
///
/// WPF was chosen for one reason above the others: it costs zero deployment files. It is in the
/// box on every framework Interlude targets, the host already runs a WPF dispatcher, and owning
/// a dialog to Revit's window is a single interop call. Every alternative — WinUI, Avalonia —
/// means shipping managed and native binaries into a flat folder that Revit shares with every
/// other add-in, which is the exact failure mode this package is built to avoid.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
public sealed class WpfFormRenderer : IFormRenderer
{
    private readonly ControlRendererRegistry _registry;

    /// <summary>Creates a renderer with the built-in control catalogue.</summary>
    public WpfFormRenderer()
        : this(ControlRendererRegistry.CreateDefault())
    {
    }

    /// <summary>Creates a renderer with a custom catalogue, for extra or replaced controls.</summary>
    public WpfFormRenderer(ControlRendererRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    /// <summary>The catalogue this renderer draws with.</summary>
    public ControlRendererRegistry Registry => _registry;

    /// <inheritdoc />
    public FormResult ShowModal(FormDefinition definition, FormSession session)
    {
        if (definition is null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        if (session is null)
        {
            throw new ArgumentNullException(nameof(session));
        }

        return WindowHost.ShowModal(
            window =>
            {
                FormWindow form = (FormWindow)window;
                form.ShowDialog();

                // FormWindow guarantees a result on close, including when the user used the
                // title-bar X, so this fallback is belt and braces rather than a real path.
                return form.Result ?? session.BuildCancelledResult(FormButtonNames.Closed);
            },
            () => new FormWindow(definition, session, _registry),
            definition.Title);
    }
}
