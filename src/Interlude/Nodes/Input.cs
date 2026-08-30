using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.DesignScript.Runtime;
using Interlude.Conditions;
using Interlude.Model;

namespace Interlude;

/// <summary>
/// The fields a user answers.
///
/// Every input returns an element describing the control, not the control itself, and every one
/// takes the same three trailing options: <c>key</c>, which names the answer in the results
/// dictionary; <c>tooltip</c>; and <c>helpText</c>. Leave <c>key</c> empty and it is derived from
/// the label — convenient for a quick form, but give real keys to any graph you intend to keep,
/// because renaming a label would otherwise rename the answer.
///
/// Choice inputs take the values themselves, not their display names. Selecting an option hands
/// back the original object — a Revit element, a family type, whatever was put in — so the answer
/// is usable directly instead of needing a lookup back from a string.
/// </summary>
public class Input
{
    private Input()
    {
    }

    /// <summary>
    /// A single-line text field. The workhorse: names, prefixes, codes, anything typed.
    ///
    /// The answer is always a string and never null — an untouched field returns its default, and
    /// a field the user cleared returns an empty string. Read it with <c>Result.GetString</c>.
    ///
    /// Use <c>Input.TextArea</c> when the answer runs to more than a line, and attach
    /// <c>Rule.Regex</c> or <c>Rule.Length</c> with <c>Behavior.WithValidation</c> when the text
    /// has to take a particular shape. Checking the format after the form closes is too late to
    /// tell the user anything useful.
    /// </summary>
    /// <param name="label">Caption shown beside the field.</param>
    /// <param name="defaultValue">Value the field starts with.</param>
    /// <param name="placeholder">Grey prompt shown while the field is empty.</param>
    /// <param name="key">Name of this answer in the results. Derived from the label when empty.</param>
    /// <param name="tooltip">Hover text.</param>
    /// <param name="helpText">A line of guidance shown under the field.</param>
    /// <returns name="element">The form element.</returns>
    /// <search>text,string,textbox,input,field</search>
    public static FormElement TextBox(
        string label,
        string defaultValue = "",
        string placeholder = "",
        string key = "",
        string tooltip = "",
        string helpText = "")
        => new TextBoxElement
        {
            Label = label,
            DefaultValue = defaultValue,
            Placeholder = NodeSupport.OrNull(placeholder),
        }.WithCommon(key, tooltip, helpText);

    /// <summary>
    /// A multi-line text field, for notes, justifications and descriptions.
    ///
    /// The answer is a single string with line breaks inside it, not a list of lines. Split it
    /// downstream if you want the lines separately.
    ///
    /// <c>lines</c> sets the height the field occupies, not a limit on what can be typed: the box
    /// scrolls once the text outgrows it.
    /// </summary>
    /// <param name="label">Caption shown beside the field.</param>
    /// <param name="defaultValue">Value the field starts with.</param>
    /// <param name="lines">Visible height, in lines.</param>
    /// <param name="placeholder">Grey prompt shown while the field is empty.</param>
    /// <param name="key">Name of this answer in the results. Derived from the label when empty.</param>
    /// <param name="tooltip">Hover text.</param>
    /// <param name="helpText">A line of guidance shown under the field.</param>
    /// <returns name="element">The form element.</returns>
    /// <search>text,multiline,notes,paragraph,textarea</search>
    public static FormElement TextArea(
        string label,
        string defaultValue = "",
        int lines = 4,
        string placeholder = "",
        string key = "",
        string tooltip = "",
        string helpText = "")
        => new TextBoxElement
        {
            Label = label,
            DefaultValue = defaultValue,
            IsMultiline = true,
            Lines = Math.Max(2, lines),
            Placeholder = NodeSupport.OrNull(placeholder),
        }.WithCommon(key, tooltip, helpText);

