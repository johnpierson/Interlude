using System.Collections.Generic;
using Autodesk.DesignScript.Runtime;
using Interlude.Conditions;

namespace Interlude;

/// <summary>
/// Tests over a form's own answers, for use with the Behavior nodes.
///
/// Conditions name the field they read by its key — the same key the answer appears under in the
/// results. They are re-evaluated whenever that field changes, so a form's behaviour is described
/// once, declaratively, rather than wired up event by event.
///
/// Comparisons are type-aware: numbers compare numerically even when typed as text, lists compare
/// element by element, and text comparison is case-sensitive unless <c>ignoreCase</c> says otherwise.
/// </summary>
public class Condition
{
    private Condition()
    {
    }

    /// <summary>
    /// True when the field's answer equals the value.
    /// </summary>
    /// <param name="key">The field to read.</param>
    /// <param name="value">What to compare against.</param>
    /// <param name="ignoreCase">Ignore letter case when comparing text.</param>
    /// <returns name="condition">The condition.</returns>
    /// <search>equals,is,same,matches,==</search>
    public static ConditionExpr Equals(string key, object value, bool ignoreCase = false)
        => Compare(key, ComparisonOperator.Equals, value, ignoreCase);

    /// <summary>
    /// True when the field's answer differs from the value.
    /// </summary>
    /// <param name="key">The field to read.</param>
    /// <param name="value">What to compare against.</param>
    /// <param name="ignoreCase">Ignore letter case when comparing text.</param>
    /// <returns name="condition">The condition.</returns>
    /// <search>not equals,different,isnot,!=</search>
    public static ConditionExpr NotEquals(string key, object value, bool ignoreCase = false)
        => Compare(key, ComparisonOperator.NotEquals, value, ignoreCase);

    /// <summary>
    /// True when the field's answer is greater than the value.
    /// </summary>
    /// <param name="key">The field to read.</param>
    /// <param name="value">What to compare against.</param>
    /// <returns name="condition">The condition.</returns>
    /// <search>greater,more,above,bigger,&gt;</search>
    public static ConditionExpr GreaterThan(string key, object value)
        => Compare(key, ComparisonOperator.GreaterThan, value, ignoreCase: false);

    /// <summary>
    /// True when the field's answer is greater than or equal to the value.
    /// </summary>
    /// <param name="key">The field to read.</param>
    /// <param name="value">What to compare against.</param>
    /// <returns name="condition">The condition.</returns>
    /// <search>at least,minimum,greater or equal,&gt;=</search>
    public static ConditionExpr AtLeast(string key, object value)
        => Compare(key, ComparisonOperator.GreaterThanOrEqual, value, ignoreCase: false);

    /// <summary>
    /// True when the field's answer is less than the value.
    /// </summary>
    /// <param name="key">The field to read.</param>
    /// <param name="value">What to compare against.</param>
    /// <returns name="condition">The condition.</returns>
    /// <search>less,under,below,smaller,&lt;</search>
    public static ConditionExpr LessThan(string key, object value)
        => Compare(key, ComparisonOperator.LessThan, value, ignoreCase: false);

    /// <summary>
    /// True when the field's answer is less than or equal to the value.
    /// </summary>
    /// <param name="key">The field to read.</param>
    /// <param name="value">What to compare against.</param>
    /// <returns name="condition">The condition.</returns>
    /// <search>at most,maximum,less or equal,&lt;=</search>
    public static ConditionExpr AtMost(string key, object value)
        => Compare(key, ComparisonOperator.LessThanOrEqual, value, ignoreCase: false);

    /// <summary>
    /// True when the field contains the value: as a substring for text, or as a member for a
    /// multi-select answer.
    /// </summary>
    /// <param name="key">The field to read.</param>
    /// <param name="value">What to look for.</param>
    /// <param name="ignoreCase">Ignore letter case when comparing text.</param>
    /// <returns name="condition">The condition.</returns>
    /// <search>contains,includes,has,substring</search>
    public static ConditionExpr Contains(string key, object value, bool ignoreCase = false)
        => Compare(key, ComparisonOperator.Contains, value, ignoreCase);

    /// <summary>
    /// True when the field's answer starts with the value.
    /// </summary>
    /// <param name="key">The field to read.</param>
    /// <param name="value">The prefix to look for.</param>
    /// <param name="ignoreCase">Ignore letter case when comparing text.</param>
    /// <returns name="condition">The condition.</returns>
    /// <search>starts with,prefix,begins</search>
    public static ConditionExpr StartsWith(string key, object value, bool ignoreCase = false)
        => Compare(key, ComparisonOperator.StartsWith, value, ignoreCase);

    /// <summary>
    /// True when the field's answer ends with the value.
    /// </summary>
    /// <param name="key">The field to read.</param>
    /// <param name="value">The suffix to look for.</param>
    /// <param name="ignoreCase">Ignore letter case when comparing text.</param>
    /// <returns name="condition">The condition.</returns>
    /// <search>ends with,suffix</search>
    public static ConditionExpr EndsWith(string key, object value, bool ignoreCase = false)
        => Compare(key, ComparisonOperator.EndsWith, value, ignoreCase);

    /// <summary>
    /// True when the field has no answer: blank text, or nothing selected. Note that false and
    /// zero are answers, not emptiness.
    /// </summary>
    /// <param name="key">The field to read.</param>
    /// <returns name="condition">The condition.</returns>
    /// <search>empty,blank,unanswered,null,nothing</search>
    public static ConditionExpr IsEmpty(string key)
        => Compare(key, ComparisonOperator.IsEmpty, null, ignoreCase: false);

