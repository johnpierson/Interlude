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
    ///
    /// The exception to the family rule: every other rule passes on an empty field and this one is
    /// the reason they can, because emptiness is dealt with here rather than in each of them.
    ///
    /// <c>Behavior.Required</c> does the same job in one node and adds the asterisk beside the
    /// label, which is what a user actually reads. Reach for this one when the requirement needs
    /// wording of its own, or when it is going into a list of rules alongside others.
    /// </summary>
    /// <param name="message">Wording shown when it does not.</param>
    /// <returns name="rule">The rule.</returns>
    /// <search>required,mandatory,not empty,must</search>
    public static ValidationRule Required(string message = "")
        => new RequiredRule { Message = NodeSupport.OrNull(message) };

    /// <summary>
    /// The field's number must fall between the bounds. Either bound can be left out for a
    /// one-sided range.
    ///
    /// Both ends are inclusive: a range of 1 to 10 accepts 1 and accepts 10.
    ///
    /// Worth knowing when this is the right tool. <c>Input.Number</c>'s own minimum and maximum
    /// stop an out-of-range value being *typed*, which is a better experience where it applies.
    /// This is for the cases that cannot: a bound that depends on another field, or a number
    /// arriving in a text box.
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
    ///
    /// For codes with a shape: <c>"^[A-Z]{3}-[0-9]{4}$"</c> for ABC-1234. **Anchor it with
    /// <c>^</c> and <c>$</c>** unless you mean "contains" — an unanchored pattern matches anywhere
    /// in the text, so without them "ABC-1234-oops" passes.
    ///
    /// Always give a <c>message</c>. The pattern is not shown to the user, and "invalid" tells
    /// somebody staring at a text box nothing they can act on; "Use the form ABC-1234" tells them
    /// exactly what to type.
    ///
    /// An empty field passes, as with every rule but <c>Rule.Required</c>. Pair the two when the
    /// field is both mandatory and shaped.
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
    ///
    /// Catches the mistyped or moved path while the user is still looking at the form, rather than
    /// three nodes downstream when the graph fails to open it.
    ///
    /// It is checked from the machine running the graph, as the user running it — so a network
    /// path they cannot reach fails here even though it exists. That is the right answer: the
    /// graph could not have opened it either.
    ///
    /// Do not attach this to a <c>forSaving</c> file field. Naming a file that does not exist yet
    /// is the entire point of a save dialog.
    /// </summary>
    /// <param name="message">Wording shown when it does not.</param>
    /// <returns name="rule">The rule.</returns>
    /// <search>file exists,path,disk,exists</search>
    public static ValidationRule FileExists(string message = "")
        => new FileExistsRule { Message = NodeSupport.OrNull(message) };

    /// <summary>
    /// The folder the field holds must exist on disk.
    ///
    /// Worth attaching to any export destination, especially one that can be typed rather than
    /// browsed to: a folder that is not there is the most common reason an otherwise correct
    /// export run produces nothing.
    ///
    /// It checks, and does not create. Making the folder is the graph's job, and if that is what
    /// you intend then this rule is the wrong one.
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
