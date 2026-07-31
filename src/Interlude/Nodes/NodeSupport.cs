using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.DesignScript.Runtime;
using Interlude.Conditions;
using Interlude.Model;

namespace Interlude;

/// <summary>
/// Shared coercions for the node facades.
///
/// Node ports are loosely typed by nature — a graph can put anything on any wire — so every
/// facade funnels its inputs through here rather than each node inventing its own idea of what
/// an empty string or a missing number means.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
internal static class NodeSupport
{
    /// <summary>Treats empty and whitespace-only text as "not supplied".</summary>
    internal static string? OrNull(string? text)
        => string.IsNullOrWhiteSpace(text) ? null : text;

    /// <summary>Reads an optional numeric port, where null means "no bound".</summary>
    internal static double? OptionalDouble(object? value)
        => value is null ? null : ValueOps.TryToDouble(value, out double number) ? number : null;

    /// <summary>Reads an optional whole-number port.</summary>
    internal static int? OptionalInt(object? value)
    {
        double? number = OptionalDouble(value);
        return number.HasValue ? (int)Math.Round(number.Value, MidpointRounding.AwayFromZero) : null;
    }

    /// <summary>Reads an optional date port.</summary>
    internal static DateTime? OptionalDate(object? value)
        => value is null ? null : ValueOps.TryToDateTime(value, out DateTime date) ? date : null;

    /// <summary>Reads a colour supplied as an <see cref="RgbColor"/> or as a hex string.</summary>
    internal static RgbColor? OptionalColor(object? value) => value switch
    {
        null => null,
        RgbColor colour => colour,
        string text when RgbColor.TryParse(text, out RgbColor parsed) => parsed,
        _ => null,
    };

    /// <summary>Flattens a port that may hold one item, a list, or nested lists.</summary>
    internal static IReadOnlyList<object?> Items(object? value) => ValueOps.AsList(value);

    /// <summary>Pairs values with display names, tolerating mismatched list lengths.</summary>
    internal static IReadOnlyList<OptionItem> Options(object? items, object? displayNames)
    {
        IReadOnlyList<object?> values = Items(items);

        IReadOnlyList<string>? names = displayNames is null
            ? null
            : Items(displayNames).Select(ValueOps.ToStringInvariant).ToList();

        return OptionItem.Pair(values, names);
    }

    /// <summary>Filters nulls out of an element list so one unwired port cannot break a form.</summary>
    internal static IReadOnlyList<FormElement> Elements(IEnumerable<FormElement>? elements)
        => elements?.Where(element => element is not null).ToList() ?? new List<FormElement>();

    /// <summary>
    /// Flattens whatever arrived on an elements port. Used by <c>Form.Show</c>, which takes
    /// <c>object</c> so that a graph passing a nested list still produces a form rather than a
    /// type error three nodes upstream of the mistake.
    /// </summary>
    internal static IReadOnlyList<FormElement> FlattenElements(object? value)
        => ElementTree.Flatten(value);

    /// <summary>Parses the compact grid column syntax: <c>"auto, *, 2*, 120"</c>.</summary>
    internal static IReadOnlyList<GridTrack> Tracks(string? specification)
    {
        if (string.IsNullOrWhiteSpace(specification))
        {
            return new[] { GridTrack.Star };
        }

        GridTrack[] tracks = specification!
            .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(GridTrack.Parse)
            .ToArray();

        return tracks.Length > 0 ? tracks : new[] { GridTrack.Star };
    }

    /// <summary>Combines conditions, treating a single condition and a list of them alike.</summary>
    internal static IReadOnlyList<ConditionExpr> Conditions(object? value)
    {
        if (value is ConditionExpr single)
        {
            return new[] { single };
        }

        return Items(value).OfType<ConditionExpr>().ToList();
    }

    /// <summary>Wraps a plain value as a computed expression, passing expressions through.</summary>
    internal static ComputedValue AsComputed(object? value) => value switch
    {
        ComputedValue computed => computed,
        _ => new ConstantComputed { Value = value },
    };

    /// <summary>
    /// Applies the shared trailing options every input node carries. Keeping them in one place
    /// is what makes "key, tooltip, helpText" mean the same thing on all twenty of them.
    /// </summary>
    internal static TElement WithCommon<TElement>(
        this TElement element,
        string? key,
        string? tooltip,
        string? helpText)
        where TElement : FormElement
        => (TElement)(element with
        {
            Key = OrNull(key) ?? element.Key,
            Tooltip = OrNull(tooltip),
            HelpText = OrNull(helpText),
        });
}
