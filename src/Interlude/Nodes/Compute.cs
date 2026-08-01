using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.DesignScript.Runtime;
using Interlude.Conditions;

namespace Interlude;

/// <summary>
/// Values worked out from other answers, for use with <c>Behavior.WithComputed</c>.
///
/// A computed field is driven by the form rather than by the user: it recalculates whenever
/// anything it reads changes, in dependency order, so a total built on a subtotal is always
/// consistent. Computed values that depend on each other in a loop are rejected when the form is
/// built, before a window appears.
/// </summary>
public class Compute
{
    private Compute()
    {
    }

    /// <summary>
    /// Fills field values into a template: <c>"Hello {firstName} {lastName}"</c>. Write a literal
    /// brace by doubling it.
    /// </summary>
    /// <param name="template">The text, with field keys in braces.</param>
    /// <returns name="computation">The computation.</returns>
    /// <search>format,template,interpolate,text,concat,string</search>
    public static ComputedValue Format(string template)
        => new FormatComputed { Template = template ?? string.Empty };

    /// <summary>
    /// Adds up several fields. Anything that is not a number counts as zero, and a multi-select
    /// of numbers adds up its own items.
    /// </summary>
    /// <param name="keys">The fields to add.</param>
    /// <returns name="computation">The computation.</returns>
    /// <search>sum,total,add,plus</search>
    public static ComputedValue Sum(object keys)
        => new SumComputed
        {
            Keys = NodeSupport.Items(keys).Select(ValueOps.ToStringInvariant).ToList(),
        };

    /// <summary>
    /// Arithmetic on two values. Each side is either a field key or a nested computation.
    /// Dividing by zero gives zero rather than infinity, so a half-filled form shows a sensible
    /// total instead of a symbol.
    /// </summary>
    /// <param name="left">A field key, a literal, or a nested computation.</param>
    /// <param name="operation">Add, Subtract, Multiply, Divide, Modulo, Power, Min or Max.</param>
    /// <param name="right">A field key, a literal, or a nested computation.</param>
    /// <returns name="computation">The computation.</returns>
    /// <search>arithmetic,math,multiply,divide,subtract,calculate</search>
    public static ComputedValue Arithmetic(object left, string operation, object right)
        => new ArithmeticComputed
        {
            Operator = Enum.TryParse(operation, ignoreCase: true, out ArithmeticOperator parsed)
                ? parsed
                : ArithmeticOperator.Add,
            Left = Operand(left),
            Right = Operand(right),
        };

    /// <summary>
    /// The current value of another field, passed through unchanged.
    ///
    /// The counterpart to <c>Compute.Constant</c>: where that supplies a fixed number to a
    /// calculation, this supplies a live one. Together they are how the two sides of
    /// <c>Compute.Arithmetic</c> and the branches of <c>Compute.If</c> get filled in.
    ///
    /// On its own it mirrors one field into another, which is worth doing when the same answer
    /// needs to be visible in two places on a long form.
    /// </summary>
    /// <param name="key">The field to read.</param>
    /// <returns name="computation">The computation.</returns>
    /// <search>field,value,reference,copy,mirror</search>
    public static ComputedValue Field(string key)
        => new FieldComputed { Key = key ?? string.Empty };

    /// <summary>
    /// A fixed value that never changes.
    ///
    /// Not useful on its own — a computed field holding a constant is a label with extra steps.
    /// It exists to be fed into the nodes that take computations on both sides: the number to
    /// multiply by in <c>Compute.Arithmetic</c>, or either branch of <c>Compute.If</c>.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns name="computation">The computation.</returns>
    /// <search>constant,literal,fixed,value</search>
    public static ComputedValue Constant([DefaultArgument("null")] object? value = null)
        => new ConstantComputed { Value = value };

    /// <summary>
    /// Maps a field's answer through a lookup table. The keys are matched against the answer's
    /// text form.
    /// </summary>
    /// <param name="key">The field to read.</param>
    /// <param name="lookupKeys">The values to match.</param>
    /// <param name="lookupValues">What each match produces.</param>
    /// <param name="fallback">What to produce when nothing matches.</param>
    /// <returns name="computation">The computation.</returns>
    /// <search>lookup,map,translate,dictionary,switch</search>
    public static ComputedValue Lookup(
        string key,
        object lookupKeys,
        object lookupValues,
        [DefaultArgument("null")] object? fallback = null)
    {
        IReadOnlyList<object?> keys = NodeSupport.Items(lookupKeys);
        IReadOnlyList<object?> values = NodeSupport.Items(lookupValues);

        Dictionary<string, object?> map = new(StringComparer.Ordinal);
        for (int i = 0; i < keys.Count; i++)
        {
            // A key with no matching value maps to null rather than shifting the whole table.
            map[ValueOps.ToStringInvariant(keys[i])] = i < values.Count ? values[i] : null;
        }

        return new LookupComputed
        {
            Key = key ?? string.Empty,
            Map = map,
            Fallback = fallback,
        };
    }

    /// <summary>
    /// Chooses between two values based on a condition.
    ///
    /// How a computed field changes its mind: a rate that depends on which discipline was picked,
    /// a total that switches between metric and imperial. The condition is a Condition node, so it
    /// reads the form's own answers, and the whole thing recalculates whenever any field it
    /// touches changes.
    ///
    /// Both branches are computations, so either can be a nested <c>Compute.If</c> — which is how
    /// a three-way choice is written, if not especially readably. Past two levels, a lookup table
    /// with <c>Compute.Lookup</c> is easier to follow and easier to change.
    /// </summary>
    /// <param name="condition">Built with the Condition nodes.</param>
    /// <param name="ifTrue">A field key, a literal, or a nested computation.</param>
    /// <param name="ifFalse">A field key, a literal, or a nested computation.</param>
    /// <returns name="computation">The computation.</returns>
    /// <search>if,conditional,ternary,choose,when</search>
    public static ComputedValue If(
        ConditionExpr condition,
        [DefaultArgument("null")] object? ifTrue = null,
        [DefaultArgument("null")] object? ifFalse = null)
        => new ConditionalComputed
        {
            Condition = condition ?? ConstantCondition.True,
            IfTrue = Operand(ifTrue),
            IfFalse = Operand(ifFalse),
        };

    /// <summary>
    /// Reads an operand port.
    ///
    /// A bare string is treated as a field key rather than as literal text, because that is what
    /// it means nine times out of ten in this position. Use <c>Compute.Constant</c> when the
    /// literal text is genuinely what was wanted.
    /// </summary>
    private static ComputedValue Operand(object? value) => value switch
    {
        ComputedValue computed => computed,
        string key when key.Length > 0 => new FieldComputed { Key = key },
        _ => new ConstantComputed { Value = value },
    };
}
