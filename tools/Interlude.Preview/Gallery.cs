using System;
using System.Collections.Generic;
using System.Linq;
using Interlude.Conditions;
using Interlude.Model;
using Interlude.Theming;
using Interlude.Validation;

namespace Interlude.Preview;

/// <summary>One entry in the harness's list of example forms.</summary>
internal sealed record Sample(string Name, string Summary, Func<FormDefinition> Build)
{
    public override string ToString() => Name;
}

/// <summary>
/// The example forms the harness shows.
///
/// These are not decoration. Between them they cover every control, every container, conditional
/// visibility, computed values, live validation and both themes, which makes the harness the
/// fastest way to see whether a rendering change broke something the tests do not look at —
/// spacing, alignment, contrast, focus order.
/// </summary>
internal static class Gallery
{
    internal static IReadOnlyList<Sample> Samples { get; } = new List<Sample>
    {
        new("Every control", "One of each input, for checking alignment and spacing.", EveryControl),
        new("Every container", "Stacks, grids, tabs, cards, splitters.", EveryContainer),
        new("Conditional form", "Fields that appear, enable and become required in response to others.", Conditional),
        new("Computed values", "A quantity takeoff whose totals recalculate as you type.", Computed),
        new("Validation", "Rules that fire while typing, including one that reads another field.", Validation),
        new("Long form", "Fifty fields, for checking scrolling and performance.", LongForm),
        new("Minimal", "The smallest useful form.", Minimal),
        new("Mono", "The monochrome theme: pills, spaced capitals, no colour.", Mono),
        new("Neubrutalism", "The default theme, leaning all the way in.", Neubrutalism),
    };

    /// <summary>
    /// The default theme with enough variety to judge it: heavy outlines against flat colour,
    /// hard shadows under every control, and a card sitting on one of its own.
    /// </summary>
    private static FormDefinition Neubrutalism() => new FormDefinition
    {
        Title = "Sheet set-up",
        Description = "Heavy outlines, hard shadows, and no gradients anywhere.",
        Window = new WindowOptions { Width = 520 },
        Elements = new FormElement[]
        {
            new LabelElement { Text = "Naming", HeadingLevel = 3 },
            new TextBoxElement { Key = "prefix", Label = "Prefix", Placeholder = "A-1", DefaultValue = "A-1" },
            new DropdownElement
            {
                Key = "discipline",
                Label = "Discipline",
                Options = Options("Architectural", "Structural", "Mechanical"),
            },
            new IntegerElement { Key = "count", Label = "Sheets", DefaultValue = 12, Minimum = 1, Maximum = 200 },
            new SeparatorElement { Caption = "Options" },
            new CheckBoxElement { Key = "titleblock", Content = "Place a title block", DefaultValue = true },
            new ToggleElement { Key = "revision", Label = "Revision", OnText = "On", OffText = "Off" },
            new SliderElement
            {
                Key = "scale",
                Label = "Scale",
                Minimum = 1,
                Maximum = 10,
                DefaultValue = 4,
                DecimalPlaces = 0,
            },
            new CardElement
            {
                Header = "Progress",
                Subheader = "Sheets issued this month",
                HasShadow = true,
                Children = new FormElement[]
                {
                    new ProgressElement { Value = 8, Maximum = 12, Segments = 12, ShowPercentage = false },
                    new ProgressElement { Value = 8, Maximum = 12 },
                },
            },
        },
    }.WithResolvedKeys();

