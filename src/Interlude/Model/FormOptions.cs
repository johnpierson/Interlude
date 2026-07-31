using System;
using System.Collections.Generic;
using Autodesk.DesignScript.Runtime;

namespace Interlude.Model;

/// <summary>
/// The less common knobs for a form, kept off <c>Form.Show</c>'s own signature.
///
/// <c>Form.Show</c> takes an <c>options</c> port instead of growing a parameter every time a new
/// setting appears. Its signature is a published contract that graphs bind to positionally, so
/// it is append-only for ever; this record is where new settings can arrive without touching it.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
public sealed record FormOptions
{
    /// <summary>Paragraph shown above the first field.</summary>
    public string? Description { get; init; }

    /// <summary>Fixed window height. Null sizes the window to its content.</summary>
    public double? Height { get; init; }

    public double? MinWidth { get; init; }

    public bool IsResizable { get; init; } = true;

    public bool ShowSubmit { get; init; } = true;

    public bool ShowCancel { get; init; } = true;

    public bool CloseOnEscape { get; init; } = true;

    public bool ShowInTaskbar { get; init; }

    public string? IconPath { get; init; }

    /// <summary>Extra footer buttons, each reporting its own tag as <c>buttonClicked</c>.</summary>
    public IReadOnlyList<ButtonElement> ExtraButtons { get; init; } = Array.Empty<ButtonElement>();

    /// <summary>Returns the definition with these options folded in.</summary>
    public FormDefinition ApplyTo(FormDefinition definition)
    {
        if (definition is null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        return definition with
        {
            Description = string.IsNullOrWhiteSpace(Description) ? definition.Description : Description,
            Window = definition.Window with
            {
                Height = Height ?? definition.Window.Height,
                MinWidth = MinWidth ?? definition.Window.MinWidth,
                IsResizable = IsResizable,
                ShowInTaskbar = ShowInTaskbar,
                IconPath = string.IsNullOrWhiteSpace(IconPath) ? definition.Window.IconPath : IconPath,
            },
            Buttons = definition.Buttons with
            {
                ShowSubmit = ShowSubmit,
                ShowCancel = ShowCancel,
                CloseOnEscape = CloseOnEscape,
                ExtraButtons = ExtraButtons,
            },
        };
    }
}
