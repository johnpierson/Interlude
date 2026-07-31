using System;
using System.Collections.Generic;
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

                string key = Template.Substring(index + 1, close - index - 1).Trim();
                builder.Append(ValueOps.ToStringInvariant(state.GetValue(key)));
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

            string key = template.Substring(index + 1, close - index - 1).Trim();
            if (key.Length > 0)
            {
                yield return key;
            }

            index = close + 1;
        }
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