    /// <summary>
    /// A masked text field, showing dots instead of characters as they are typed.
    ///
    /// Be clear about what this does and does not give you. **The answer comes back as plain
    /// text**, and it is held in memory with the form's other remembered answers for the rest of
    /// the Dynamo session — pass <c>rememberValues: false</c> to <c>Form.Show</c> if that matters.
    /// Nothing is written to disk, and nothing is encrypted. The masking stops somebody reading
    /// the screen over a shoulder, which is the whole of its job.
    ///
    /// There is no default value on purpose: a password baked into a saved graph is a password
    /// shared with everybody the graph is sent to.
    /// </summary>
    /// <param name="label">Caption shown beside the field.</param>
    /// <param name="placeholder">Grey prompt shown while the field is empty.</param>
    /// <param name="key">Name of this answer in the results. Derived from the label when empty.</param>
    /// <param name="tooltip">Hover text.</param>
    /// <param name="helpText">A line of guidance shown under the field.</param>
    /// <returns name="element">The form element.</returns>
    /// <search>password,secret,masked,credential</search>
    public static FormElement Password(
        string label,
        string placeholder = "",
        string key = "",
        string tooltip = "",
        string helpText = "")
        => new PasswordElement
        {
            Label = label,
            Placeholder = NodeSupport.OrNull(placeholder),
        }.WithCommon(key, tooltip, helpText);

    /// <summary>
    /// A decimal number field, with spinner buttons and an optional unit suffix.
    ///
    /// The answer is a number, never a string, so it can go straight into arithmetic. Read it with
    /// <c>Result.GetNumber</c>.
    ///
    /// <c>minimum</c> and <c>maximum</c> clamp what the field will accept as it is typed, which is
    /// not the same as validating it: a value outside the range never gets entered rather than
    /// being entered and then complained about. Leave them null for an unbounded field.
    ///
    /// <c>unit</c> is decoration shown inside the field — it is not converted or appended to the
    /// answer, which stays a bare number.
    /// </summary>
    /// <param name="label">Caption shown beside the field.</param>
    /// <param name="defaultValue">Value the field starts with.</param>
    /// <param name="minimum">Lowest allowed value. Null for no lower bound.</param>
    /// <param name="maximum">Highest allowed value. Null for no upper bound.</param>
    /// <param name="increment">Step applied by the spinner buttons and arrow keys.</param>
    /// <param name="decimalPlaces">Digits shown after the decimal separator.</param>
    /// <param name="unit">Suffix shown inside the field, such as "mm".</param>
    /// <param name="key">Name of this answer in the results. Derived from the label when empty.</param>
    /// <param name="tooltip">Hover text.</param>
    /// <param name="helpText">A line of guidance shown under the field.</param>
    /// <returns name="element">The form element.</returns>
    /// <search>number,double,decimal,numeric,value</search>
    public static FormElement Number(
        string label,
        double defaultValue = 0,
        [DefaultArgument("null")] object? minimum = null,
        [DefaultArgument("null")] object? maximum = null,
        double increment = 1,
        int decimalPlaces = 2,
        string unit = "",
        string key = "",
        string tooltip = "",
        string helpText = "")
        => new NumericElement
        {
            Label = label,
            DefaultValue = defaultValue,
            Minimum = NodeSupport.OptionalDouble(minimum),
            Maximum = NodeSupport.OptionalDouble(maximum),
            Increment = increment,
            DecimalPlaces = Math.Max(0, decimalPlaces),
            Unit = NodeSupport.OrNull(unit),
        }.WithCommon(key, tooltip, helpText);

