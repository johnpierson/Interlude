using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using Autodesk.DesignScript.Runtime;

namespace Interlude.Conditions;

/// <summary>
/// A value derived from other fields. An input carrying a computed value is driven by the form
/// rather than by the user, which is how "total = qty * price" is expressed without wiring.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(ConstantComputed), "constant")]
[JsonDerivedType(typeof(FieldComputed), "field")]
[JsonDerivedType(typeof(FormatComputed), "format")]
[JsonDerivedType(typeof(SumComputed), "sum")]
[JsonDerivedType(typeof(ArithmeticComputed), "arithmetic")]
[JsonDerivedType(typeof(LookupComputed), "lookup")]
[JsonDerivedType(typeof(ConditionalComputed), "conditional")]
public abstract record ComputedValue
{
    /// <summary>Every form key this expression reads, directly or through nested expressions.</summary>
    public abstract IEnumerable<string> DependsOn();

    /// <summary>Evaluates the expression against a snapshot of form state.</summary>
    public abstract object? Compute(IFormStateReader state);
}

/// <summary>A literal.</summary>
[IsVisibleInDynamoLibrary(false)]
public sealed record ConstantComputed : ComputedValue
{
    public object? Value { get; init; }

    public override IEnumerable<string> DependsOn() => Array.Empty<string>();

    public override object? Compute(IFormStateReader state) => Value;
}

/// <summary>The current value of another field, passed through unchanged.</summary>
[IsVisibleInDynamoLibrary(false)]
public sealed record FieldComputed : ComputedValue
{
    public string Key { get; init; } = string.Empty;

    public override IEnumerable<string> DependsOn()
    {
        if (!string.IsNullOrEmpty(Key))
        {
            yield return Key;
        }
    }

    public override object? Compute(IFormStateReader state) => state.GetValue(Key);
}

/// <summary>
/// String interpolation over field values: <c>"Hello {firstName} {lastName}"</c>.
/// Literal braces are written doubled, as in <see cref="string.Format(string, object[])"/>.
///
/// A placeholder may carry a .NET format specifier after a colon — <c>{total:0.00}</c>,
/// <c>{when:d}</c>, <c>{when:HH:mm}</c> — which is why the split is at the <em>first</em> colon:
/// everything after it belongs to the specifier, colons and all. A key containing a colon
/// therefore cannot be written in a template, which only reaches keys set by hand, since
/// <see cref="Model.FormKeys.Slugify"/> cannot produce one.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
public sealed record FormatComputed : ComputedValue
{
    public string Template { get; init; } = string.Empty;

    public override IEnumerable<string> DependsOn() => ParsePlaceholders(Template);

    public override object? Compute(IFormStateReader state)
    {
        StringBuilder builder = new(Template.Length + 16);
        int index = 0;

        while (index < Template.Length)
        {
            char current = Template[index];

            if (current == '{')
            {
                if (index + 1 < Template.Length && Template[index + 1] == '{')
                {
                    builder.Append('{');
                    index += 2;
                    continue;
                }

                int close = Template.IndexOf('}', index + 1);
                if (close < 0)
                {
                    // Unterminated placeholder: emit the rest verbatim rather than throwing
                    // while the user is still halfway through typing a template.
                    builder.Append(Template, index, Template.Length - index);
                    break;
                }

                (string key, string? spec) = SplitPlaceholder(
                    Template.Substring(index + 1, close - index - 1));
                builder.Append(Render(state.GetValue(key), spec));
                index = close + 1;
                continue;
            }

            if (current == '}' && index + 1 < Template.Length && Template[index + 1] == '}')
            {
                builder.Append('}');
                index += 2;
                continue;
            }

            builder.Append(current);
            index++;
        }

        return builder.ToString();
    }

    private static IEnumerable<string> ParsePlaceholders(string template)
    {
        int index = 0;
        while (index < template.Length)
        {
            if (template[index] != '{')
            {
                index++;
                continue;
            }

            if (index + 1 < template.Length && template[index + 1] == '{')
            {
                index += 2;
                continue;
            }

            int close = template.IndexOf('}', index + 1);
            if (close < 0)
            {
                yield break;
            }

            (string key, _) = SplitPlaceholder(template.Substring(index + 1, close - index - 1));
            if (key.Length > 0)
            {
                yield return key;
            }

            index = close + 1;
        }
    }

    /// <summary>Separates <c>total:0.00</c> into the key and its format specifier.</summary>
    private static (string Key, string? Spec) SplitPlaceholder(string token)
    {
        int colon = token.IndexOf(':');
        if (colon < 0)
        {
            return (token.Trim(), null);
        }

        string spec = token.Substring(colon + 1).Trim();
        return (token.Substring(0, colon).Trim(), spec.Length > 0 ? spec : null);
    }

