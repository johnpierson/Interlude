using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Autodesk.DesignScript.Runtime;

namespace Interlude.Conditions;

/// <summary>
/// The one place where loosely-typed form values are compared, coerced and stringified.
///
/// Everything here is <see cref="CultureInfo.InvariantCulture"/> on purpose. Values arrive
/// from Dynamo, from JSON and from text boxes typed by users on de-DE or fr-FR machines; if
/// parsing followed the current culture then "1,5" would mean 1.5 on one machine and 15 on
/// another and a saved form would stop round-tripping. Culture belongs in the renderer, at
/// display time, and nowhere else.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
public static class ValueOps
{
    /// <summary>Renders a value as an invariant string. Sequences become comma-joined lists.</summary>
    public static string ToStringInvariant(object? value)
    {
        switch (value)
        {
            case null:
                return string.Empty;
            case string s:
                return s;
            case bool b:
                return b ? "true" : "false";
            case double d:
                return d.ToString("R", CultureInfo.InvariantCulture);
            case float f:
                return f.ToString("R", CultureInfo.InvariantCulture);
            case decimal m:
                return m.ToString(CultureInfo.InvariantCulture);
            case DateTime dt:
                return dt.ToString("o", CultureInfo.InvariantCulture);
            case IFormattable formattable:
                return formattable.ToString(null, CultureInfo.InvariantCulture);
            default:
                if (TryAsSequence(value, out IReadOnlyList<object?> items))
                {
                    return string.Join(", ", items.Select(ToStringInvariant));
                }

                return value.ToString() ?? string.Empty;
        }
    }