    /// <summary>
    /// A whole-number field: counts, quantities, indices — anything a fraction would be nonsense
    /// for.
    ///
    /// The answer is an integer. Read it with <c>Result.GetInteger</c>. This is the node to reach
    /// for rather than <c>Input.Number</c> with the decimal places set to zero, because that one
    /// still hands back 3.0 where this hands back 3, and the difference shows up downstream in
    /// list indices and string formatting.
    /// </summary>
    /// <param name="label">Caption shown beside the field.</param>
    /// <param name="defaultValue">Value the field starts with.</param>
    /// <param name="minimum">Lowest allowed value. Null for no lower bound.</param>
    /// <param name="maximum">Highest allowed value. Null for no upper bound.</param>
    /// <param name="increment">Step applied by the spinner buttons and arrow keys.</param>
    /// <param name="unit">Suffix shown inside the field.</param>
    /// <param name="key">Name of this answer in the results. Derived from the label when empty.</param>
    /// <param name="tooltip">Hover text.</param>
    /// <param name="helpText">A line of guidance shown under the field.</param>
    /// <returns name="element">The form element.</returns>
    /// <search>integer,whole,count,int</search>
    public static FormElement Integer(
        string label,
        int defaultValue = 0,
        [DefaultArgument("null")] object? minimum = null,
        [DefaultArgument("null")] object? maximum = null,
        int increment = 1,
        string unit = "",
        string key = "",
        string tooltip = "",
        string helpText = "")
        => new IntegerElement
        {
            Label = label,
            DefaultValue = defaultValue,
            Minimum = NodeSupport.OptionalInt(minimum),
            Maximum = NodeSupport.OptionalInt(maximum),
            Increment = increment,
            Unit = NodeSupport.OrNull(unit),
        }.WithCommon(key, tooltip, helpText);

    /// <summary>
    /// A number chosen by dragging along a track, with the value shown beside it.
    ///
    /// Good when the range matters more than the exact figure — an opacity, a tolerance, a
    /// percentage — and the user is choosing by feel. When the exact figure is the point, and
    /// especially when it might be typed from a specification, <c>Input.Number</c> is kinder: a
    /// slider cannot be typed into.
    ///
    /// The answer is a number. <c>step</c> snaps the track to increments; zero lets it move
    /// continuously.
    /// </summary>
    /// <param name="label">Caption shown beside the field.</param>
    /// <param name="minimum">Left end of the track.</param>
    /// <param name="maximum">Right end of the track.</param>
    /// <param name="defaultValue">Value the slider starts at.</param>
    /// <param name="step">Snap increment. Zero for continuous.</param>
    /// <param name="decimalPlaces">Digits shown in the readout.</param>
    /// <param name="key">Name of this answer in the results. Derived from the label when empty.</param>
    /// <param name="tooltip">Hover text.</param>
    /// <param name="helpText">A line of guidance shown under the field.</param>
    /// <returns name="element">The form element.</returns>
    /// <search>slider,range,drag,number</search>
    public static FormElement Slider(
        string label,
        double minimum = 0,
        double maximum = 100,
        double defaultValue = 0,
        double step = 1,
        int decimalPlaces = 2,
        string key = "",
        string tooltip = "",
        string helpText = "")
        => new SliderElement
        {
            Label = label,
            Minimum = minimum,
            Maximum = maximum,
            DefaultValue = defaultValue,
            Step = step,
            DecimalPlaces = Math.Max(0, decimalPlaces),
        }.WithCommon(key, tooltip, helpText);

    /// <summary>
    /// A tick box. The answer is true or false, and never null.
    ///
    /// Note where the wording goes: a tick box has no separate label column. What you pass as
    /// <c>label</c> is printed beside the box, and the answer key is derived from that wording.
    /// Phrase it as the thing being turned on — "Include sheets", not "Sheets" — because the user
    /// reads it as a statement they are agreeing to.
    ///
    /// This is what <c>Condition.IsChecked</c> tests, which makes it the usual way to reveal part
    /// of a form: tick a box, and a group appears.
    /// </summary>
    /// <param name="label">Text shown beside the box.</param>
    /// <param name="defaultValue">Whether the box starts ticked.</param>
    /// <param name="key">Name of this answer in the results. Derived from the label when empty.</param>
    /// <param name="tooltip">Hover text.</param>
    /// <param name="helpText">A line of guidance shown under the field.</param>
    /// <returns name="element">The form element.</returns>
    /// <search>checkbox,tick,boolean,toggle,yes,no</search>
    public static FormElement CheckBox(
        string label,
        bool defaultValue = false,
        string key = "",
        string tooltip = "",
        string helpText = "")
    {
        // The wording sits beside the box rather than in a label column, which is how a check
        // box reads. The key still derives from that wording.
        CheckBoxElement element = new()
        {
            Content = label,
            DefaultValue = defaultValue,
        };

        return element.WithCommon(
            string.IsNullOrWhiteSpace(key) ? FormKeys.Slugify(label) : key,
            tooltip,
            helpText);
    }

