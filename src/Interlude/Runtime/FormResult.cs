using System;
using System.Collections.Generic;
using Autodesk.DesignScript.Runtime;
using Interlude.Model;

namespace Interlude.Runtime;

/// <summary>Well-known values of <see cref="FormResult.ButtonClicked"/>.</summary>
[IsVisibleInDynamoLibrary(false)]
public static class FormButtonNames
{
    public const string Submit = "submit";

    public const string Cancel = "cancel";

    /// <summary>The window was closed with the title-bar X or the Escape key.</summary>
    public const string Closed = "closed";

    /// <summary>The form never appeared, because the trigger input was false.</summary>
    public const string Skipped = "skipped";
}

/// <summary>
/// The answers a form came back with.
///
/// <see cref="Values"/> always contains an entry for every field, including when the user
/// cancelled. Returning nulls on cancel — the pattern Interlude is replacing — pushes a null
/// check into every downstream node and produces confusing failures three nodes later; a
/// cancelled form here returns each field's default and says so via <see cref="WasCancelled"/>.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
public sealed class FormResult
{
    internal FormResult(
        IReadOnlyDictionary<string, object?> values,
        bool wasSubmitted,
        string buttonClicked,
        FormDefinition? definition)
    {
        Values = values;
        WasSubmitted = wasSubmitted;
        ButtonClicked = buttonClicked;
        Definition = definition;
    }

    /// <summary>Every field's value, keyed by its result key. Never null and never missing a key.</summary>
    public IReadOnlyDictionary<string, object?> Values { get; }

    /// <summary>True when the user completed the form rather than backing out of it.</summary>
    public bool WasSubmitted { get; }

    /// <summary>True when the user cancelled, closed or skipped the form.</summary>
    public bool WasCancelled => !WasSubmitted;

    /// <summary>
    /// Which button ended the form: <see cref="FormButtonNames.Submit"/>,
    /// <see cref="FormButtonNames.Cancel"/>, <see cref="FormButtonNames.Closed"/>,
    /// <see cref="FormButtonNames.Skipped"/>, or a custom button's tag.
    /// </summary>
    public string ButtonClicked { get; }

    /// <summary>The form that produced this result, for nodes that want to inspect it.</summary>
    public FormDefinition? Definition { get; }

    /// <summary>Looks a value up, returning null when the key is not part of this form.</summary>
    public object? Get(string key)
        => key is not null && Values.TryGetValue(key, out object? value) ? value : null;

    /// <summary>Whether this form has a field with the given key.</summary>
    public bool HasKey(string key) => key is not null && Values.ContainsKey(key);

    /// <summary>Builds a cancelled result carrying every field's default value.</summary>
    public static FormResult Cancelled(FormDefinition definition, string buttonClicked = FormButtonNames.Cancel)
    {
        if (definition is null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        Dictionary<string, object?> defaults = new(StringComparer.Ordinal);
        foreach (InputElement input in definition.Inputs())
        {
            if (!string.IsNullOrEmpty(input.Key))
            {
                defaults[input.Key] = input.GetEffectiveDefault();
            }
        }

        return new FormResult(defaults, false, buttonClicked, definition);
    }
}