    /// <summary>
    /// True when the field has an answer.
    ///
    /// The usual way to reveal the next step of a form once the previous one is filled in: show
    /// the options only after a file has been chosen.
    ///
    /// Note what counts. False and zero are answers, so an unticked box and a numeric field
    /// reading 0 are both "not empty". Only blank text and nothing-selected are empty.
    /// </summary>
    /// <param name="key">The field to read.</param>
    /// <returns name="condition">The condition.</returns>
    /// <search>not empty,answered,filled,has value</search>
    public static ConditionExpr IsNotEmpty(string key)
        => Compare(key, ComparisonOperator.IsNotEmpty, null, ignoreCase: false);

    /// <summary>
    /// True when a tick box or switch is on.
    /// </summary>
    /// <param name="key">The field to read.</param>
    /// <returns name="condition">The condition.</returns>
    /// <search>checked,ticked,on,true,enabled</search>
    public static ConditionExpr IsChecked(string key)
        => Compare(key, ComparisonOperator.IsChecked, null, ignoreCase: false);

    /// <summary>
    /// True when a tick box or switch is off.
    /// </summary>
    /// <param name="key">The field to read.</param>
    /// <returns name="condition">The condition.</returns>
    /// <search>unchecked,unticked,off,false</search>
    public static ConditionExpr IsNotChecked(string key)
        => Compare(key, ComparisonOperator.IsNotChecked, null, ignoreCase: false);

    /// <summary>
    /// True when the field's answer is one of the listed values.
    /// </summary>
    /// <param name="key">The field to read.</param>
    /// <param name="values">The values to accept.</param>
    /// <param name="ignoreCase">Ignore letter case when comparing text.</param>
    /// <returns name="condition">The condition.</returns>
    /// <search>in,one of,among,any of</search>
    public static ConditionExpr In(string key, object values, bool ignoreCase = false)
        => Compare(key, ComparisonOperator.In, NodeSupport.Items(values), ignoreCase);

    /// <summary>
    /// True when the field's answer matches a regular expression.
    ///
    /// For steering a form on the *shape* of an answer rather than its exact value — revealing the
    /// sheet-number options only once the prefix looks like a real prefix.
    ///
    /// Unanchored patterns match anywhere in the text; add <c>^</c> and <c>$</c> when the whole
    /// answer has to match. This is the Condition-side twin of <c>Rule.Regex</c>: use that one to
    /// stop a form being submitted, this one to change what the form shows.
    /// </summary>
    /// <param name="key">The field to read.</param>
    /// <param name="pattern">A .NET regular expression.</param>
    /// <param name="ignoreCase">Ignore letter case when matching.</param>
    /// <returns name="condition">The condition.</returns>
    /// <search>matches,regex,pattern,expression</search>
    public static ConditionExpr Matches(string key, string pattern, bool ignoreCase = false)
        => Compare(key, ComparisonOperator.Matches, pattern, ignoreCase);

    /// <summary>
    /// True when every one of the given conditions is true. An empty list is true.
    /// </summary>
    /// <param name="conditions">The conditions to combine.</param>
    /// <returns name="condition">The condition.</returns>
    /// <search>and,all,every,both</search>
    public static ConditionExpr And(object conditions)
        => new LogicalCondition
        {
            Operator = LogicalOperator.And,
            Terms = NodeSupport.Conditions(conditions),
        };

    /// <summary>
    /// True when any one of the given conditions is true. An empty list is false.
    /// </summary>
    /// <param name="conditions">The conditions to combine.</param>
    /// <returns name="condition">The condition.</returns>
    /// <search>or,any,either</search>
    public static ConditionExpr Or(object conditions)
        => new LogicalCondition
        {
            Operator = LogicalOperator.Or,
            Terms = NodeSupport.Conditions(conditions),
        };

    /// <summary>
    /// Inverts a condition: true where it was false, and false where it was true.
    ///
    /// Often avoidable — there is already <c>Condition.NotEquals</c>, <c>Condition.IsEmpty</c> and
    /// <c>Condition.IsNotChecked</c> — and the direct one reads better in a graph than a negated
    /// one. Where this earns its place is inverting something composite: not (A and B).
    /// </summary>
    /// <param name="condition">The condition to invert.</param>
    /// <returns name="condition">The condition.</returns>
    /// <search>not,invert,negate,opposite</search>
    public static ConditionExpr Not(ConditionExpr condition)
        => new LogicalCondition
        {
            Operator = LogicalOperator.Not,
            Terms = new[] { condition ?? ConstantCondition.False },
        };

    /// <summary>
    /// A condition with a fixed answer, useful for switching a behaviour off from the graph.
    /// </summary>
    /// <param name="value">The fixed answer.</param>
    /// <returns name="condition">The condition.</returns>
    /// <search>always,constant,true,false,fixed</search>
    public static ConditionExpr Always(bool value = true)
        => value ? ConstantCondition.True : ConstantCondition.False;

    private static ConditionExpr Compare(string key, ComparisonOperator op, object? operand, bool ignoreCase)
        => new ComparisonCondition
        {
            Key = key ?? string.Empty,
            Operator = op,
            Operand = operand,
            IgnoreCase = ignoreCase,
        };
}
