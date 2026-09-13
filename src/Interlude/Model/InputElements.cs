using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.DesignScript.Runtime;
using Interlude.Conditions;

namespace Interlude.Model;

/// <summary>A single or multi-line text field.</summary>
[IsVisibleInDynamoLibrary(false)]
public sealed record TextBoxElement : InputElement
{
    /// <summary>Grey prompt shown while the field is empty.</summary>
    public string? Placeholder { get; init; }

    public bool IsMultiline { get; init; }

    /// <summary>Visible height of a multi-line field, in lines.</summary>
    public int Lines { get; init; } = 4;

    public int? MaxLength { get; init; }

    /// <summary>Wraps long lines instead of scrolling sideways.</summary>
    public bool WrapText { get; init; } = true;

    public override object? GetFallbackValue() => string.Empty;

    public override object? Coerce(object? value) => ValueOps.ToStringInvariant(value);
}

/// <summary>A masked text field. The value is a plain string once the form is submitted.</summary>
[IsVisibleInDynamoLibrary(false)]
public sealed record PasswordElement : InputElement
{
    public string? Placeholder { get; init; }

    public int? MaxLength { get; init; }

    public override object? GetFallbackValue() => string.Empty;

    public override object? Coerce(object? value) => ValueOps.ToStringInvariant(value);
}

/// <summary>A decimal number field with optional spinner buttons.</summary>
[IsVisibleInDynamoLibrary(false)]
public sealed record NumericElement : InputElement
{
    public double? Minimum { get; init; }

    public double? Maximum { get; init; }

    /// <summary>Step applied by the spinner buttons and the arrow keys.</summary>
    public double Increment { get; init; } = 1d;

    public int DecimalPlaces { get; init; } = 2;

    /// <summary>Suffix shown inside the field, such as "mm" or "%".</summary>
    public string? Unit { get; init; }

    public bool ShowSpinner { get; init; } = true;

    public override object? GetFallbackValue() => Minimum ?? 0d;

    public override object? Coerce(object? value)
    {
        // Deliberately not clamped: clamping mid-keystroke fights the user as they type "-"
        // or delete a digit. Bounds are enforced by RangeRule at validation time.
        return ValueOps.TryToDouble(value, out double number) ? number : Minimum ?? 0d;
    }
}

/// <summary>A whole-number field.</summary>
[IsVisibleInDynamoLibrary(false)]
public sealed record IntegerElement : InputElement
{
    public int? Minimum { get; init; }

    public int? Maximum { get; init; }

    public int Increment { get; init; } = 1;

    public string? Unit { get; init; }

    public bool ShowSpinner { get; init; } = true;

    public override object? GetFallbackValue() => Minimum ?? 0;

    public override object? Coerce(object? value)
    {
        if (!ValueOps.TryToDouble(value, out double number))
        {
            return Minimum ?? 0;
        }

        double rounded = Math.Round(number, MidpointRounding.AwayFromZero);
        if (rounded > int.MaxValue)
        {
            return int.MaxValue;
        }

        if (rounded < int.MinValue)
        {
            return int.MinValue;
        }

        return (int)rounded;
    }
}

/// <summary>A number chosen by dragging along a track.</summary>
[IsVisibleInDynamoLibrary(false)]
public sealed record SliderElement : InputElement
{
    public double Minimum { get; init; }

    public double Maximum { get; init; } = 100d;

    /// <summary>Snap increment. Zero means continuous.</summary>
    public double Step { get; init; } = 1d;

    public int DecimalPlaces { get; init; } = 2;

    /// <summary>Shows the current number beside the track.</summary>
    public bool ShowValue { get; init; } = true;

    /// <summary>Shows tick marks at each <see cref="Step"/>.</summary>
    public bool ShowTicks { get; init; }

    public override object? GetFallbackValue() => Minimum;