    /// <summary>
    /// The monochrome theme on a form with enough variety to judge it: pill controls, capitalised
    /// headings, a segmented bar, and the embedded font doing the talking.
    /// </summary>
    private static FormDefinition Mono() => new FormDefinition
    {
        Title = "Habit tracking",
        Description = "Monochrome, pill-shaped, and set in Comic Neue.",
        Window = new WindowOptions { Width = 480 },
        Theme = new ThemeDefinition
        {
            Mode = AppearanceMode.Light,
            Shape = ControlShape.Pill,
            UppercaseHeaders = true,
            HeaderTracking = 0.08d,
            LightPalette = ThemePalette.Light with
            {
                Background = RgbColor.Parse("#FFFFFF"),
                Surface = RgbColor.Parse("#F7F7F8"),
                SurfaceAlt = RgbColor.Parse("#EEEEF1"),
                Border = RgbColor.Parse("#D9D9DE"),
                BorderStrong = RgbColor.Parse("#9A9AA4"),
                Foreground = RgbColor.Parse("#16161A"),
                ForegroundMuted = RgbColor.Parse("#6B6B75"),
                ControlBackground = RgbColor.Parse("#FFFFFF"),
                ControlBackgroundHover = RgbColor.Parse("#F2F2F4"),
                Accent = RgbColor.Parse("#16161A"),
                AccentHover = RgbColor.Parse("#33333A"),
                AccentForeground = RgbColor.Parse("#FFFFFF"),
                Error = RgbColor.Parse("#B3261E"),
            },
        },
        Elements = new FormElement[]
        {
            new LabelElement { Text = "This week", HeadingLevel = 3 },
            new ProgressElement { Value = 6, Maximum = 7, Segments = 7, ShowPercentage = false },
            new SeparatorElement(),
            new TextBoxElement { Key = "habit", Label = "Habit", Placeholder = "Drink water" },
            new DropdownElement
            {
                Key = "repeat",
                Label = "Repeat",
                Options = Options("Every day", "Weekdays", "Weekends"),
            },
            new SliderElement { Key = "target", Label = "Target", Minimum = 1, Maximum = 10, DefaultValue = 7, DecimalPlaces = 0 },
            new CheckBoxElement { Key = "remind", Content = "Remind me" },
            new ToggleElement { Key = "active", Label = "Active", OnText = "On", OffText = "Off", DefaultValue = true },
            new GroupBoxElement
            {
                Header = "Advanced",
                Children = new FormElement[]
                {
                    new NumericElement { Key = "streak", Label = "Streak goal", DefaultValue = 30, Unit = "days" },
                    new ColorPickerElement { Key = "tint", Label = "Tint", DefaultValue = RgbColor.Parse("#16161A") },
                },
            },
        },
    }.WithResolvedKeys();

    private static FormDefinition Minimal() => new FormDefinition
    {
        Title = "Rename views",
        Description = "The smallest form worth showing.",
        Elements = new FormElement[]
        {
            new TextBoxElement { Label = "Prefix", Placeholder = "e.g. WIP_", RequiredIf = ConstantCondition.True },
            new CheckBoxElement { Content = "Include sheets" },
        },
    }.WithResolvedKeys();

