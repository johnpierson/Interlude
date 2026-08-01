using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.DesignScript.Runtime;
using Interlude.Conditions;
using Interlude.Model;
using Interlude.Validation;

namespace Interlude;

/// <summary>
/// Adds behaviour to an element: when it is visible, when it is enabled, when it is required,
/// what makes it valid, and what its value is computed from.
///
/// Every node here returns a <em>new</em> element rather than changing the one it was given.
/// Elements are values, so the same element can be fed into two different behaviours without
/// one of them affecting the other, and re-running a graph rebuilds the tree from scratch with
/// nothing left over from last time.
/// </summary>
public class Behavior
{
    private Behavior()
    {
    }

    /// <summary>
    /// Shows the element only while the condition holds. A hidden element takes up no space, is
    /// never validated, and never blocks submission — but its value still appears in the results.
    /// </summary>
    /// <param name="element">The element to control.</param>
    /// <param name="condition">Built with the Condition nodes.</param>
    /// <returns name="element">A copy of the element with the condition attached.</returns>
    /// <search>visible,show,hide,conditional,visibleif</search>
    public static FormElement VisibleIf(FormElement element, ConditionExpr condition)
        => Require(element) with { VisibleIf = condition };

    /// <summary>
    /// Enables the element only while the condition holds. A disabled element is still visible,
    /// greyed out, and still contributes its value.
    /// </summary>
    /// <param name="element">The element to control.</param>
    /// <param name="condition">Built with the Condition nodes.</param>
    /// <returns name="element">A copy of the element with the condition attached.</returns>
    /// <search>enabled,disabled,greyed,conditional,enabledif</search>
    public static FormElement EnabledIf(FormElement element, ConditionExpr condition)
        => Require(element) with { EnabledIf = condition };

    /// <summary>
    /// Makes the element required only while the condition holds.
    ///
    /// For the field that matters in one case and not in another: a justification demanded only
    /// when an override was ticked, a folder demanded only when exporting was chosen.
    ///
    /// The asterisk beside the label appears and disappears with the condition, so the user is
    /// never asked for something the form has not yet told them it wants. And a field that is
    /// required but hidden is not enforced — hidden always wins, so the form can never be blocked
    /// by a control nobody can see.
    /// </summary>
    /// <param name="element">The element to control.</param>
    /// <param name="condition">Built with the Condition nodes.</param>
    /// <param name="message">Wording shown when the field is left empty.</param>
    /// <returns name="element">A copy of the element with the condition attached.</returns>
    /// <search>required,mandatory,conditional,requiredif</search>
    public static FormElement RequiredIf(FormElement element, ConditionExpr condition, string message = "")
        => Require(element) with
        {
            RequiredIf = condition,
            RequiredMessage = NodeSupport.OrNull(message),
        };

    /// <summary>
    /// Makes the element always required. The form cannot be submitted while it is empty.
    ///
    /// Adds the asterisk beside the label as well as enforcing it, which is why this is the node
    /// to use rather than attaching <c>Rule.Required</c> by hand: a requirement the user only
    /// discovers by pressing Submit is a worse form.
    ///
    /// Remember that false and zero are answers. A tick box is never empty, so requiring one does
    /// nothing — to insist a box is ticked, use <c>Rule.Required</c>'s sibling test through
    /// <c>Behavior.RequiredIf</c> with <c>Condition.IsNotChecked</c>, or simply validate it.
    /// </summary>
    /// <param name="element">The element to control.</param>
    /// <param name="message">Wording shown when the field is left empty.</param>
    /// <returns name="element">A copy of the element, marked required.</returns>
    /// <search>required,mandatory,must,asterisk</search>
    public static FormElement Required(FormElement element, string message = "")
        => Require(element) with
        {
            RequiredIf = ConstantCondition.True,
            RequiredMessage = NodeSupport.OrNull(message),
        };

    /// <summary>
    /// Adds a validation rule, built with the Rule nodes. Rules are checked as the user types,
    /// and the first one to fail is the message they see.
    /// </summary>
    /// <param name="element">The element to check.</param>
    /// <param name="rule">One rule, or a list of them.</param>
    /// <returns name="element">A copy of the element with the rules attached.</returns>
    /// <search>validation,rule,check,validate,constraint</search>
    public static FormElement WithValidation(FormElement element, List<object> rule)
    {
        FormElement target = Require(element);

        // A single rule arrives as a one-item list: the port is declared as a list so that
        // Dynamo hands the whole list over rather than calling this node once per rule, and
        // DesignScript promotes a lone value into a list on the way in.
        List<ValidationRule> rules = new(target.Rules);
        rules.AddRange(NodeSupport.Items(rule).OfType<ValidationRule>());

        return target with { Rules = rules };
    }

    /// <summary>
    /// Drives the element's value from other fields instead of from the user. The field becomes
    /// read-only and updates whenever anything it depends on changes.
    /// </summary>
    /// <param name="element">The element to drive.</param>
    /// <param name="computation">Built with the Compute nodes.</param>
    /// <returns name="element">A copy of the element with the computation attached.</returns>
    /// <search>computed,calculated,derived,formula,expression</search>
    public static FormElement WithComputed(FormElement element, ComputedValue computation)
    {
        FormElement target = Require(element);

        if (target is not InputElement input)
        {
            throw new ArgumentException(
                $"Only inputs can have a computed value, and {target.GetType().Name} is not one. " +
                "Use a text or number input to display a computed result.",
                nameof(element));
        }

        return input with { Computed = computation };
    }