    public override object? Coerce(object? value)
    {
        if (!ValueOps.TryToDouble(value, out double number))
        {
            return Minimum;
        }

        // A slider physically cannot represent an out-of-range value, so unlike a numeric
        // field it does clamp: otherwise the stored value and the visible thumb disagree.
        double low = Math.Min(Minimum, Maximum);
        double high = Math.Max(Minimum, Maximum);
        return Math.Min(high, Math.Max(low, number));
    }
}

/// <summary>Shared behaviour for the controls that pick from a fixed list of options.</summary>
[IsVisibleInDynamoLibrary(false)]
public abstract record OptionInputElement : InputElement
{
    public IReadOnlyList<OptionItem> Options { get; init; } = Array.Empty<OptionItem>();

    /// <summary>Starts on the first option rather than with nothing chosen.</summary>
    public bool SelectFirstByDefault { get; init; } = true;

    public override object? GetFallbackValue()
        => SelectFirstByDefault && Options.Count > 0 ? Options[0].Value : null;

    /// <summary>
    /// Snaps an incoming value onto one of the options, so the stored value is always the
    /// author's original object rather than a look-alike. Matching a display string as well
    /// means a saved form or a graph-supplied default written as text still resolves.
    /// </summary>
    public override object? Coerce(object? value)
    {
        if (value is null)
        {
            return null;
        }

        OptionItem? match = OptionItem.Find(Options, value);
        if (match is not null)
        {
            return match.Value;
        }

        string text = ValueOps.ToStringInvariant(value);
        match = Options.FirstOrDefault(option =>
            string.Equals(option.Display, text, StringComparison.Ordinal));

        return match is not null ? match.Value : null;
    }
}

/// <summary>A drop-down list.</summary>
[IsVisibleInDynamoLibrary(false)]
public sealed record DropdownElement : OptionInputElement
{
    /// <summary>Grey prompt shown while nothing is chosen.</summary>
    public string? Placeholder { get; init; }

    /// <summary>Lets the user type a value that is not in the list.</summary>
    public bool IsEditable { get; init; }

    /// <summary>Adds a type-ahead filter box to the drop-down.</summary>
    public bool ShowSearch { get; init; }

    public override object? Coerce(object? value)
    {
        object? snapped = base.Coerce(value);

        // An editable drop-down is half text box: text the user invented is a real answer.
        if (snapped is null && IsEditable && !ValueOps.IsEmpty(value))
        {
            return ValueOps.ToStringInvariant(value);
        }

        return snapped;
    }
}

/// <summary>A set of mutually exclusive radio buttons.</summary>
[IsVisibleInDynamoLibrary(false)]
public sealed record RadioGroupElement : OptionInputElement
{
    public LayoutOrientation Orientation { get; init; } = LayoutOrientation.Vertical;

    /// <summary>Arranges the buttons into this many columns. Zero lays them out in one line or column.</summary>
    public int Columns { get; init; }
}

/// <summary>A tick box.</summary>
[IsVisibleInDynamoLibrary(false)]
public sealed record CheckBoxElement : InputElement
{
    /// <summary>Text shown beside the box, which is usually more natural than a separate label.</summary>
    public string? Content { get; init; }

    public override object? GetFallbackValue() => false;

    public override object? Coerce(object? value) => ValueOps.ToBool(value);
}

/// <summary>An on/off switch.</summary>
[IsVisibleInDynamoLibrary(false)]
public sealed record ToggleElement : InputElement
{
    /// <summary>Text shown when the switch is on.</summary>
    public string? OnText { get; init; }

    /// <summary>Text shown when the switch is off.</summary>
    public string? OffText { get; init; }

    public override object? GetFallbackValue() => false;

    public override object? Coerce(object? value) => ValueOps.ToBool(value);
}

/// <summary>
/// A list the user picks from, optionally choosing several at once.
/// A single-select list stores the chosen object; a multi-select list stores a list of objects.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
public sealed record ListSelectionElement : InputElement
{
    public IReadOnlyList<OptionItem> Options { get; init; } = Array.Empty<OptionItem>();