    /// <summary>
    /// An on/off switch. The answer is true or false, exactly as a tick box.
    ///
    /// The difference from <c>Input.CheckBox</c> is what it says to the reader, not what it
    /// returns. A switch reads as a setting that takes effect — a mode being turned on — and it
    /// gets its own caption in the label column plus wording for each state. A tick box reads as a
    /// statement being agreed to. Pick by which sentence fits.
    /// </summary>
    /// <param name="label">Caption shown beside the switch.</param>
    /// <param name="defaultValue">Whether the switch starts on.</param>
    /// <param name="onText">Wording shown when the switch is on.</param>
    /// <param name="offText">Wording shown when the switch is off.</param>
    /// <param name="key">Name of this answer in the results. Derived from the label when empty.</param>
    /// <param name="tooltip">Hover text.</param>
    /// <param name="helpText">A line of guidance shown under the field.</param>
    /// <returns name="element">The form element.</returns>
    /// <search>toggle,switch,boolean,on,off</search>
    public static FormElement Toggle(
        string label,
        bool defaultValue = false,
        string onText = "On",
        string offText = "Off",
        string key = "",
        string tooltip = "",
        string helpText = "")
        => new ToggleElement
        {
            Label = label,
            DefaultValue = defaultValue,
            OnText = onText,
            OffText = offText,
        }.WithCommon(key, tooltip, helpText);

    /// <summary>
    /// A drop-down list, for one choice out of many.
    ///
    /// **The answer is the selected item itself, not its display name.** Feed in Revit elements,
    /// family types, whatever you have, and pass their names separately as <c>displayNames</c>;
    /// what comes back is the object you put in, ready to use. This is the difference that removes
    /// the lookup-by-name step — and the bug where two things share a name — from the middle of
    /// every graph that asks the user to pick something.
    ///
    /// With no <c>defaultValue</c> and no <c>placeholder</c>, the first item starts selected, so
    /// the field is never empty. Give a <c>placeholder</c> instead when "nothing chosen yet" is a
    /// state you want to be able to tell apart, and pair it with <c>Behavior.Required</c>.
    ///
    /// Above roughly a dozen options this beats <c>Input.RadioButtons</c> on space; below about
    /// four, radio buttons show every choice at once and save a click.
    /// </summary>
    /// <param name="label">Caption shown beside the field.</param>
    /// <param name="items">The values to choose between. Can be any objects.</param>
    /// <param name="displayNames">What to show for each item. Falls back to each item's own text.</param>
    /// <param name="defaultValue">Which item starts selected. Null selects the first.</param>
    /// <param name="placeholder">Grey prompt shown while nothing is selected.</param>
    /// <param name="key">Name of this answer in the results. Derived from the label when empty.</param>
    /// <param name="tooltip">Hover text.</param>
    /// <param name="helpText">A line of guidance shown under the field.</param>
    /// <returns name="element">The form element.</returns>
    /// <search>dropdown,combobox,select,choose,list,pick</search>
    public static FormElement DropDown(
        string label,
        [DefaultArgument("null")] List<object>? items = null,
        [DefaultArgument("null")] List<object>? displayNames = null,
        [DefaultArgument("null")] object? defaultValue = null,
        string placeholder = "",
        string key = "",
        string tooltip = "",
        string helpText = "")
        => new DropdownElement
        {
            Label = label,
            Options = NodeSupport.Options(items, displayNames),
            DefaultValue = defaultValue,
            Placeholder = NodeSupport.OrNull(placeholder),
            SelectFirstByDefault = defaultValue is null && string.IsNullOrWhiteSpace(placeholder),
        }.WithCommon(key, tooltip, helpText);

