using System;
using Autodesk.DesignScript.Runtime;
using Interlude.Conditions;
using Interlude.Validation;

namespace Interlude;

/// <summary>
/// Checks applied to a field's answer, for use with <c>Behavior.WithValidation</c>.
///
/// Rules run as the user types and block submission while any of them fails. A rule on a field
/// the user cannot see is never applied — a hidden field can never stop a form being submitted,
/// which would otherwise mean an error with no control to fix it.
///
/// Except for <c>Rule.Required</c>, every rule passes on an empty field. Emptiness is
/// <c>Behavior.Required</c>'s business, so an optional field with a range on it stays optional.
/// </summary>
public class Rule
{
    private Rule()
    {
    }

    /// <summary>
    /// The field must have an answer.
    /// </summary>
    /// <param name="message">Wording shown when it does not.</param>
    /// <returns name="rule">The rule.</returns>
    /// <search>required,mandatory,not empty,must</search>
    public static ValidationRule Required(string message = "")
        => new RequiredRule { Message = NodeSupport.OrNull(message) };

    /// <summary>
    /// The field's number must fall between the bounds. Either bound can be left out.
    /// </summary>
    /// <param name="minimum">Lowest acceptable value.</param>
    /// <param name="maximum">Highest acceptable value.</param>
    /// <param name="message">Wording shown when it does not.</param>
    /// <returns name="rule">The rule.</returns>
    /// <search>range,between,minimum,maximum,bounds,limit</search>
    public static ValidationRule Range(
        [DefaultArgument("null")] object? minimum = null,
        [DefaultArgument("null")] object? maximum = null,
        string message = "")
        => new RangeRule
        {
            Minimum = NodeSupport.OptionalDouble(minimum),
            Maximum = NodeSupport.OptionalDouble(maximum),
            Message = NodeSupport.OrNull(message),
        };

    /// <summary>
    /// The field's text must match a regular expression.
    /// </summary>
    /// <param name="pattern">A .NET regular expression.</param>
    /// <param name="message">Wording shown when it does not match.</param>
    /// <param name="ignoreCase">Ignore letter case when matching.</param>
    /// <returns name="rule">The rule.</returns>
    /// <search>regex,pattern,format,matches,expression</search>
    public static ValidationRule Regex(string pattern, string message = "", bool ignoreCase = false)
        => new RegexRule
        {
            Pattern = pattern ?? string.Empty,
            IgnoreCase = ignoreCase,
            Message = NodeSupport.OrNull(message),
        };

    /// <summary>
    /// The field's text length, or the number of items selected, must fall between the bounds.
    /// </summary>
    /// <param name="minimum">Fewest acceptable characters or items.</param>
    /// <param name="maximum">Most acceptable characters or items.</param>
    /// <param name="message">Wording shown when it does not.</param>
    /// <returns name="rule">The rule.</returns>
    /// <search>length,characters,count,size,items</search>
    public static ValidationRule Length(
        [DefaultArgument("null")] object? minimum = null,
        [DefaultArgument("null")] object? maximum = null,
        string message = "")
        => new LengthRule
        {
            Minimum = NodeSupport.OptionalInt(minimum),
            Maximum = NodeSupport.OptionalInt(maximum),
            Message = NodeSupport.OrNull(message),
        };

    /// <summary>
    /// The path the field holds must exist on disk.
    /// </summary>
    /// <param name="message">Wording shown when it does not.</param>
    /// <returns name="rule">The rule.</returns>
    /// <search>file exists,path,disk,exists</search>
    public static ValidationRule FileExists(string message = "")
        => new FileExistsRule { Message = NodeSupport.OrNull(message) };

    /// <summary>
    /// The folder the field holds must exist on disk.
    /// </summary>
    /// <param name="message">Wording shown when it does not.</param>
    /// <returns name="rule">The rule.</returns>
    /// <search>folder exists,directory,path,disk,exists</search>
    public static ValidationRule FolderExists(string message = "")
        => new FileExistsRule { MustBeDirectory = true, Message = NodeSupport.OrNull(message) };

    /// <summary>
    /// The field's answer must compare correctly against another field, for rules such as
    /// "end date must be after start date".
    /// </summary>
    /// <param name="otherKey">The field to compare against.</param>
    /// <param name="operation">
    /// Equals, NotEquals, GreaterThan, GreaterThanOrEqual, LessThan or LessThanOrEqual.
    /// </param>
    /// <param name="message">Wording shown when the comparison fails.</param>
    /// <returns name="rule">The rule.</returns>
    /// <search>compare,other field,after,before,greater,cross field</search>
    public static ValidationRule CompareTo(string otherKey, string operation = "GreaterThan", string message = "")
        => new ComparisonRule
        {
            OtherKey = otherKey ?? string.Empty,
            Operator = Enum.TryParse(operation, ignoreCase: true, out ComparisonOperator parsed)
                ? parsed
                : ComparisonOperator.GreaterThan,
            Message = NodeSupport.OrNull(message),
        };
}