    /// <summary>
    /// Sets the name this element's answer appears under in the results, overriding the one
    /// derived from its label. Worth doing for any graph you intend to keep.
    /// </summary>
    /// <param name="element">The element to name.</param>
    /// <param name="key">The result key.</param>
    /// <returns name="element">A copy of the element with the key set.</returns>
    /// <search>key,name,rename,identifier</search>
    public static FormElement WithKey(FormElement element, string key)
        => Require(element) with { Key = key ?? string.Empty };

    /// <summary>
    /// Adds hover text and a line of guidance under the element.
    ///
    /// The two are for different readers. <c>tooltip</c> is found only by someone who already
    /// suspects there is more to know; <c>helpText</c> sits under the field where everyone reads
    /// it. Put the thing users get wrong in the help text, and the detail in the tooltip.
    ///
    /// A line of help under the field it belongs to beats a paragraph of <c>Layout.Label</c> above
    /// the group, because the reader never has to work out which field it was about.
    /// </summary>
    /// <param name="element">The element to annotate.</param>
    /// <param name="tooltip">Hover text.</param>
    /// <param name="helpText">A line of guidance shown under the element.</param>
    /// <returns name="element">A copy of the element with the text attached.</returns>
    /// <search>tooltip,help,hint,description,guidance</search>
    public static FormElement WithHelp(FormElement element, string tooltip = "", string helpText = "")
        => Require(element) with
        {
            Tooltip = NodeSupport.OrNull(tooltip) ?? element?.Tooltip,
            HelpText = NodeSupport.OrNull(helpText) ?? element?.HelpText,
        };

    /// <summary>
    /// Overrides an element's size and spacing. Everything left null stays as the theme decided.
    ///
    /// A last resort, and deliberately so. Sizing is the theme's job — set it once with
    /// <c>Theme.Create</c> and every form in the office agrees — and a form pinned together with
    /// per-element overrides has to be re-tuned by hand whenever the theme, the density or the
    /// font changes underneath it.
    ///
    /// Where it is genuinely right: one field that has to be wider than the rest because of what
    /// goes in it, such as a full file path in a column sized for names.
    /// </summary>
    /// <param name="element">The element to size.</param>
    /// <param name="width">Fixed width in pixels.</param>
    /// <param name="height">Fixed height in pixels.</param>
    /// <param name="labelWidth">Width of this element's label column. Zero stacks the label above.</param>
    /// <param name="margin">Space around the element in pixels.</param>
    /// <returns name="element">A copy of the element with the sizing applied.</returns>
    /// <search>style,width,height,size,margin,spacing</search>
    public static FormElement WithSize(
        FormElement element,
        [DefaultArgument("null")] object? width = null,
        [DefaultArgument("null")] object? height = null,
        [DefaultArgument("null")] object? labelWidth = null,
        [DefaultArgument("null")] object? margin = null)
    {
        double? marginValue = NodeSupport.OptionalDouble(margin);

        return Restyle(element, style => style with
        {
            Width = NodeSupport.OptionalDouble(width) ?? style.Width,
            Height = NodeSupport.OptionalDouble(height) ?? style.Height,
            LabelWidth = NodeSupport.OptionalDouble(labelWidth) ?? style.LabelWidth,
            Margin = marginValue.HasValue ? Edges.Uniform(marginValue.Value) : style.Margin,
        });
    }

    /// <summary>
    /// Makes an input read-only. It stays visible and still contributes its value.
    ///
    /// For showing a figure the user should see but not change — a value read from the model, an
    /// identifier the graph generated.
    ///
    /// The difference from <c>Behavior.EnabledIf</c> with a false condition is what it says: a
    /// disabled field looks broken or not-yet-applicable, while a read-only one looks settled.
    /// Both still contribute their value to the answers.
    ///
    /// <c>Behavior.WithComputed</c> already makes a field read-only, so there is no need for both.
    /// </summary>
    /// <param name="element">The input to lock.</param>
    /// <param name="readOnly">Whether the field is locked.</param>
    /// <returns name="element">A copy of the element.</returns>
    /// <search>readonly,locked,disabled,display</search>
    public static FormElement ReadOnly(FormElement element, bool readOnly = true)
    {
        FormElement target = Require(element);

        return target is InputElement input
            ? input with { IsReadOnly = readOnly }
            : target;
    }

    /// <summary>Applies a change to an element's style, creating one if it had none.</summary>
    internal static FormElement Restyle(FormElement element, Func<ElementStyle, ElementStyle> change)
    {
        FormElement target = Require(element);
        return target with { Style = change(target.Style ?? ElementStyle.Empty) };
    }

    /// <summary>
    /// Rejects a null element with an explanation. Without this, an unwired port produces a
    /// NullReferenceException several nodes later, pointing at the wrong place entirely.
    /// </summary>
    private static FormElement Require(FormElement element)
        => element ?? throw new ArgumentNullException(
            nameof(element),
            "This node needs a form element. Check that the element port is connected.");
}