    /// <summary>
    /// A list box showing several options at once, with a filter above it.
    ///
    /// **The shape of the answer depends on <paramref name="allowMultiple"/>**, and this is the
    /// thing to get right before wiring anything downstream. With it true — the default — the
    /// answer is a *list* of chosen items, empty when nothing is picked. With it false the answer
    /// is a single item. Read the multiple case with <c>Result.GetList</c>.
    ///
    /// As with every choice input, what comes back is the object that went in, not its display
    /// name.
    ///
    /// Prefer this to <c>Input.DropDown</c> when the user needs to see the options without opening
    /// anything, when they may need more than one, or when there are enough of them that the
    /// filter box earns its place.
    /// </summary>
    /// <param name="label">Caption shown beside the field.</param>
    /// <param name="items">The values to choose between. Can be any objects.</param>
    /// <param name="displayNames">What to show for each item. Falls back to each item's own text.</param>
    /// <param name="allowMultiple">Whether several items can be chosen at once.</param>
    /// <param name="defaultValue">Which item or items start selected.</param>
    /// <param name="visibleRows">How many rows are shown before the list scrolls.</param>
    /// <param name="key">Name of this answer in the results. Derived from the label when empty.</param>
    /// <param name="tooltip">Hover text.</param>
    /// <param name="helpText">A line of guidance shown under the field.</param>
    /// <returns name="element">The form element.</returns>
    /// <search>listbox,list,multiselect,select,choose</search>
    public static FormElement ListBox(
        string label,
        [DefaultArgument("null")] List<object>? items = null,
        [DefaultArgument("null")] List<object>? displayNames = null,
        bool allowMultiple = true,
        [DefaultArgument("null")] List<object>? defaultValue = null,
        int visibleRows = 6,
        string key = "",
        string tooltip = "",
        string helpText = "")
        => new ListSelectionElement
        {
            Label = label,
            Options = NodeSupport.Options(items, displayNames),
            AllowMultiple = allowMultiple,
            DefaultValue = defaultValue,
            VisibleRows = Math.Max(2, visibleRows),
        }.WithCommon(key, tooltip, helpText);

    /// <summary>
    /// A set of mutually exclusive radio buttons, every choice visible at once.
    ///
    /// The answer is the selected item itself, as with the other choice inputs. One is always
    /// selected — the first, unless <c>defaultValue</c> says otherwise — so there is no "nothing
    /// chosen" state to guard against.
    ///
    /// Best for two to five options where the choice steers the rest of the form, because the user
    /// can read every alternative without opening anything. Past that it costs more vertical space
    /// than it is worth and <c>Input.DropDown</c> is the better shape.
    /// </summary>
    /// <param name="label">Caption shown beside the field.</param>
    /// <param name="items">The values to choose between. Can be any objects.</param>
    /// <param name="displayNames">What to show for each item. Falls back to each item's own text.</param>
    /// <param name="defaultValue">Which item starts selected. Null selects the first.</param>
    /// <param name="horizontal">Lay the buttons out in a row instead of a column.</param>
    /// <param name="key">Name of this answer in the results. Derived from the label when empty.</param>
    /// <param name="tooltip">Hover text.</param>
    /// <param name="helpText">A line of guidance shown under the field.</param>
    /// <returns name="element">The form element.</returns>
    /// <search>radio,option,choice,exclusive</search>
    public static FormElement RadioButtons(
        string label,
        [DefaultArgument("null")] List<object>? items = null,
        [DefaultArgument("null")] List<object>? displayNames = null,
        [DefaultArgument("null")] object? defaultValue = null,
        bool horizontal = false,
        string key = "",
        string tooltip = "",
        string helpText = "")
        => new RadioGroupElement
        {
            Label = label,
            Options = NodeSupport.Options(items, displayNames),
            DefaultValue = defaultValue,
            Orientation = horizontal ? LayoutOrientation.Horizontal : LayoutOrientation.Vertical,
        }.WithCommon(key, tooltip, helpText);

