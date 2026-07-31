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
    /// A single-line text field.
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
    /// A multi-line text field.
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
    /// A masked text field. The answer is returned as plain text.
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
    /// A decimal number field.
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
    /// A whole-number field.
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
    /// A number chosen by dragging along a track.
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
    /// A tick box. The answer is true or false.
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
    /// An on/off switch. The answer is true or false.
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
    /// A drop-down list. The answer is the selected item itself, not its display name.
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
        [DefaultArgument("null")] object? items = null,
        [DefaultArgument("null")] object? displayNames = null,
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
    /// A list to pick from. With <paramref name="allowMultiple"/> the answer is a list of the
    /// chosen items; otherwise it is the single chosen item.
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
        [DefaultArgument("null")] object? items = null,
        [DefaultArgument("null")] object? displayNames = null,
        bool allowMultiple = true,
        [DefaultArgument("null")] object? defaultValue = null,
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
    /// A set of mutually exclusive radio buttons.
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
        [DefaultArgument("null")] object? items = null,
        [DefaultArgument("null")] object? displayNames = null,
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
    /// A hierarchy to pick from, built from <c>Input.TreeItem</c> nodes.
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
        [DefaultArgument("null")] object? nodes = null,
        bool allowMultiple = true,
        [DefaultArgument("null")] object? defaultValue = null,
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
    /// One item of a tree, for <c>Input.TreeSelect</c>. Nest these to build a hierarchy.
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
        [DefaultArgument("null")] object? children = null,
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
    /// A calendar field. The answer is a DateTime, or null when left empty.
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
    /// A colour field. The answer is an Interlude colour; use <c>Result.GetColor</c> to read it
    /// as a hex string or as red, green, blue and alpha numbers.
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
        [DefaultArgument("null")] object? presets = null,
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
    /// A file path with a Browse button. With <paramref name="allowMultiple"/> the answer is a
    /// list of paths; otherwise it is a single path string.
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
    /// A folder path with a Browse button.
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
}