    private static FormDefinition EveryControl() => new FormDefinition
    {
        Title = "Every control",
        Description = "One of each, to check that they line up and read as one form.",
        Window = new WindowOptions { Width = 560, MaxHeight = 860 },
        Elements = new FormElement[]
        {
            new TextBoxElement { Label = "Text", Placeholder = "Type something" },
            new TextBoxElement { Label = "Notes", IsMultiline = true, Lines = 3, Placeholder = "Several lines" },
            new PasswordElement { Label = "Password", Placeholder = "Hidden" },
            new NumericElement { Label = "Number", DefaultValue = 1.75, Unit = "m", Minimum = 0, Maximum = 10 },
            new IntegerElement { Label = "Integer", DefaultValue = 3, Minimum = 0, Maximum = 100 },
            new SliderElement { Label = "Slider", Minimum = 0, Maximum = 100, DefaultValue = 40, Step = 5 },
            new DropdownElement
            {
                Label = "Dropdown",
                Options = Options("Concrete", "Steel", "Timber", "Masonry"),
            },
            new RadioGroupElement { Label = "Radio", Options = Options("Metric", "Imperial") },
            new CheckBoxElement { Content = "A check box" },
            new ToggleElement { Label = "Toggle", OnText = "Enabled", OffText = "Disabled" },
            new ListSelectionElement { Label = "Multi list", Options = Options("North", "South", "East", "West"), VisibleRows = 4 },
            new ListSelectionElement { Label = "Single list", AllowMultiple = false, Options = Options("A", "B", "C"), VisibleRows = 3 },
            new TreeSelectionElement
            {
                Label = "Tree",
                Roots = new[]
                {
                    new TreeNode
                    {
                        Display = "Level 1",
                        Value = "L1",
                        IsExpanded = true,
                        Children = new[]
                        {
                            new TreeNode { Display = "Room 101", Value = "101" },
                            new TreeNode { Display = "Room 102", Value = "102" },
                        },
                    },
                    new TreeNode { Display = "Level 2", Value = "L2" },
                },
            },
            new DatePickerElement { Label = "Date", DefaultToToday = true },
            new DatePickerElement { Label = "Date and time", IncludeTime = true, DefaultToToday = true },
            new ColorPickerElement
            {
                Label = "Colour",
                DefaultValue = RgbColor.Parse("#3366CC"),
                Presets = new[]
                {
                    RgbColor.Parse("#C42B1C"), RgbColor.Parse("#E3B341"), RgbColor.Parse("#1A7F37"),
                    RgbColor.Parse("#2F6FEB"), RgbColor.Parse("#6E40C9"), RgbColor.Parse("#1B1F24"),
                },
            },
            new FilePickerElement { Label = "File", Filter = "Revit files|*.rvt|All files|*.*" },
            new FolderPickerElement { Label = "Folder" },

            new SeparatorElement { Caption = "Display" },
            new LabelElement { Text = "A heading", HeadingLevel = 3 },
            new LabelElement { Text = "Muted body text sitting under it.", IsMuted = true },
            new MarkdownElement
            {
                Text = "Markdown supports **bold**, *italic*, `code` and [links](https://dynamobim.org).\n\n" +
                       "- bullet one\n- bullet two\n\n1. numbered\n2. numbered",
            },
            new ProgressElement { Value = 65 },
            new ButtonElement { Text = "A body button", Tag = "body" },
        },
    }.WithResolvedKeys();

    private static FormDefinition EveryContainer() => new FormDefinition
    {
        Title = "Every container",
        Description = "Layout containers, nested the way a real form nests them.",
        Window = new WindowOptions { Width = 620, MaxHeight = 820 },
        Elements = new FormElement[]
        {
            new CardElement
            {
                Header = "Card",
                Subheader = "A raised panel with a heading",
                Children = new FormElement[]
                {
                    new TextBoxElement { Label = "Inside a card", Style = new ElementStyle { LabelWidth = 0 } },
                },
            },
            new GroupBoxElement
            {
                Header = "Group box",
                Children = new FormElement[]
                {
                    new HStackElement
                    {
                        Children = new FormElement[]
                        {
                            new NumericElement { Label = "Width", Unit = "mm", Style = new ElementStyle { LabelWidth = 50 } },
                            new NumericElement { Label = "Height", Unit = "mm", Style = new ElementStyle { LabelWidth = 50 } },
                        },
                    },
                },
            },
            new ExpanderElement
            {
                Header = "Expander",
                IsExpanded = false,
                Children = new FormElement[] { new TextBoxElement { Label = "Hidden until opened" } },
            },
            new GridElement
            {
                Columns = new[] { GridTrack.Auto, GridTrack.Star, GridTrack.Star },
                Children = new FormElement[]
                {
                    new LabelElement { Text = "Grid:" },
                    new TextBoxElement { Style = new ElementStyle { LabelWidth = 0 } },
                    new TextBoxElement { Style = new ElementStyle { LabelWidth = 0 } },
                },
            },
            new TabsElement
            {
                Children = new FormElement[]
                {
                    new TabPageElement
                    {
                        Header = "General",
                        Children = new FormElement[] { new TextBoxElement { Label = "On tab one" } },
                    },
                    new TabPageElement
                    {
                        Header = "Advanced",
                        Children = new FormElement[] { new CheckBoxElement { Content = "On tab two" } },
                    },
                },
            },
            new SplitViewElement
            {
                SplitterPosition = 0.4,
                Style = new ElementStyle { Height = 120 },
                Children = new FormElement[]
                {
                    new CardElement { Header = "Left", Children = Array.Empty<FormElement>() },
                    new CardElement { Header = "Right", Children = Array.Empty<FormElement>() },
                },
            },
        },
    }.WithResolvedKeys();