    /// <summary>
    /// A hierarchy to pick from — levels and rooms, disciplines and sheets, folders and files.
    ///
    /// The branches are built from <c>Input.TreeItem</c> nodes, nested by feeding items into a
    /// parent's <c>children</c> port. As with the flat choice inputs the answer is whatever each
    /// item's <c>value</c> was, and with <c>allowMultiple</c> it is a list of them.
    ///
    /// A branch that only groups its children should be built with <c>selectable: false</c>, so
    /// the user cannot return a category where the graph expects a thing.
    /// </summary>
    /// <param name="label">Caption shown beside the field.</param>
    /// <param name="nodes">The root items of the tree.</param>
    /// <param name="allowMultiple">Whether several items can be chosen at once.</param>
    /// <param name="defaultValue">Which item or items start selected.</param>
    /// <param name="expandAll">Whether every branch starts open.</param>
    /// <param name="key">Name of this answer in the results. Derived from the label when empty.</param>
    /// <param name="tooltip">Hover text.</param>
    /// <param name="helpText">A line of guidance shown under the field.</param>
    /// <returns name="element">The form element.</returns>
    /// <search>tree,hierarchy,nested,select</search>
    public static FormElement TreeSelect(
        string label,
        [DefaultArgument("null")] List<object>? nodes = null,
        bool allowMultiple = true,
        [DefaultArgument("null")] List<object>? defaultValue = null,
        bool expandAll = false,
        string key = "",
        string tooltip = "",
        string helpText = "")
        => new TreeSelectionElement
        {
            Label = label,
            Roots = NodeSupport.Items(nodes).OfType<TreeNode>().ToList(),
            AllowMultiple = allowMultiple,
            DefaultValue = defaultValue,
            ExpandAll = expandAll,
        }.WithCommon(key, tooltip, helpText);

    /// <summary>
    /// One item of a tree, for <c>Input.TreeSelect</c>.
    ///
    /// Build a hierarchy by feeding items into a parent's <c>children</c> port, as deep as you
    /// like. This is the only Input node that does not produce a form element on its own: it is
    /// the material <c>Input.TreeSelect</c> is made of, and placing it anywhere else does nothing.
    ///
    /// <c>value</c> is what selecting the item returns, and falls back to the display name when
    /// left empty — so a tree of plain strings needs nothing else, while a tree of Revit elements
    /// carries them through untouched.
    /// </summary>
    /// <param name="displayName">What the user reads.</param>
    /// <param name="value">What choosing this item returns. Falls back to the display name.</param>
    /// <param name="children">Items nested beneath this one.</param>
    /// <param name="expanded">Whether this item starts open.</param>
    /// <param name="selectable">False for a branch that only groups its children.</param>
    /// <returns name="treeItem">The tree item.</returns>
    /// <search>tree,item,node,branch,leaf</search>
    public static TreeNode TreeItem(
        string displayName,
        [DefaultArgument("null")] object? value = null,
        [DefaultArgument("null")] List<object>? children = null,
        bool expanded = false,
        bool selectable = true)
        => new()
        {
            Display = displayName,
            Value = value ?? displayName,
            Children = NodeSupport.Items(children).OfType<TreeNode>().ToList(),
            IsExpanded = expanded,
            IsSelectable = selectable,
        };

    /// <summary>
    /// A calendar field, optionally with a time of day.
    ///
    /// The answer is a DateTime — **or null, when the field is left empty**. This is the one input
    /// whose answer can genuinely be nothing, so either attach <c>Behavior.Required</c> or handle
    /// the empty case downstream. <c>Result.GetDate</c> takes a fallback for exactly this.
    ///
    /// The field is shown and typed in the machine's own date format, so a user in one region and
    /// a user in another see what each expects. Only the display is regional; the value that
    /// comes back is a proper date, not the text of one.
    ///
    /// To make one date depend on another — an end after a start — use <c>Rule.CompareTo</c> with
    /// <c>Behavior.WithValidation</c>.
    /// </summary>
    /// <param name="label">Caption shown beside the field.</param>
    /// <param name="defaultValue">Date the field starts on.</param>
    /// <param name="includeTime">Add a time-of-day box beside the calendar.</param>
    /// <param name="minimum">Earliest selectable date.</param>
    /// <param name="maximum">Latest selectable date.</param>
    /// <param name="key">Name of this answer in the results. Derived from the label when empty.</param>
    /// <param name="tooltip">Hover text.</param>
    /// <param name="helpText">A line of guidance shown under the field.</param>
    /// <returns name="element">The form element.</returns>
    /// <search>date,calendar,time,when,datetime</search>
    public static FormElement DatePicker(
        string label,
        [DefaultArgument("null")] object? defaultValue = null,
        bool includeTime = false,
        [DefaultArgument("null")] object? minimum = null,
        [DefaultArgument("null")] object? maximum = null,
        string key = "",
        string tooltip = "",
        string helpText = "")
        => new DatePickerElement
        {
            Label = label,
            DefaultValue = NodeSupport.OptionalDate(defaultValue),
            IncludeTime = includeTime,
            Minimum = NodeSupport.OptionalDate(minimum),
            Maximum = NodeSupport.OptionalDate(maximum),
        }.WithCommon(key, tooltip, helpText);