    public bool AllowMultiple { get; init; } = true;

    /// <summary>Adds a filter box above the list.</summary>
    public bool ShowSearch { get; init; } = true;

    /// <summary>Adds "select all" and "select none" buttons. Multi-select only.</summary>
    public bool ShowSelectAll { get; init; } = true;

    /// <summary>How many rows are visible before the list scrolls.</summary>
    public int VisibleRows { get; init; } = 6;

    public override object? GetFallbackValue()
        => AllowMultiple ? Array.Empty<object?>() : null;

    public override object? Coerce(object? value)
    {
        if (!AllowMultiple)
        {
            object? single = ValueOps.TryAsSequence(value, out IReadOnlyList<object?> items)
                ? items.FirstOrDefault()
                : value;

            return OptionItem.Find(Options, single)?.Value;
        }

        List<object?> selected = new();
        foreach (object? candidate in ValueOps.AsList(value))
        {
            OptionItem? match = OptionItem.Find(Options, candidate);
            if (match is not null && !selected.Any(existing => ValueOps.AreStateEqual(existing, match.Value)))
            {
                selected.Add(match.Value);
            }
        }

        return selected;
    }
}

/// <summary>
/// A hierarchy the user picks from. Like a list, a single-select tree stores the chosen object
/// and a multi-select tree stores a list of objects.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
public sealed record TreeSelectionElement : InputElement
{
    public IReadOnlyList<TreeNode> Roots { get; init; } = Array.Empty<TreeNode>();

    public bool AllowMultiple { get; init; } = true;

    /// <summary>Shows a tick box beside each node. Implied by <see cref="AllowMultiple"/>.</summary>
    public bool ShowCheckBoxes { get; init; } = true;

    public bool ExpandAll { get; init; }

    public bool ShowSearch { get; init; } = true;

    /// <summary>Ticking a branch ticks everything beneath it.</summary>
    public bool CheckChildrenWithParent { get; init; } = true;

    public override object? GetFallbackValue()
        => AllowMultiple ? Array.Empty<object?>() : null;

    public override object? Coerce(object? value)
    {
        IReadOnlyList<TreeNode> selectable = Flatten().Where(node => node.IsSelectable).ToList();

        if (!AllowMultiple)
        {
            object? single = ValueOps.TryAsSequence(value, out IReadOnlyList<object?> items)
                ? items.FirstOrDefault()
                : value;

            TreeNode? found = selectable.FirstOrDefault(node => ValueOps.AreStateEqual(node.Value, single));
            return found?.Value;
        }

        List<object?> selected = new();
        foreach (object? candidate in ValueOps.AsList(value))
        {
            TreeNode? found = selectable.FirstOrDefault(node => ValueOps.AreStateEqual(node.Value, candidate));
            if (found is not null && !selected.Any(existing => ValueOps.AreStateEqual(existing, found.Value)))
            {
                selected.Add(found.Value);
            }
        }

        return selected;
    }

    /// <summary>Every node in the tree, depth first.</summary>
    public IEnumerable<TreeNode> Flatten() => Roots.SelectMany(root => root.Descend());
}

/// <summary>A calendar field. The value is a <see cref="DateTime"/>, or null when unanswered.</summary>
[IsVisibleInDynamoLibrary(false)]
public sealed record DatePickerElement : InputElement
{
    public DateTime? Minimum { get; init; }

    public DateTime? Maximum { get; init; }

    /// <summary>Adds a time-of-day field beside the calendar.</summary>
    public bool IncludeTime { get; init; }

    /// <summary>Starts on today's date rather than empty.</summary>
    public bool DefaultToToday { get; init; }

    public override object? GetFallbackValue()
        => DefaultToToday ? DateTime.Today : null;

