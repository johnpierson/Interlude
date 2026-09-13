using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Autodesk.DesignScript.Runtime;
using Interlude.Conditions;

namespace Interlude.Validation;

/// <summary>The field must carry a value.</summary>
[IsVisibleInDynamoLibrary(false)]
public sealed record RequiredRule : ValidationRule
{
    public override ValidationOutcome Validate(object? value, IFormStateReader state)
        => ValueOps.IsEmpty(value) ? Fail("This field is required.") : ValidationOutcome.Valid;
}

/// <summary>The field's number must fall within bounds. Either bound may be omitted.</summary>
[IsVisibleInDynamoLibrary(false)]
public sealed record RangeRule : ValidationRule
{
    public double? Minimum { get; init; }

    public double? Maximum { get; init; }

    public override ValidationOutcome Validate(object? value, IFormStateReader state)
    {
        // Emptiness is RequiredRule's business. A range rule on a blank optional field passes.
        if (ValueOps.IsEmpty(value))
        {
            return ValidationOutcome.Valid;
        }

        if (!ValueOps.TryToDouble(value, out double number) ||
            double.IsNaN(number) || double.IsInfinity(number))
        {
            return Fail("Enter a number.");
        }

        if (Minimum.HasValue && number < Minimum.Value)
        {
            return Fail(Maximum.HasValue
                ? string.Format(CultureInfo.CurrentCulture, "Enter a value between {0} and {1}.", Minimum.Value, Maximum.Value)
                : string.Format(CultureInfo.CurrentCulture, "Enter a value of at least {0}.", Minimum.Value));
        }

        if (Maximum.HasValue && number > Maximum.Value)
        {
            return Fail(Minimum.HasValue
                ? string.Format(CultureInfo.CurrentCulture, "Enter a value between {0} and {1}.", Minimum.Value, Maximum.Value)
                : string.Format(CultureInfo.CurrentCulture, "Enter a value of at most {0}.", Maximum.Value));
        }

        return ValidationOutcome.Valid;
    }
}

/// <summary>The field's text must match a regular expression.</summary>
[IsVisibleInDynamoLibrary(false)]
public sealed record RegexRule : ValidationRule
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

    public string Pattern { get; init; } = string.Empty;

    public bool IgnoreCase { get; init; }

    public override ValidationOutcome Validate(object? value, IFormStateReader state)
    {
        if (ValueOps.IsEmpty(value) || Pattern.Length == 0)
        {
            return ValidationOutcome.Valid;
        }

        RegexOptions options = RegexOptions.CultureInvariant;
        if (IgnoreCase)
        {
            options |= RegexOptions.IgnoreCase;
        }

        try
        {
            return Regex.IsMatch(ValueOps.ToStringInvariant(value), Pattern, options, RegexTimeout)
                ? ValidationOutcome.Valid
                : Fail("This value is not in the expected format.");
        }
        catch (ArgumentException)
        {
            // A broken pattern is the form author's bug. Say so rather than blocking the user
            // behind a rule that can never pass.
            return Fail("This field has an invalid validation pattern.");
        }
        catch (RegexMatchTimeoutException)
        {
            return Fail("This value could not be checked in time.");
        }
    }
}

/// <summary>The field's text length (or selection count) must fall within bounds.</summary>
[IsVisibleInDynamoLibrary(false)]
public sealed record LengthRule : ValidationRule
{
    public int? Minimum { get; init; }

    public int? Maximum { get; init; }

    public override ValidationOutcome Validate(object? value, IFormStateReader state)
    {
        int length = ValueOps.TryAsSequence(value, out IReadOnlyList<object?> items)
            ? items.Count
            : ValueOps.ToStringInvariant(value).Length;

        if (length == 0 && !Minimum.HasValue)
        {
            return ValidationOutcome.Valid;
        }

        if (Minimum.HasValue && length < Minimum.Value)
        {
            return Fail(string.Format(CultureInfo.CurrentCulture,
                "Enter at least {0} character(s).", Minimum.Value));
        }

        if (Maximum.HasValue && length > Maximum.Value)
        {
            return Fail(string.Format(CultureInfo.CurrentCulture,
                "Enter at most {0} character(s).", Maximum.Value));
        }

        return ValidationOutcome.Valid;
    }
}

/// <summary>The field's path must exist on disk.</summary>
[IsVisibleInDynamoLibrary(false)]
public sealed record FileExistsRule : ValidationRule
{
    /// <summary>Checks for a directory instead of a file.</summary>
    public bool MustBeDirectory { get; init; }