    /// <summary>
    /// A colour field: a swatch that opens a picker, beside a hex box that can be typed into.
    ///
    /// The answer is an Interlude colour rather than a string or a Revit colour, so read it with
    /// <c>Result.GetColor</c> — which hands back the hex text and the red, green, blue and alpha
    /// numbers together, and you take whichever the next node wants.
    ///
    /// <c>presets</c> is the practical way to keep a team on a palette: offer the office's own
    /// colours as swatches above the picker, and the free choice underneath stays available for
    /// the case nobody anticipated.
    /// </summary>
    /// <param name="label">Caption shown beside the field.</param>
    /// <param name="defaultValue">Starting colour, as hex such as "#3366CC".</param>
    /// <param name="showAlpha">Add an opacity slider.</param>
    /// <param name="presets">Hex colours offered as swatches above the picker.</param>
    /// <param name="key">Name of this answer in the results. Derived from the label when empty.</param>
    /// <param name="tooltip">Hover text.</param>
    /// <param name="helpText">A line of guidance shown under the field.</param>
    /// <returns name="element">The form element.</returns>
    /// <search>colour,color,swatch,rgb,hex,picker</search>
    public static FormElement ColorPicker(
        string label,
        string defaultValue = "#000000",
        bool showAlpha = false,
        [DefaultArgument("null")] List<object>? presets = null,
        string key = "",
        string tooltip = "",
        string helpText = "")
        => new ColorPickerElement
        {
            Label = label,
            DefaultValue = NodeSupport.OptionalColor(defaultValue) ?? RgbColor.Black,
            ShowAlpha = showAlpha,
            Presets = NodeSupport.Items(presets)
                .Select(NodeSupport.OptionalColor)
                .Where(colour => colour.HasValue)
                .Select(colour => colour!.Value)
                .ToList(),
        }.WithCommon(key, tooltip, helpText);

    /// <summary>
    /// A file path with a Browse button, and a box that can also be typed or pasted into.
    ///
    /// **The shape of the answer depends on <paramref name="allowMultiple"/>**: false gives a
    /// single path string, true gives a list of them. <c>Result.GetFilePaths</c> always hands back
    /// a list, whichever way the field was configured, which saves the graph from caring.
    ///
    /// <c>forSaving</c> switches to a save dialog — one that will happily name a file that does
    /// not exist yet. That is the point of it, and it is also why attaching
    /// <c>Rule.FileExists</c> to a saving field is a contradiction.
    ///
    /// Browsing does not read the file or check that it is what the filter claims; the answer is
    /// a path, and opening it is the graph's business.
    /// </summary>
    /// <param name="label">Caption shown beside the field.</param>
    /// <param name="defaultValue">Path the field starts with.</param>
    /// <param name="filter">Dialog filter, such as "Revit files|*.rvt|All files|*.*".</param>
    /// <param name="allowMultiple">Whether several files can be chosen.</param>
    /// <param name="forSaving">Show a save dialog instead of an open dialog.</param>
    /// <param name="key">Name of this answer in the results. Derived from the label when empty.</param>
    /// <param name="tooltip">Hover text.</param>
    /// <param name="helpText">A line of guidance shown under the field.</param>
    /// <returns name="element">The form element.</returns>
    /// <search>file,path,browse,open,save,filepath</search>
    public static FormElement FilePath(
        string label,
        string defaultValue = "",
        string filter = "All files|*.*",
        bool allowMultiple = false,
        bool forSaving = false,
        string key = "",
        string tooltip = "",
        string helpText = "")
        => new FilePickerElement
        {
            Label = label,
            DefaultValue = defaultValue,
            Filter = string.IsNullOrWhiteSpace(filter) ? "All files|*.*" : filter,
            AllowMultiple = allowMultiple,
            IsSaveDialog = forSaving,
        }.WithCommon(key, tooltip, helpText);