    private static FormDefinition Conditional() => new FormDefinition
    {
        Title = "Export settings",
        Description = "Fields appear, enable and become required in response to other fields.",
        Elements = new FormElement[]
        {
            new DropdownElement
            {
                Key = "format",
                Label = "Format",
                Options = Options("DWG", "IFC", "PDF"),
            },
            new GroupBoxElement
            {
                Header = "DWG options",
                VisibleIf = Equals("format", "DWG"),
                Children = new FormElement[]
                {
                    new DropdownElement { Key = "dwgVersion", Label = "Version", Options = Options("2013", "2018", "2024") },
                    new CheckBoxElement { Key = "explode", Content = "Explode nested families" },
                },
            },
            new GroupBoxElement
            {
                Header = "IFC options",
                VisibleIf = Equals("format", "IFC"),
                Children = new FormElement[]
                {
                    new DropdownElement { Key = "ifcVersion", Label = "Schema", Options = Options("IFC2x3", "IFC4") },
                    new CheckBoxElement { Key = "baseQuantities", Content = "Export base quantities" },
                },
            },
            new SeparatorElement(),
            new CheckBoxElement { Key = "toFolder", Content = "Export to a specific folder" },
            new FolderPickerElement
            {
                Key = "folder",
                Label = "Folder",
                EnabledIf = new ComparisonCondition { Key = "toFolder", Operator = ComparisonOperator.IsChecked },
                RequiredIf = new ComparisonCondition { Key = "toFolder", Operator = ComparisonOperator.IsChecked },
            },
            new CheckBoxElement { Key = "needsReason", Content = "This export needs a justification" },
            new TextBoxElement
            {
                Key = "reason",
                Label = "Justification",
                IsMultiline = true,
                Lines = 3,
                VisibleIf = new ComparisonCondition { Key = "needsReason", Operator = ComparisonOperator.IsChecked },
                RequiredIf = ConstantCondition.True,
            },
        },
    }.WithResolvedKeys();

    private static FormDefinition Computed() => new FormDefinition
    {
        Title = "Quantity takeoff",
        Description = "The totals are computed. Change a quantity and watch them settle in order.",
        Window = new WindowOptions { Width = 480 },
        Elements = new FormElement[]
        {
            new NumericElement { Key = "quantity", Label = "Quantity", DefaultValue = 12d, Minimum = 0 },
            new NumericElement { Key = "unitPrice", Label = "Unit price", DefaultValue = 45.5d, Minimum = 0, Unit = "£" },
            new NumericElement
            {
                Key = "subtotal",
                Label = "Subtotal",
                Unit = "£",
                ShowSpinner = false,
                Computed = new ArithmeticComputed
                {
                    Operator = ArithmeticOperator.Multiply,
                    Left = new FieldComputed { Key = "quantity" },
                    Right = new FieldComputed { Key = "unitPrice" },
                },
            },
            new SliderElement { Key = "vatRate", Label = "VAT", Minimum = 0, Maximum = 25, DefaultValue = 20, Step = 0.5, DecimalPlaces = 1 },
            new NumericElement
            {
                Key = "vat",
                Label = "VAT amount",
                Unit = "£",
                ShowSpinner = false,
                Computed = new ArithmeticComputed
                {
                    Operator = ArithmeticOperator.Divide,
                    Left = new ArithmeticComputed
                    {
                        Operator = ArithmeticOperator.Multiply,
                        Left = new FieldComputed { Key = "subtotal" },
                        Right = new FieldComputed { Key = "vatRate" },
                    },
                    Right = new ConstantComputed { Value = 100d },
                },
            },
            new NumericElement
            {
                Key = "total",
                Label = "Total",
                Unit = "£",
                ShowSpinner = false,
                Computed = new ArithmeticComputed
                {
                    Operator = ArithmeticOperator.Add,
                    Left = new FieldComputed { Key = "subtotal" },
                    Right = new FieldComputed { Key = "vat" },
                },
            },
            new SeparatorElement(),
            new TextBoxElement { Key = "orderedBy", Label = "Ordered by", DefaultValue = "Ada" },
            new TextBoxElement
            {
                Key = "summary",
                Label = "Summary",
                IsReadOnly = true,
                Computed = new FormatComputed
                {
                    Template = "{quantity} items for {orderedBy}, £{total} including VAT",
                },
            },
        },
    }.WithResolvedKeys();

