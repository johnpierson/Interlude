using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.DesignScript.Runtime;
using Interlude.Conditions;

namespace Interlude.Model;

/// <summary>
/// One choice in a dropdown, radio group or list.
///
/// <see cref="Value"/> is deliberately <c>object</c>: a graph passes in Revit elements, family
/// types or anything else, and selecting an option hands that same object back — never its
/// display string. That round trip is the main thing dropdown nodes exist to preserve.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
public sealed record OptionItem
{
    /// <summary>What selecting this option puts into the form's results.</summary>
    public object? Value { get; init; }

    /// <summary>What the user reads.</summary>
    public string Display { get; init; } = string.Empty;

    /// <summary>Optional secondary line shown beneath <see cref="Display"/>.</summary>
    public string? Description { get; init; }

    /// <summary>Optional path to a small image shown beside the option.</summary>
    public string? IconPath { get; init; }

    /// <summary>A disabled option is visible but cannot be chosen.</summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>An option whose display text is its own value.</summary>
    public static OptionItem FromValue(object? value)
        => new() { Value = value, Display = ValueOps.ToStringInvariant(value) };

    /// <summary>
    /// Pairs values with display names. Missing display names fall back to the value's own text,
    /// and extra display names are ignored, so a graph with mismatched list lengths still shows
    /// a usable form instead of throwing.
    /// </summary>
    public static IReadOnlyList<OptionItem> Pair(
        IReadOnlyList<object?>? values,
        IReadOnlyList<string>? displayNames)
    {
        if (values is null || values.Count == 0)
        {
            return Array.Empty<OptionItem>();
        }

        List<OptionItem> options = new(values.Count);
        for (int i = 0; i < values.Count; i++)
        {
            string display = displayNames is not null && i < displayNames.Count && displayNames[i] is not null
                ? displayNames[i]
                : ValueOps.ToStringInvariant(values[i]);

            options.Add(new OptionItem { Value = values[i], Display = display });
        }

        return options;
    }

    /// <summary>Finds the option matching <paramref name="value"/>, or null.</summary>
    public static OptionItem? Find(IReadOnlyList<OptionItem> options, object? value)
        => options.FirstOrDefault(option => ValueOps.AreEqual(option.Value, value));

    public override string ToString() => Display;
}

/// <summary>
/// One node of a <see cref="TreeSelectionElement"/>. Children are themselves tree nodes, so a
/// whole hierarchy is one immutable value.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
public sealed record TreeNode
{
    /// <summary>What selecting this node puts into the form's results.</summary>
    public object? Value { get; init; }

    /// <summary>What the user reads.</summary>
    public string Display { get; init; } = string.Empty;

    public IReadOnlyList<TreeNode> Children { get; init; } = Array.Empty<TreeNode>();

    /// <summary>Whether the node starts expanded.</summary>
    public bool IsExpanded { get; init; }

    /// <summary>A disabled node is visible but cannot be chosen; its children still can.</summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>
    /// A branch node exists to group its children and is usually not itself a selectable answer.
    /// </summary>
    public bool IsSelectable { get; init; } = true;

    /// <summary>This node and every node beneath it, depth first.</summary>
    public IEnumerable<TreeNode> Descend()
    {
        yield return this;

        foreach (TreeNode child in Children)
        {
            foreach (TreeNode descendant in child.Descend())
            {
                yield return descendant;
            }
        }
    }

    public override string ToString() => Display;
}