    /// <summary>Attempts a numeric reading of <paramref name="value"/>.</summary>
    public static bool TryToDouble(object? value, out double result)
    {
        switch (value)
        {
            case null:
                result = 0d;
                return false;
            case double d:
                result = d;
                return true;
            case float f:
                result = f;
                return true;
            case decimal m:
                result = (double)m;
                return true;
            case int i:
                result = i;
                return true;
            case long l:
                result = l;
                return true;
            case short sh:
                result = sh;
                return true;
            case byte by:
                result = by;
                return true;
            case bool b:
                result = b ? 1d : 0d;
                return true;
            case string s:
                return double.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands,
                    CultureInfo.InvariantCulture, out result);
            default:
                result = 0d;
                return false;
        }
    }

    /// <summary>Numeric reading of <paramref name="value"/>, or <paramref name="fallback"/>.</summary>
    public static double ToDouble(object? value, double fallback = 0d)
        => TryToDouble(value, out double d) ? d : fallback;

    /// <summary>
    /// Truthiness. Null, false, 0, empty strings and empty sequences are false; the strings
    /// "true"/"yes"/"1"/"on" are true. Everything else present and non-empty is true.
    /// </summary>
    public static bool ToBool(object? value)
    {
        switch (value)
        {
            case null:
                return false;
            case bool b:
                return b;
            case string s:
                if (s.Length == 0)
                {
                    return false;
                }

                if (bool.TryParse(s, out bool parsed))
                {
                    return parsed;
                }

                if (s.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
                    s.Equals("on", StringComparison.OrdinalIgnoreCase) ||
                    s.Equals("1", StringComparison.Ordinal))
                {
                    return true;
                }

                if (s.Equals("no", StringComparison.OrdinalIgnoreCase) ||
                    s.Equals("off", StringComparison.OrdinalIgnoreCase) ||
                    s.Equals("0", StringComparison.Ordinal))
                {
                    return false;
                }

                return true;
            default:
                if (TryToDouble(value, out double d))
                {
                    return d != 0d;
                }

                if (TryAsSequence(value, out IReadOnlyList<object?> items))
                {
                    return items.Count > 0;
                }

                return true;
        }
    }

    /// <summary>
    /// True when the value carries no user input: null, empty/whitespace text, or an empty sequence.
    /// Note that <c>false</c> and <c>0</c> are values, not emptiness.
    /// </summary>
    public static bool IsEmpty(object? value)
    {
        switch (value)
        {
            case null:
                return true;
            case string s:
                return s.Trim().Length == 0;
            default:
                return TryAsSequence(value, out IReadOnlyList<object?> items) && items.Count == 0;
        }
    }

    /// <summary>
    /// Equality across loosely-typed values: numbers compare numerically, booleans compare
    /// as booleans, sequences compare element-wise, everything else compares as invariant text.
    /// </summary>
    public static bool AreEqual(object? left, object? right, bool ignoreCase = false)
    {
        if (left is null && right is null)
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is bool || right is bool)
        {
            return ToBool(left) == ToBool(right);
        }

        bool leftIsSequence = TryAsSequence(left, out IReadOnlyList<object?> leftItems);
        bool rightIsSequence = TryAsSequence(right, out IReadOnlyList<object?> rightItems);
        if (leftIsSequence || rightIsSequence)
        {
            if (!leftIsSequence || !rightIsSequence || leftItems.Count != rightItems.Count)
            {
                return false;
            }

            for (int i = 0; i < leftItems.Count; i++)
            {
                if (!AreEqual(leftItems[i], rightItems[i], ignoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        if (IsNumeric(left) && IsNumeric(right) &&
            TryToDouble(left, out double ld) && TryToDouble(right, out double rd))
        {
            return NearlyEqual(ld, rd);
        }

        // Value objects (DateTime, RgbColor, boxed enums, Revit elements...) get a real shot
        // at equality before we fall back to comparing their rendered text.
        if (left.GetType() == right.GetType() && left.Equals(right))
        {
            return true;
        }

        return string.Equals(
            ToStringInvariant(left),
            ToStringInvariant(right),
            ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    }

    /// <summary>
    /// Ordering comparison. Returns false when the two values cannot be ordered, which makes
    /// "greater than" on nonsense inputs simply false rather than an exception mid-keystroke.
    /// </summary>
    public static bool TryCompare(object? left, object? right, out int comparison)
    {
        if (left is not null && right is not null &&
            IsNumeric(left) && IsNumeric(right) &&
            TryToDouble(left, out double ld) && TryToDouble(right, out double rd))
        {
            comparison = NearlyEqual(ld, rd) ? 0 : ld.CompareTo(rd);
            return true;
        }

        if (left is DateTime leftDate && TryToDateTime(right, out DateTime rightDate))
        {
            comparison = leftDate.CompareTo(rightDate);
            return true;
        }

        if (right is DateTime rd2 && TryToDateTime(left, out DateTime ld2))
        {
            comparison = ld2.CompareTo(rd2);
            return true;
        }

        if (left is string || right is string)
        {
            // Numeric text ("12" vs "9") should still order numerically.
            if (TryToDouble(left, out double lnum) && TryToDouble(right, out double rnum))
            {
                comparison = NearlyEqual(lnum, rnum) ? 0 : lnum.CompareTo(rnum);
                return true;
            }

            comparison = string.CompareOrdinal(ToStringInvariant(left), ToStringInvariant(right));
            return true;
        }

        comparison = 0;
        return false;
    }

    /// <summary>Parses a value as a date, accepting round-trip and common invariant formats.</summary>
    public static bool TryToDateTime(object? value, out DateTime result)
    {
        switch (value)
        {
            case DateTime dt:
                result = dt;
                return true;
            case DateTimeOffset dto:
                result = dto.LocalDateTime;
                return true;
            case string s:
                return DateTime.TryParse(s, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out result);
            default:
                result = default;
                return false;
        }
    }

    /// <summary>
    /// Treats a value as a sequence. Strings are deliberately NOT sequences here: a text box
    /// containing "abc" must not behave like a three-item list.
    /// </summary>
    public static bool TryAsSequence(object? value, out IReadOnlyList<object?> items)
    {
        if (value is string || value is null)
        {
            items = Array.Empty<object?>();
            return false;
        }

        if (value is IReadOnlyList<object?> readOnlyList)
        {
            items = readOnlyList;
            return true;
        }

        if (value is IEnumerable enumerable)
        {
            List<object?> buffer = new();
            foreach (object? item in enumerable)
            {
                buffer.Add(item);
            }

            items = buffer;
            return true;
        }

        items = Array.Empty<object?>();
        return false;
    }

    /// <summary>Coerces a value into a list, wrapping scalars into a single-item list.</summary>
    public static IReadOnlyList<object?> AsList(object? value)
    {
        if (value is null)
        {
            return Array.Empty<object?>();
        }

        return TryAsSequence(value, out IReadOnlyList<object?> items) ? items : new[] { value };
    }

    private static bool IsNumeric(object value)
        => value is double or float or decimal or int or long or short or byte or sbyte or uint or ulong or ushort
           || (value is string s && double.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands,
               CultureInfo.InvariantCulture, out _));

    /// <summary>
    /// Doubles that came from text, sliders and JSON should not disagree over the last bit,
    /// so numeric equality is tolerant rather than exact.
    /// </summary>
    private static bool NearlyEqual(double a, double b)
    {
        if (a.Equals(b))
        {
            return true;
        }

        if (double.IsNaN(a) || double.IsNaN(b) || double.IsInfinity(a) || double.IsInfinity(b))
        {
            return false;
        }

        double scale = Math.Max(Math.Abs(a), Math.Abs(b));
        return Math.Abs(a - b) <= 1e-9 * Math.Max(1d, scale);
    }
}