    private static FormDefinition Validation() => new FormDefinition
    {
        Title = "Validation",
        Description = "Rules run as you type. Try submitting an empty form first.",
        Elements = new FormElement[]
        {
            new TextBoxElement
            {
                Key = "projectCode",
                Label = "Project code",
                Placeholder = "ABC-1234",
                HelpText = "Three letters, a dash, then four digits.",
                RequiredIf = ConstantCondition.True,
                Rules = new ValidationRule[]
                {
                    new RegexRule { Pattern = "^[A-Z]{3}-[0-9]{4}$", Message = "Use the form ABC-1234." },
                },
            },
            new TextBoxElement
            {
                Key = "email",
                Label = "Email",
                Rules = new ValidationRule[]
                {
                    new RegexRule { Pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$", Message = "That does not look like an email address." },
                },
            },
            new IntegerElement
            {
                Key = "levels",
                Label = "Levels",
                DefaultValue = 1,
                Rules = new ValidationRule[] { new RangeRule { Minimum = 1, Maximum = 200 } },
            },
            new DatePickerElement { Key = "start", Label = "Start", DefaultToToday = true },
            new DatePickerElement
            {
                Key = "end",
                Label = "End",
                DefaultToToday = true,
                HelpText = "Must be after the start date.",
                Rules = new ValidationRule[]
                {
                    new ComparisonRule
                    {
                        OtherKey = "start",
                        Operator = ComparisonOperator.GreaterThan,
                        Message = "The end date must be after the start date.",
                    },
                },
            },
            new FilePickerElement
            {
                Key = "template",
                Label = "Template",
                Rules = new ValidationRule[] { new FileExistsRule() },
            },
        },
    }.WithResolvedKeys();

    private static FormDefinition LongForm()
    {
        List<FormElement> fields = new()
        {
            new LabelElement { Text = "Fifty fields", HeadingLevel = 2 },
            new LabelElement { Text = "For checking that scrolling and layout hold up.", IsMuted = true },
        };

        for (int i = 1; i <= 50; i++)
        {
            fields.Add(new TextBoxElement
            {
                Key = "field" + i.ToString(System.Globalization.CultureInfo.InvariantCulture),
                Label = "Field " + i.ToString(System.Globalization.CultureInfo.InvariantCulture),
            });
        }

        return new FormDefinition
        {
            Title = "Long form",
            Elements = fields,
            Window = new WindowOptions { Width = 520, MaxHeight = 700 },
        }.WithResolvedKeys();
    }

    private static IReadOnlyList<OptionItem> Options(params string[] values)
        => values.Select(value => new OptionItem { Value = value, Display = value }).ToList();

    private static ConditionExpr Equals(string key, object value)
        => new ComparisonCondition { Key = key, Operator = ComparisonOperator.Equals, Operand = value };
}