    /// <summary>
    /// A folder path with a Browse button, and a box that can also be typed or pasted into.
    ///
    /// The answer is a single path string, without a trailing separator. The folder is not created
    /// and not checked — attach <c>Rule.FolderExists</c> with <c>Behavior.WithValidation</c> when
    /// the graph cannot cope with being pointed at somewhere that is not there, which is worth
    /// doing for an export destination typed by hand.
    /// </summary>
    /// <param name="label">Caption shown beside the field.</param>
    /// <param name="defaultValue">Path the field starts with.</param>
    /// <param name="key">Name of this answer in the results. Derived from the label when empty.</param>
    /// <param name="tooltip">Hover text.</param>
    /// <param name="helpText">A line of guidance shown under the field.</param>
    /// <returns name="element">The form element.</returns>
    /// <search>folder,directory,path,browse</search>
    public static FormElement DirectoryPath(
        string label,
        string defaultValue = "",
        string key = "",
        string tooltip = "",
        string helpText = "")
        => new FolderPickerElement
        {
            Label = label,
            DefaultValue = defaultValue,
        }.WithCommon(key, tooltip, helpText);

    /// <summary>
    /// A button that lets the user pick elements directly in the Revit model. The form minimises
    /// while they pick and comes back when they finish, with a summary of what they chose beside
    /// the button.
    ///
    /// **The answer is the picked Revit element itself** — the same element every Dynamo Revit
    /// node works with — not an id or a name. A multi-select field stores a list of elements and a
    /// single-select field stores one, read with <c>Result.GetList</c> or straight out of
    /// <c>values</c>. Pressing Escape during the pick keeps whatever was selected before.
    ///
    /// This only works with Dynamo running inside Revit, and Interlude still references no Revit
    /// assembly: the picking goes through the Revit API that is already loaded in the process.
    /// Anywhere else — Dynamo Sandbox, a saved form opened for review — the button is disabled
    /// with an explanation, and the rest of the form works normally.
    ///
    /// Elements cannot ride along in a saved form file, for the same reason as drop-down options:
    /// they do not exist in another model. The field's configuration round-trips; its answer is
    /// live model data.
    /// </summary>
    /// <param name="label">Caption shown beside the field.</param>
    /// <param name="allowMultiple">Whether several elements can be picked. False ends the pick at the first click.</param>
    /// <param name="buttonText">Caption on the button. Empty gets "Select in model…".</param>
    /// <param name="prompt">Text shown in Revit's status bar while picking.</param>
    /// <param name="defaultValue">Elements the field starts with, from the graph.</param>
    /// <param name="key">Name of this answer in the results. Derived from the label when empty.</param>
    /// <param name="tooltip">Hover text.</param>
    /// <param name="helpText">A line of guidance shown under the field.</param>
    /// <returns name="element">The form element.</returns>
    /// <search>select,revit,pick,model,element,selection</search>
    public static FormElement SelectElements(
        string label,
        bool allowMultiple = true,
        string buttonText = "",
        string prompt = "",
        [DefaultArgument("null")] List<object>? defaultValue = null,
        string key = "",
        string tooltip = "",
        string helpText = "")
        => new ModelSelectionElement
        {
            Label = label,
            AllowMultiple = allowMultiple,
            ButtonText = NodeSupport.OrNull(buttonText),
            Prompt = NodeSupport.OrNull(prompt),
            DefaultValue = defaultValue,
        }.WithCommon(key, tooltip, helpText);
}
