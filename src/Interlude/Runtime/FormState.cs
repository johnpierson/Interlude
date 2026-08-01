using System;
using System.Collections.Generic;
using Autodesk.DesignScript.Runtime;
using Interlude.Conditions;
using Interlude.Model;

namespace Interlude.Runtime;

/// <summary>The live value of every field, and the only thing conditions are allowed to see.</summary>
[IsVisibleInDynamoLibrary(false)]
internal sealed class FormStateStore : IFormStateReader
{
    private readonly Dictionary<string, object?> _values = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public IReadOnlyCollection<string> Keys => _values.Keys;

    /// <inheritdoc />
    public object? GetValue(string key)
        => key is not null && _values.TryGetValue(key, out object? value) ? value : null;

    /// <inheritdoc />
    public bool TryGetValue(string key, out object? value)
    {
        if (key is null)
        {
            value = null;
            return false;
        }

        return _values.TryGetValue(key, out value);
    }

    /// <summary>Stores a value, reporting whether it actually differs from what was there.</summary>
    public bool Set(string key, object? value)
    {
        if (string.IsNullOrEmpty(key))
        {
            return false;
        }

        if (_values.TryGetValue(key, out object? existing) && ValueOps.AreEqual(existing, value))
        {
            return false;
        }

        _values[key] = value;
        return true;
    }

    /// <summary>A point-in-time copy of every value.</summary>
    public Dictionary<string, object?> Snapshot() => new(_values, StringComparer.Ordinal);
}

/// <summary>What changed about an element in one propagation pass.</summary>
[Flags]
[IsVisibleInDynamoLibrary(false)]
internal enum StateChangeKind
{
    None = 0,
    Value = 1 << 0,
    Visibility = 1 << 1,
    Enabled = 1 << 2,
    Required = 1 << 3,
    Validation = 1 << 4,
}

/// <summary>
/// Everything the renderer needs to know about one element right now. Unlike the model, this is
/// mutable: it is the session's working copy, updated in place and handed to the view.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
internal sealed class ElementRuntimeState
{
    internal ElementRuntimeState(FormElement element)
    {
        Element = element;
    }

    /// <summary>The model element this state belongs to.</summary>
    public FormElement Element { get; }

    /// <summary>The element's result key. Empty for elements that produce no value.</summary>
    public string Key => Element.Key;

    /// <summary>
    /// False when the element's own <c>VisibleIf</c> fails or any ancestor is hidden. A hidden
    /// element is neither validated nor required, but its value still appears in the results.
    /// </summary>
    public bool IsVisible { get; internal set; } = true;

    /// <summary>False when the element's own <c>EnabledIf</c> fails or any ancestor is disabled.</summary>
    public bool IsEnabled { get; internal set; } = true;

    /// <summary>True when <c>RequiredIf</c> is satisfied and the element is visible.</summary>
    public bool IsRequired { get; internal set; }

    /// <summary>The element's current value, for inputs.</summary>
    public object? Value { get; internal set; }

    /// <summary>The failing rule's message, or null when the element is valid.</summary>
    public string? Error { get; internal set; }

    /// <summary>Whether the user has edited this element yet.</summary>
    public bool IsTouched { get; internal set; }

    /// <summary>True when no rule is failing.</summary>
    public bool IsValid => Error is null;
}

/// <summary>One element's change within a propagation batch.</summary>
[IsVisibleInDynamoLibrary(false)]
internal sealed class ElementStateChange
{
    internal ElementStateChange(ElementRuntimeState state, StateChangeKind kind)
    {
        State = state;
        Kind = kind;
    }

    public ElementRuntimeState State { get; }

    public FormElement Element => State.Element;

    public StateChangeKind Kind { get; }

    /// <summary>Convenience test for a particular kind of change.</summary>
    public bool Includes(StateChangeKind kind) => (Kind & kind) != 0;
}

/// <summary>
/// One coalesced batch of changes. The session raises exactly one of these per edit, no matter
/// how far the change cascaded, so the renderer applies a single consistent update rather than
/// re-laying-out the window once per affected field.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
internal sealed class FormStateChangedEventArgs : EventArgs
{
    internal FormStateChangedEventArgs(IReadOnlyList<ElementStateChange> changes)
    {
        Changes = changes;
    }

    public IReadOnlyList<ElementStateChange> Changes { get; }
}
