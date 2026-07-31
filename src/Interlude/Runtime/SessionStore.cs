using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Autodesk.DesignScript.Runtime;

namespace Interlude.Runtime;

/// <summary>
/// Where a form's answers are kept between runs. The seam exists so that opt-in disk
/// persistence can be added later without the rest of the package noticing.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
public interface IResultStore
{
    /// <summary>Retrieves the last submitted result for a form.</summary>
    bool TryGet(string formId, out FormResult? result);

    /// <summary>Records a form's result.</summary>
    void Save(string formId, FormResult result);

    /// <summary>Forgets one form's result.</summary>
    void Remove(string formId);

    /// <summary>Forgets everything.</summary>
    void Clear();
}

/// <summary>
/// Remembers what each form was answered with, for the lifetime of the Dynamo process.
///
/// Only submitted results are stored. Cancelling deliberately does not overwrite the cache:
/// backing out of a dialog should not destroy the answers given the last time it was completed,
/// which is exactly what a user expects and exactly what the pattern this package replaces got
/// wrong.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
public sealed class SessionStore : IResultStore
{
    private readonly ConcurrentDictionary<string, FormResult> _results =
        new(StringComparer.Ordinal);

    /// <summary>The store used by <c>Form.Show</c>.</summary>
    public static SessionStore Instance { get; } = new();

    /// <summary>How many forms currently have remembered answers.</summary>
    public int Count => _results.Count;

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <inheritdoc />
    public void Remove(string formId)
    {
        if (!string.IsNullOrEmpty(formId))
        {
            _results.TryRemove(formId, out _);
        }
    }

    /// <inheritdoc />
    public void Clear() => _results.Clear();
}
