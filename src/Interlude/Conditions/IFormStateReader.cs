using System.Collections.Generic;
using Autodesk.DesignScript.Runtime;

namespace Interlude.Conditions;

/// <summary>
/// Read-only view of the live form state. Conditions, computed values and validation
/// rules see the form through this interface only, which is what makes the whole
/// evaluation layer testable without a window.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
public interface IFormStateReader
{
    /// <summary>Every key currently present in the form.</summary>
    IReadOnlyCollection<string> Keys { get; }

    /// <summary>Gets the current value for <paramref name="key"/>, or null when the key is unknown.</summary>
    object? GetValue(string key);

    /// <summary>Gets the current value for <paramref name="key"/>, reporting whether the key exists at all.</summary>
    bool TryGetValue(string key, out object? value);
}
