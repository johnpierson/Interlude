using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Autodesk.DesignScript.Runtime;
using Interlude.Conditions;
using Interlude.Validation;

namespace Interlude.Model;

/// <summary>
/// One node of a form's element tree.
///
/// Elements are immutable records. Dynamo re-executes a graph from scratch on every change, so
/// the tree is rebuilt rather than reconciled, and there is no mutable state to get out of sync.
/// The <c>Behavior</c> nodes that appear to modify an element in fact return a new one via
/// <c>with</c>, which preserves the concrete element type without a visitor.
///
/// Adding a control means adding a sealed subclass and registering a renderer for it. No enum,
/// no switch in the renderer core.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(TextBoxElement), "textBox")]
[JsonDerivedType(typeof(PasswordElement), "password")]
[JsonDerivedType(typeof(NumericElement), "numeric")]
[JsonDerivedType(typeof(IntegerElement), "integer")]
[JsonDerivedType(typeof(SliderElement), "slider")]
[JsonDerivedType(typeof(DropdownElement), "dropdown")]
[JsonDerivedType(typeof(RadioGroupElement), "radioGroup")]
[JsonDerivedType(typeof(CheckBoxElement), "checkBox")]
[JsonDerivedType(typeof(ToggleElement), "toggle")]
[JsonDerivedType(typeof(ListSelectionElement), "listSelection")]
[JsonDerivedType(typeof(TreeSelectionElement), "treeSelection")]
[JsonDerivedType(typeof(DatePickerElement), "datePicker")]
[JsonDerivedType(typeof(ColorPickerElement), "colorPicker")]
[JsonDerivedType(typeof(FilePickerElement), "filePicker")]
[JsonDerivedType(typeof(FolderPickerElement), "folderPicker")]
[JsonDerivedType(typeof(LabelElement), "label")]
[JsonDerivedType(typeof(MarkdownElement), "markdown")]
[JsonDerivedType(typeof(ImageElement), "image")]
[JsonDerivedType(typeof(SeparatorElement), "separator")]
[JsonDerivedType(typeof(SpacerElement), "spacer")]
[JsonDerivedType(typeof(ProgressElement), "progress")]
[JsonDerivedType(typeof(ButtonElement), "button")]
[JsonDerivedType(typeof(VStackElement), "vStack")]
[JsonDerivedType(typeof(HStackElement), "hStack")]
[JsonDerivedType(typeof(GridElement), "grid")]
[JsonDerivedType(typeof(GroupBoxElement), "groupBox")]
[JsonDerivedType(typeof(TabsElement), "tabs")]
[JsonDerivedType(typeof(TabPageElement), "tabPage")]
[JsonDerivedType(typeof(ExpanderElement), "expander")]
[JsonDerivedType(typeof(CardElement), "card")]
[JsonDerivedType(typeof(ScrollElement), "scroll")]
[JsonDerivedType(typeof(DockElement), "dock")]
[JsonDerivedType(typeof(SplitViewElement), "splitView")]
public abstract record FormElement
{
    /// <summary>
    /// The name this element's value appears under in the results dictionary. Left empty, it is
    /// derived from <see cref="Label"/> by <see cref="FormKeys.Slugify"/> when the form is shown.
    /// </summary>
    public string Key { get; init; } = string.Empty;

    /// <summary>The caption shown beside or above the control.</summary>
    public string? Label { get; init; }

    /// <summary>Hover text.</summary>
    public string? Tooltip { get; init; }

    /// <summary>A line of guidance shown under the control.</summary>
    public string? HelpText { get; init; }

    /// <summary>When set and unsatisfied, the element is hidden and takes up no space.</summary>
    public ConditionExpr? VisibleIf { get; init; }

    /// <summary>When set and unsatisfied, the element is shown greyed out and cannot be edited.</summary>
    public ConditionExpr? EnabledIf { get; init; }

    /// <summary>When set and satisfied, the element must have a value before the form can be submitted.</summary>
    public ConditionExpr? RequiredIf { get; init; }

    /// <summary>Replaces the stock "This field is required." wording.</summary>
    public string? RequiredMessage { get; init; }

    /// <summary>Checks applied to this element's value while the user types.</summary>
    public IReadOnlyList<ValidationRule> Rules { get; init; } = Array.Empty<ValidationRule>();

    /// <summary>Presentation overrides. Null means "whatever the theme says".</summary>
    public ElementStyle? Style { get; init; }

    /// <summary>
    /// Whether this element contributes a value to the results dictionary. Display elements and
    /// containers do not, which is why a form's results contain only what the user actually answered.
    /// </summary>
    [JsonIgnore]
    public virtual bool ProducesValue => false;

    /// <summary>Every form key this element's behaviour reads.</summary>
    public IEnumerable<string> BehaviourDependencies()
    {
        if (VisibleIf is not null)
        {
            foreach (string key in VisibleIf.DependsOn())
            {
                yield return key;
            }
        }

        if (EnabledIf is not null)
        {
            foreach (string key in EnabledIf.DependsOn())
            {
                yield return key;
            }
        }

        if (RequiredIf is not null)
        {
            foreach (string key in RequiredIf.DependsOn())
            {
                yield return key;
            }
        }

        foreach (ValidationRule rule in Rules)
        {
            foreach (string key in rule.DependsOn())
            {
                yield return key;
            }
        }
    }
}

/// <summary>An element the user answers. Inputs are the only elements that produce values.</summary>
[IsVisibleInDynamoLibrary(false)]
public abstract record InputElement : FormElement
{
    /// <summary>
    /// The value the control starts with. Null means "use this control's natural empty value",
    /// which <see cref="GetFallbackValue"/> supplies.
    /// </summary>
    public object? DefaultValue { get; init; }

    /// <summary>
    /// When set, this input is driven by the form rather than by the user: its value is
    /// recomputed whenever anything it depends on changes.
    /// </summary>
    public ComputedValue? Computed { get; init; }

    /// <summary>A read-only input is shown normally but cannot be edited.</summary>
    public bool IsReadOnly { get; init; }

    /// <inheritdoc />
    [JsonIgnore]
    public override bool ProducesValue => true;

    /// <summary>
    /// The value this input reports when the user never touches it and no default was given.
    /// This is a method rather than a property so it never lands in the JSON schema.
    /// </summary>
    public abstract object? GetFallbackValue();

    /// <summary>
    /// Normalises a value arriving from a control, from JSON, or from a graph into the shape
    /// this input actually stores. Text boxes hand back strings, JSON hands back
    /// <c>JsonElement</c>, and a graph may hand back anything at all.
    /// </summary>
    public virtual object? Coerce(object? value) => value;

    /// <summary>The starting value: the author's default when given, otherwise the natural empty value.</summary>
    public object? GetEffectiveDefault()
        => Coerce(DefaultValue ?? GetFallbackValue());
}

/// <summary>An element that shows something but collects nothing.</summary>
[IsVisibleInDynamoLibrary(false)]
public abstract record DisplayElement : FormElement
{
}

/// <summary>An element that arranges other elements.</summary>
[IsVisibleInDynamoLibrary(false)]
public abstract record ContainerElement : FormElement
{
    public IReadOnlyList<FormElement> Children { get; init; } = Array.Empty<FormElement>();
}