    /// <summary>
    /// Renders one placeholder.
    ///
    /// A specifier the runtime cannot use falls back to the plain display form rather than
    /// throwing, on the same grounds as the unterminated placeholder above: the author may be
    /// halfway through typing it, and a template is re-rendered on every keystroke.
    /// </summary>
    private static string Render(object? value, string? spec)
    {
        if (spec is null)
        {
            return ValueOps.ToDisplayString(value);
        }

        // A multi-select formats item by item, so "{prices:0.00}" reads as a list of prices
        // rather than falling back to the unformatted join.
        if (ValueOps.TryAsSequence(value, out IReadOnlyList<object?> items))
        {
            return string.Join(", ", items.Select(item => Render(item, spec)));
        }

        // Form values are loosely typed: a number that came from JSON or a text box may still be
        // a string, and "{total:0.00}" should not silently do nothing depending on where the
        // value came from.
        object? target = value is string text && ValueOps.TryToDouble(text, out double number)
            ? number
            : value;

        if (target is IFormattable formattable)
        {
            try
            {
                return formattable.ToString(spec, CultureInfo.InvariantCulture);
            }
            catch (FormatException)
            {
                return ValueOps.ToDisplayString(value);
            }
        }

        return ValueOps.ToDisplayString(value);
    }
}

/// <summary>Numeric total of several fields. Non-numeric values contribute zero.</summary>
[IsVisibleInDynamoLibrary(false)]
public sealed record SumComputed : ComputedValue
{
    public IReadOnlyList<string> Keys { get; init; } = Array.Empty<string>();

    public override IEnumerable<string> DependsOn() => Keys;

    public override object? Compute(IFormStateReader state)
    {
        double total = 0d;

        foreach (string key in Keys)
        {
            object? value = state.GetValue(key);

            // A multi-select of numbers sums its items, which is what "sum these fields" means
            // when one of them happens to be a list box.
            if (ValueOps.TryAsSequence(value, out IReadOnlyList<object?> items))
            {
                total += items.Sum(item => ValueOps.ToDouble(item));
                continue;
            }

            total += ValueOps.ToDouble(value);
        }

        return total;
    }
}

/// <summary>Supported binary arithmetic.</summary>
[IsVisibleInDynamoLibrary(false)]
public enum ArithmeticOperator
{
    Add,
    Subtract,
    Multiply,
    Divide,
    Modulo,
    Power,
    Min,
    Max,
}

/// <summary>Arithmetic over two nested expressions.</summary>
[IsVisibleInDynamoLibrary(false)]
public sealed record ArithmeticComputed : ComputedValue
{
    public ArithmeticOperator Operator { get; init; } = ArithmeticOperator.Add;

    public ComputedValue Left { get; init; } = new ConstantComputed();

    public ComputedValue Right { get; init; } = new ConstantComputed();

    public override IEnumerable<string> DependsOn()
        => Left.DependsOn().Concat(Right.DependsOn());

    public override object? Compute(IFormStateReader state)
    {
        double left = ValueOps.ToDouble(Left.Compute(state));
        double right = ValueOps.ToDouble(Right.Compute(state));

        switch (Operator)
        {
            case ArithmeticOperator.Add:
                return left + right;
            case ArithmeticOperator.Subtract:
                return left - right;
            case ArithmeticOperator.Multiply:
                return left * right;
            case ArithmeticOperator.Divide:
                // Division by zero yields 0 rather than Infinity: a half-filled form should show
                // a blank-ish total, not "8" in a text box.
                return right == 0d ? 0d : left / right;
            case ArithmeticOperator.Modulo:
                return right == 0d ? 0d : left % right;
            case ArithmeticOperator.Power:
                return Math.Pow(left, right);
            case ArithmeticOperator.Min:
                return Math.Min(left, right);
            case ArithmeticOperator.Max:
                return Math.Max(left, right);
            default:
                return 0d;
        }
    }
}

/// <summary>Maps one field's value through a lookup table.</summary>
[IsVisibleInDynamoLibrary(false)]
public sealed record LookupComputed : ComputedValue
{
    public string Key { get; init; } = string.Empty;

    /// <summary>Keys are matched against the field's invariant string form.</summary>
    public IReadOnlyDictionary<string, object?> Map { get; init; }
        = new Dictionary<string, object?>(StringComparer.Ordinal);

    /// <summary>Returned when the field's value is not in the map.</summary>
    public object? Fallback { get; init; }

    public override IEnumerable<string> DependsOn()
    {
        if (!string.IsNullOrEmpty(Key))
        {
            yield return Key;
        }
    }

    public override object? Compute(IFormStateReader state)
    {
        string lookup = ValueOps.ToStringInvariant(state.GetValue(Key));
        return Map.TryGetValue(lookup, out object? mapped) ? mapped : Fallback;
    }
}

/// <summary>Chooses between two expressions based on a condition.</summary>
[IsVisibleInDynamoLibrary(false)]
public sealed record ConditionalComputed : ComputedValue
{
    public ConditionExpr Condition { get; init; } = ConstantCondition.True;

    public ComputedValue IfTrue { get; init; } = new ConstantComputed();

    public ComputedValue IfFalse { get; init; } = new ConstantComputed();

    public override IEnumerable<string> DependsOn()
        => Condition.DependsOn().Concat(IfTrue.DependsOn()).Concat(IfFalse.DependsOn());

    public override object? Compute(IFormStateReader state)
        => Condition.Evaluate(state) ? IfTrue.Compute(state) : IfFalse.Compute(state);
}
