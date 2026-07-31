using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Autodesk.DesignScript.Runtime;
using Interlude.Conditions;

namespace Interlude.Validation;

/// <summary>The answer a rule gives about one value.</summary>
[IsVisibleInDynamoLibrary(false)]
public readonly record struct ValidationOutcome
{
    /// <summary>The value satisfied the rule.</summary>
    public static readonly ValidationOutcome Valid = new(true, null);

    private ValidationOutcome(bool isValid, string? message)
    {
        IsValid = isValid;
        Message = message;
    }

    public bool IsValid { get; }

    /// <summary>User-facing explanation, present only when <see cref="IsValid"/> is false.</summary>
    public string? Message { get; }

    /// <summary>The value broke the rule, for the stated reason.</summary>
    public static ValidationOutcome Invalid(string message) => new(false, message);
}

/// <summary>
/// One check applied to one field's value. Rules can read the rest of the form through
/// <see cref="IFormStateReader"/>, so "end date must be after start date" is expressible;
/// declaring those extra keys in <see cref="DependsOn"/> is what makes such a rule re-run
/// when the *other* field changes.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(RequiredRule), "required")]
[JsonDerivedType(typeof(RangeRule), "range")]
[JsonDerivedType(typeof(RegexRule), "regex")]
[JsonDerivedType(typeof(LengthRule), "length")]
[JsonDerivedType(typeof(FileExistsRule), "fileExists")]
[JsonDerivedType(typeof(ComparisonRule), "comparison")]
// Listed so a form containing one still serializes. Its predicate cannot cross JSON, so the
// rule comes back as one that always passes; see CustomPredicateRule.
[JsonDerivedType(typeof(CustomPredicateRule), "custom")]
public abstract record ValidationRule
{
    /// <summary>Overrides the rule's built-in wording.</summary>
    public string? Message { get; init; }

    /// <summary>
    /// Form keys this rule reads in addition to the value it is attached to.
    /// Returning the wrong set here does not produce a wrong answer, only a stale one.
    /// </summary>
    public virtual IEnumerable<string> DependsOn() => Array.Empty<string>();

    /// <summary>Checks <paramref name="value"/>, which is the current value of the owning field.</summary>
    public abstract ValidationOutcome Validate(object? value, IFormStateReader state);

    /// <summary>Builds the failure outcome, preferring an author-supplied <see cref="Message"/>.</summary>
    protected ValidationOutcome Fail(string defaultMessage)
        => ValidationOutcome.Invalid(string.IsNullOrWhiteSpace(Message) ? defaultMessage : Message!);
}
