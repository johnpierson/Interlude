using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Autodesk.DesignScript.Runtime;

namespace Interlude.Runtime;

/// <summary>
/// Remembers what each form was answered with, for the lifetime of the Dynamo process.
///
/// Only submitted results are stored. Cancelling deliberately does not overwrite the cache:
/// backing out of a dialog should not destroy the answers given the last time it was completed,
/// which is exactly what a user expects and exactly what the pattern this package replaces got
/// wrong.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
internal sealed class SessionStore
{
    private readonly ConcurrentDictionary<string, FormResult> _results =
        new(StringComparer.Ordinal);

    /// <summary>The store used by <c>Form.Show</c>.</summary>
    public static SessionStore Instance { get; } = new();

    /// <summary>How many forms currently have remembered answers.</summary>
    public int Count => _results.Count;

    /// <summary>Retrieves the last submitted result for a form.</summary>
    public bool TryGet(string formId, out FormResult? result)
    {
        if (string.IsNullOrEmpty(formId))
        {
            result = null;
            return false;
        }

        return _results.TryGetValue(formId, out result);
    }

    /// <summary>The remembered values for a form, or null.</summary>
    public IReadOnlyDictionary<string, object?>? TryGetValues(string formId)
        => TryGet(formId, out FormResult? result) ? result!.Values : null;

    /// <summary>Records a form's result.</summary>
    public void Save(string formId, FormResult result)
    {
        if (string.IsNullOrEmpty(formId) || result is null)
        {
            return;
        }

        if (!result.WasSubmitted)
        {
            // Cancelling is not an answer, so it must not replace one.
            return;
        }

        _results[formId] = result;
    }

    /// <summary>Forgets one form's result.</summary>
    public void Remove(string formId)
    {
        if (!string.IsNullOrEmpty(formId))
        {
            _results.TryRemove(formId, out _);
        }
    }

    /// <summary>Forgets everything.</summary>
    public void Clear() => _results.Clear();
}
