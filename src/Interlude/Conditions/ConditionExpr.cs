using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text.Json.Serialization;
using Autodesk.DesignScript.Runtime;

namespace Interlude.Conditions;

/// <summary>
/// A condition over form state, expressed as an object tree rather than a string mini-language.
///
/// The object form is deliberate: it maps one-to-one onto Dynamo node composition, it serializes
/// without a parser, and <see cref="DependsOn"/> makes dependency extraction a plain tree walk
/// instead of a static analysis problem. A string expression language that compiles down to
/// exactly these types is a clean future addition, not a prerequisite.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(ComparisonCondition), "comparison")]
[JsonDerivedType(typeof(LogicalCondition), "logical")]
[JsonDerivedType(typeof(ConstantCondition), "constant")]
public abstract record ConditionExpr
{
    /// <summary>Every form key this condition reads, directly or through nested terms.</summary>
    public abstract IEnumerable<string> DependsOn();

    /// <summary>Evaluates the condition against a snapshot of form state.</summary>
    public abstract bool Evaluate(IFormStateReader state);
}

/// <summary>A condition with a fixed answer. <see cref="True"/> is the "no condition" case.</summary>
[IsVisibleInDynamoLibrary(false)]
public sealed record ConstantCondition : ConditionExpr
{
    /// <summary>Always satisfied.</summary>
    public static readonly ConstantCondition True = new() { Value = true };

    /// <summary>Never satisfied.</summary>
    public static readonly ConstantCondition False = new() { Value = false };

    public bool Value { get; init; } = true;

    public override IEnumerable<string> DependsOn() => Array.Empty<string>();

    public override bool Evaluate(IFormStateReader state) => Value;
}

/// <summary>How a field's value is measured against an operand.</summary>
[IsVisibleInDynamoLibrary(false)]
public enum ComparisonOperator
{
    Equals,
    NotEquals,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,

    /// <summary>Substring for text; membership for multi-select values.</summary>
    Contains,
    NotContains,
    StartsWith,
    EndsWith,

    /// <summary>Null, blank text, or an empty selection.</summary>
    IsEmpty,
    IsNotEmpty,

    /// <summary>Truthiness, for checkboxes and toggles.</summary>
    IsChecked,
    IsNotChecked,

    /// <summary>The field's value appears in the operand list.</summary>
    In,
    NotIn,

    /// <summary>The field's text matches a regular expression.</summary>
    Matches,
}

/// <summary>Compares one field's value against a fixed operand.</summary>
[IsVisibleInDynamoLibrary(false)]
public sealed record ComparisonCondition : ConditionExpr
{
    /// <summary>Regexes come from graph authors, so a runaway pattern must not wedge the UI thread.</summary>
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

    /// <summary>The form key being tested.</summary>
    public string Key { get; init; } = string.Empty;

    public ComparisonOperator Operator { get; init; } = ComparisonOperator.Equals;

    /// <summary>The value compared against. Unused by the unary operators.</summary>
    public object? Operand { get; init; }

    /// <summary>Applies to text comparisons only; numeric and boolean comparisons ignore it.</summary>
    public bool IgnoreCase { get; init; }

    public override IEnumerable<string> DependsOn()
    {
        if (!string.IsNullOrEmpty(Key))
        {
            yield return Key;
        }
    }

