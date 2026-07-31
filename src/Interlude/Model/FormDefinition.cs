using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using Autodesk.DesignScript.Runtime;
using Interlude.Theming;

namespace Interlude.Model;

/// <summary>The submit / cancel strip along the bottom of a form.</summary>
[IsVisibleInDynamoLibrary(false)]
public sealed record FormButtons
{
    /// <summary>Submit and Cancel, both shown.</summary>
    public static readonly FormButtons Default = new();

    public string SubmitText { get; init; } = "Submit";

    public string CancelText { get; init; } = "Cancel";

    public bool ShowSubmit { get; init; } = true;

    public bool ShowCancel { get; init; } = true;

    /// <summary>Extra buttons placed to the left of Submit, each reporting its own tag.</summary>
    public IReadOnlyList<ButtonElement> ExtraButtons { get; init; } = Array.Empty<ButtonElement>();

    /// <summary>Escape cancels the form.</summary>
    public bool CloseOnEscape { get; init; } = true;
}

/// <summary>How the form's window is sized and framed.</summary>
[IsVisibleInDynamoLibrary(false)]
public sealed record WindowOptions
{
    /// <summary>A 420-pixel-wide resizable dialog that grows to fit its content.</summary>
    public static readonly WindowOptions Default = new();

    public double Width { get; init; } = 420d;

    /// <summary>Null sizes the window to its content, up to <see cref="MaxHeight"/>.</summary>
    public double? Height { get; init; }

    public double MinWidth { get; init; } = 280d;

    public double MinHeight { get; init; } = 120d;

    public double MaxHeight { get; init; } = 800d;

    public bool IsResizable { get; init; } = true;

    /// <summary>
    /// Kept false by default. A modal dialog owned by Revit does not belong in the taskbar,
    /// where it reads as a second application.
    /// </summary>
    public bool ShowInTaskbar { get; init; }

    /// <summary>
    /// Kept false by default on purpose. Interlude owns its window to the host instead, which
    /// keeps the dialog above Revit without floating it above unrelated applications.
    /// </summary>
    public bool Topmost { get; init; }

    /// <summary>Optional path to a window icon.</summary>
    public string? IconPath { get; init; }
}

/// <summary>
/// A whole form, as data.
///
/// This is the contract that outlives every other decision in the package: a form is a value
/// that can be built by nodes, saved to JSON, diffed in a pull request, replayed in tests and
/// rendered by something other than WPF one day.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
public sealed record FormDefinition
{
    /// <summary>The schema version written by this build.</summary>
    public const int CurrentSchemaVersion = 1;

    /// <summary>
    /// Version of the serialized shape. Readers refuse anything newer than they understand
    /// rather than guessing at fields they have never seen.
    /// </summary>
    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    public string Title { get; init; } = string.Empty;

    /// <summary>Optional paragraph shown under the title.</summary>
    public string? Description { get; init; }

    public IReadOnlyList<FormElement> Elements { get; init; } = Array.Empty<FormElement>();

    public FormButtons Buttons { get; init; } = FormButtons.Default;

    public WindowOptions Window { get; init; } = WindowOptions.Default;

    public ThemeDefinition Theme { get; init; } = ThemeDefinition.Default;

    /// <summary>
    /// Identifies this form across runs, for remembered values and for the re-entrancy latch.
    /// When left empty, <see cref="ResolveFormId"/> derives a stable one from the form's shape.
    /// </summary>
    public string FormId { get; init; } = string.Empty;

    /// <summary>Pre-fills the form with the previous run's answers.</summary>
    public bool RememberValues { get; init; } = true;

    /// <summary>
    /// What to do with no UI available. False (the default) throws with a clear explanation;
    /// true returns every field's default so a scheduled or command-line run can proceed.
    /// </summary>
    public bool HeadlessUseDefaults { get; init; }

    /// <summary>Every element in the tree, depth first, parents before children.</summary>
    public IEnumerable<FormElement> AllElements() => ElementTree.Descend(Elements);

    /// <summary>Every element that contributes a value to the results, in document order.</summary>
    public IEnumerable<InputElement> Inputs() => AllElements().OfType<InputElement>();

    /// <summary>
    /// The identity used to remember values between runs: the explicit <see cref="FormId"/> when
    /// given, otherwise a hash of the title and the ordered field keys. Basing the fallback on
    /// shape means editing a label does not silently inherit answers from a different form,
    /// while re-running the same graph does.
    /// </summary>
    public string ResolveFormId()
    {
        if (!string.IsNullOrWhiteSpace(FormId))
        {
            return FormId.Trim();
        }

        StringBuilder builder = new();
        builder.Append(Title);

        foreach (InputElement input in Inputs())
        {
            builder.Append('\u001F').Append(input.Key);
        }

        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));

        StringBuilder identity = new("auto-", 21);
        for (int i = 0; i < 8; i++)
        {
            identity.Append(hash[i].ToString("x2", CultureInfo.InvariantCulture));
        }

        return identity.ToString();
    }

    /// <summary>
    /// Assigns a key to every input that does not have one and resolves duplicates, returning
    /// the corrected form. Always call this before building a session: keys are the contract
    /// between the form and the graph reading its results.
    /// </summary>
    public FormDefinition WithResolvedKeys() => FormKeys.Assign(this);
}