    public override ValidationOutcome Validate(object? value, IFormStateReader state)
    {
        if (ValueOps.IsEmpty(value))
        {
            return ValidationOutcome.Valid;
        }

        foreach (object? item in ValueOps.AsList(value))
        {
            string path = ValueOps.ToStringInvariant(item);
            if (path.Length == 0)
            {
                continue;
            }

            bool exists;
            try
            {
                exists = MustBeDirectory ? Directory.Exists(path) : File.Exists(path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                // An unreachable UNC share or a malformed path reads as "not there" rather
                // than throwing out of a keystroke handler.
                exists = false;
            }

            if (!exists)
            {
                return Fail(MustBeDirectory
                    ? "This folder does not exist."
                    : "This file does not exist.");
            }
        }

        return ValidationOutcome.Valid;
    }
}

/// <summary>
/// Compares this field against another field, for rules like "end date must be after start date".
/// </summary>
[IsVisibleInDynamoLibrary(false)]
public sealed record ComparisonRule : ValidationRule
{
    /// <summary>The other field to compare against.</summary>
    public string OtherKey { get; init; } = string.Empty;

    public ComparisonOperator Operator { get; init; } = ComparisonOperator.NotEquals;

    public bool IgnoreCase { get; init; }

    public override IEnumerable<string> DependsOn()
    {
        if (!string.IsNullOrEmpty(OtherKey))
        {
            yield return OtherKey;
        }
    }

    public override ValidationOutcome Validate(object? value, IFormStateReader state)
    {
        if (ValueOps.IsEmpty(value))
        {
            return ValidationOutcome.Valid;
        }

        object? other = state.GetValue(OtherKey);

        // Reuse the condition engine so validation and VisibleIf can never disagree about
        // what "greater than" means.
        ComparisonCondition condition = new()
        {
            Key = "$value",
            Operator = Operator,
            Operand = other,
            IgnoreCase = IgnoreCase,
        };

        bool satisfied = condition.Evaluate(new SingleValueReader(value, state));
        return satisfied ? ValidationOutcome.Valid : Fail($"This value must be {Describe()} '{OtherKey}'.");
    }

    private string Describe() => Operator switch
    {
        ComparisonOperator.Equals => "equal to",
        ComparisonOperator.NotEquals => "different from",
        ComparisonOperator.GreaterThan => "greater than",
        ComparisonOperator.GreaterThanOrEqual => "greater than or equal to",
        ComparisonOperator.LessThan => "less than",
        ComparisonOperator.LessThanOrEqual => "less than or equal to",
        _ => "comparable to",
    };

    /// <summary>Presents the value under test as the reserved key <c>$value</c>.</summary>
    private sealed class SingleValueReader : IFormStateReader
    {
        private readonly object? _value;
        private readonly IFormStateReader _inner;

        internal SingleValueReader(object? value, IFormStateReader inner)
        {
            _value = value;
            _inner = inner;
        }

        public IReadOnlyCollection<string> Keys => _inner.Keys;

        public object? GetValue(string key)
            => string.Equals(key, "$value", StringComparison.Ordinal) ? _value : _inner.GetValue(key);

        public bool TryGetValue(string key, out object? value)
        {
            if (string.Equals(key, "$value", StringComparison.Ordinal))
            {
                value = _value;
                return true;
            }

            return _inner.TryGetValue(key, out value);
        }
    }
}

/// <summary>
/// An arbitrary predicate supplied in code. Not serializable: <see cref="Serialization.FormJson"/>
/// rejects forms carrying one so executable code cannot be lost at a JSON boundary.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
public sealed record CustomPredicateRule : ValidationRule
{
    /// <summary>Returns true when the value is acceptable.</summary>
    [JsonIgnore]
    public Func<object?, IFormStateReader, bool>? Predicate { get; init; }

    /// <summary>Extra form keys the predicate reads.</summary>
    [JsonIgnore]
    public IReadOnlyList<string> ExtraKeys { get; init; } = Array.Empty<string>();

    public override IEnumerable<string> DependsOn() => ExtraKeys;

    public override ValidationOutcome Validate(object? value, IFormStateReader state)
    {
        if (Predicate is null)
        {
            return ValidationOutcome.Valid;
        }

        try
        {
            return Predicate(value, state) ? ValidationOutcome.Valid : Fail("This value is not valid.");
        }
        catch (Exception ex)
        {
            // Author code runs on the UI thread during typing; a throw must degrade to a
            // validation message, never take the dialog down.
            return Fail($"Validation failed: {ex.Message}");
        }
    }
}