    public override object? Coerce(object? value)
    {
        if (ValueOps.IsEmpty(value))
        {
            return null;
        }

        return ValueOps.TryToDateTime(value, out DateTime date) ? date : null;
    }
}

/// <summary>A colour field. The value is an <see cref="RgbColor"/>.</summary>
[IsVisibleInDynamoLibrary(false)]
public sealed record ColorPickerElement : InputElement
{
    /// <summary>Adds an opacity slider.</summary>
    public bool ShowAlpha { get; init; }

    /// <summary>Swatches offered above the full picker.</summary>
    public IReadOnlyList<RgbColor> Presets { get; init; } = Array.Empty<RgbColor>();

    public override object? GetFallbackValue() => RgbColor.Black;

    public override object? Coerce(object? value)
    {
        switch (value)
        {
            case RgbColor color:
                return ShowAlpha ? color : color with { Alpha = 255 };
            case string text when RgbColor.TryParse(text, out RgbColor parsed):
                return ShowAlpha ? parsed : parsed with { Alpha = 255 };
            default:
                return RgbColor.Black;
        }
    }
}

/// <summary>
/// A file path with a Browse button. Stores a string, or a list of strings when
/// <see cref="AllowMultiple"/> is set.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
public sealed record FilePickerElement : InputElement
{
    /// <summary>Windows dialog filter, for example <c>"Revit files|*.rvt|All files|*.*"</c>.</summary>
    public string Filter { get; init; } = "All files|*.*";

    public bool AllowMultiple { get; init; }

    /// <summary>Shows a save dialog instead of an open dialog.</summary>
    public bool IsSaveDialog { get; init; }

    public string? InitialDirectory { get; init; }

    /// <summary>Default extension applied by the save dialog, without the dot.</summary>
    public string? DefaultExtension { get; init; }

    public override object? GetFallbackValue()
        => AllowMultiple ? Array.Empty<object?>() : string.Empty;

    public override object? Coerce(object? value)
    {
        if (!AllowMultiple)
        {
            if (ValueOps.TryAsSequence(value, out IReadOnlyList<object?> items))
            {
                return items.Count > 0 ? ValueOps.ToStringInvariant(items[0]) : string.Empty;
            }

            return ValueOps.ToStringInvariant(value);
        }

        return ValueOps.AsList(value)
            .Select(ValueOps.ToStringInvariant)
            .Where(path => path.Length > 0)
            .Cast<object?>()
            .ToList();
    }
}

/// <summary>A folder path with a Browse button.</summary>
[IsVisibleInDynamoLibrary(false)]
public sealed record FolderPickerElement : InputElement
{
    public string? InitialDirectory { get; init; }

    public override object? GetFallbackValue() => string.Empty;

    public override object? Coerce(object? value) => ValueOps.ToStringInvariant(value);
}

/// <summary>
/// A button that lets the user pick elements in the host Revit model. A single-select field
/// stores the picked element; a multi-select field stores a list of elements. Outside Revit the
/// button renders disabled, so the same form still opens everywhere.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
public sealed record ModelSelectionElement : InputElement
{
    public bool AllowMultiple { get; init; } = true;

    /// <summary>Caption on the button. Null gets a stock "Select in model…".</summary>
    public string? ButtonText { get; init; }

    /// <summary>Text shown in Revit's status bar while picking.</summary>
    public string? Prompt { get; init; }

    public override object? GetFallbackValue()
        => AllowMultiple ? Array.Empty<object?>() : null;

    /// <summary>
    /// Elements pass through untouched — there is nothing to snap them onto and no way to
    /// inspect them without a Revit reference. Only the single/list shape is normalised.
    /// </summary>
    public override object? Coerce(object? value)
    {
        if (!AllowMultiple)
        {
            return ValueOps.TryAsSequence(value, out IReadOnlyList<object?> items)
                ? items.FirstOrDefault()
                : value;
        }

        return ValueOps.AsList(value)
            .Where(item => item is not null)
            .ToList();
    }
}