    public override bool Evaluate(IFormStateReader state)
    {
        object? value = state.GetValue(Key);

        switch (Operator)
        {
            case ComparisonOperator.Equals:
                return ValueOps.AreEqual(value, Operand, IgnoreCase);
            case ComparisonOperator.NotEquals:
                return !ValueOps.AreEqual(value, Operand, IgnoreCase);

            case ComparisonOperator.GreaterThan:
                return ValueOps.TryCompare(value, Operand, out int gt) && gt > 0;
            case ComparisonOperator.GreaterThanOrEqual:
                return ValueOps.TryCompare(value, Operand, out int ge) && ge >= 0;
            case ComparisonOperator.LessThan:
                return ValueOps.TryCompare(value, Operand, out int lt) && lt < 0;
            case ComparisonOperator.LessThanOrEqual:
                return ValueOps.TryCompare(value, Operand, out int le) && le <= 0;

            case ComparisonOperator.Contains:
                return EvaluateContains(value);
            case ComparisonOperator.NotContains:
                return !EvaluateContains(value);

            case ComparisonOperator.StartsWith:
                return ValueOps.ToStringInvariant(value)
                    .StartsWith(ValueOps.ToStringInvariant(Operand), Comparison);
            case ComparisonOperator.EndsWith:
                return ValueOps.ToStringInvariant(value)
                    .EndsWith(ValueOps.ToStringInvariant(Operand), Comparison);

            case ComparisonOperator.IsEmpty:
                return ValueOps.IsEmpty(value);
            case ComparisonOperator.IsNotEmpty:
                return !ValueOps.IsEmpty(value);

            case ComparisonOperator.IsChecked:
                return ValueOps.ToBool(value);
            case ComparisonOperator.IsNotChecked:
                return !ValueOps.ToBool(value);

            case ComparisonOperator.In:
                return EvaluateIn(value);
            case ComparisonOperator.NotIn:
                return !EvaluateIn(value);

            case ComparisonOperator.Matches:
                return EvaluateMatches(value);

            default:
                return false;
        }
    }

    private StringComparison Comparison
        => IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    /// <summary>
    /// "Contains" means membership for a multi-select and substring for text, because that is
    /// what a graph author means in each case.
    /// </summary>
    private bool EvaluateContains(object? value)
    {
        if (ValueOps.TryAsSequence(value, out IReadOnlyList<object?> items))
        {
            return items.Any(item => ValueOps.AreEqual(item, Operand, IgnoreCase));
        }

        return ValueOps.ToStringInvariant(value)
            .IndexOf(ValueOps.ToStringInvariant(Operand), Comparison) >= 0;
    }

    private bool EvaluateIn(object? value)
    {
        IReadOnlyList<object?> candidates = ValueOps.AsList(Operand);

        // A multi-select is "in" the candidate set when every selected item is a candidate.
        if (ValueOps.TryAsSequence(value, out IReadOnlyList<object?> selected))
        {
            return selected.Count > 0 &&
                   selected.All(item => candidates.Any(c => ValueOps.AreEqual(item, c, IgnoreCase)));
        }

        return candidates.Any(c => ValueOps.AreEqual(value, c, IgnoreCase));
    }

    private bool EvaluateMatches(object? value)
    {
        string pattern = ValueOps.ToStringInvariant(Operand);
        if (pattern.Length == 0)
        {
            return false;
        }

        RegexOptions options = RegexOptions.CultureInvariant;
        if (IgnoreCase)
        {
            options |= RegexOptions.IgnoreCase;
        }

        try
        {
            return Regex.IsMatch(ValueOps.ToStringInvariant(value), pattern, options, RegexTimeout);
        }
        catch (ArgumentException)
        {
            // An invalid pattern is an authoring mistake, not a reason to tear down the dialog.
            return false;
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }
}

/// <summary>Boolean combination of nested conditions.</summary>
[IsVisibleInDynamoLibrary(false)]
public enum LogicalOperator
{
    And,
    Or,

    /// <summary>Negates the first term; further terms are ignored.</summary>
    Not,
}

/// <summary>Combines nested conditions with And / Or / Not.</summary>
[IsVisibleInDynamoLibrary(false)]
public sealed record LogicalCondition : ConditionExpr
{
    public LogicalOperator Operator { get; init; } = LogicalOperator.And;

    public IReadOnlyList<ConditionExpr> Terms { get; init; } = Array.Empty<ConditionExpr>();

    public override IEnumerable<string> DependsOn()
        => Terms.SelectMany(term => term.DependsOn());

    public override bool Evaluate(IFormStateReader state)
    {
        switch (Operator)
        {
            case LogicalOperator.And:
                // An empty And is vacuously true, which keeps "no conditions" and
                // "an And of nothing" behaving identically.
                return Terms.All(term => term.Evaluate(state));
            case LogicalOperator.Or:
                return Terms.Any(term => term.Evaluate(state));
            case LogicalOperator.Not:
                return Terms.Count > 0 && !Terms[0].Evaluate(state);
            default:
                return false;
        }
    }
}
