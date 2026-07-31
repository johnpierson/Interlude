using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Autodesk.DesignScript.Runtime;
using Interlude.Conditions;
using Interlude.Model;
using Interlude.Runtime;
using Interlude.Theming;

namespace Interlude.Rendering.Wpf.Controls;

/// <summary>Single or multi-line text.</summary>
[IsVisibleInDynamoLibrary(false)]
internal sealed class TextBoxRenderer : ControlRenderer<TextBoxElement>
{
    protected override FrameworkElement BuildCore(TextBoxElement element, RenderContext context)
    {
        TextBox box = new()
        {
            AcceptsReturn = element.IsMultiline,
            TextWrapping = element.WrapText ? TextWrapping.Wrap : TextWrapping.NoWrap,
            IsReadOnly = element.IsReadOnly || element.Computed is not null,
            VerticalContentAlignment = element.IsMultiline ? VerticalAlignment.Top : VerticalAlignment.Center,
        };

        if (element.MaxLength is > 0)
        {
            box.MaxLength = element.MaxLength.Value;
        }

        if (element.IsMultiline)
        {
            box.MinHeight = Math.Max(2, element.Lines) * 18d;
            box.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        }

        if (!string.IsNullOrEmpty(element.Placeholder))
        {
            FieldState.SetPlaceholder(box, element.Placeholder!);
        }

        box.TextChanged += (_, _) => context.ReportValue(element, box.Text);
        return box;
    }

    public override object? ReadValue(FrameworkElement control) => ((TextBox)control).Text;

    public override void WriteValue(FrameworkElement control, object? value)
    {
        TextBox box = (TextBox)control;
        string text = ValueOps.ToStringInvariant(value);

        // Assigning Text resets the caret, so only do it when the text really differs.
        if (!string.Equals(box.Text, text, StringComparison.Ordinal))
        {
            box.Text = text;
        }
    }
}

/// <summary>A masked field.</summary>
[IsVisibleInDynamoLibrary(false)]
internal sealed class PasswordRenderer : ControlRenderer<PasswordElement>
{
    protected override FrameworkElement BuildCore(PasswordElement element, RenderContext context)
    {
        PasswordBox box = new();

        if (element.MaxLength is > 0)
        {
            box.MaxLength = element.MaxLength.Value;
        }

        box.PasswordChanged += (_, _) => context.ReportValue(element, box.Password);
        return box;
    }

    public override object? ReadValue(FrameworkElement control) => ((PasswordBox)control).Password;

    public override void WriteValue(FrameworkElement control, object? value)
    {
        PasswordBox box = (PasswordBox)control;
        string text = ValueOps.ToStringInvariant(value);

        if (!string.Equals(box.Password, text, StringComparison.Ordinal))
        {
            box.Password = text;
        }
    }
}

/// <summary>A decimal number.</summary>
[IsVisibleInDynamoLibrary(false)]
internal sealed class NumericRenderer : ControlRenderer<NumericElement>
{
    protected override FrameworkElement BuildCore(NumericElement element, RenderContext context)
    {
        NumericBox box = new(
            element.Minimum,
            element.Maximum,
            element.Increment,
            element.DecimalPlaces,
            element.Unit,
            element.ShowSpinner && element.Computed is null,
            isInteger: false);

        box.Entry.IsReadOnly = element.IsReadOnly || element.Computed is not null;
        box.SetResourceReference(FrameworkElement.StyleProperty, "Interlude.FieldSurface");
        box.ValueChanged += (_, _) => context.ReportValue(element, box.Value);
        return box;
    }

    public override object? ReadValue(FrameworkElement control) => ((NumericBox)control).Value;

    public override void WriteValue(FrameworkElement control, object? value)
        => ((NumericBox)control).Write(ValueOps.ToDouble(value));
}

/// <summary>A whole number.</summary>
[IsVisibleInDynamoLibrary(false)]
internal sealed class IntegerRenderer : ControlRenderer<IntegerElement>
{
    protected override FrameworkElement BuildCore(IntegerElement element, RenderContext context)
    {
        NumericBox box = new(
            element.Minimum,
            element.Maximum,
            element.Increment,
            decimals: 0,
            element.Unit,
            element.ShowSpinner && element.Computed is null,
            isInteger: true);

        box.Entry.IsReadOnly = element.IsReadOnly || element.Computed is not null;
        box.SetResourceReference(FrameworkElement.StyleProperty, "Interlude.FieldSurface");
        box.ValueChanged += (_, _) => context.ReportValue(element, (int)Math.Round(box.Value));
        return box;
    }

    public override object? ReadValue(FrameworkElement control)
        => (int)Math.Round(((NumericBox)control).Value);

    public override void WriteValue(FrameworkElement control, object? value)
        => ((NumericBox)control).Write(ValueOps.ToDouble(value));
}

/// <summary>A number chosen by dragging.</summary>
[IsVisibleInDynamoLibrary(false)]
internal sealed class SliderRenderer : ControlRenderer<SliderElement>
{
    protected override FrameworkElement BuildCore(SliderElement element, RenderContext context)
    {
        Grid layout = new();
        layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        layout.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        Slider slider = new()
        {
            Minimum = Math.Min(element.Minimum, element.Maximum),
            Maximum = Math.Max(element.Minimum, element.Maximum),
            SmallChange = element.Step > 0d ? element.Step : 1d,
            LargeChange = element.Step > 0d ? element.Step * 10d : 10d,
            IsSnapToTickEnabled = element.Step > 0d,
            TickFrequency = element.Step > 0d ? element.Step : 1d,
            TickPlacement = element.ShowTicks ? TickPlacement.BottomRight : TickPlacement.None,
            VerticalAlignment = VerticalAlignment.Center,
        };

        Grid.SetColumn(slider, 0);
        layout.Children.Add(slider);

        TextBlock? readout = null;
        if (element.ShowValue)
        {
            readout = new TextBlock
            {
                MinWidth = 44,
                Margin = new Thickness(8, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Right,
            };
            readout.SetResourceReference(TextBlock.ForegroundProperty, ThemeKeys.ForegroundMuted);

            Grid.SetColumn(readout, 1);
            layout.Children.Add(readout);
        }

        // The readout is the one place a slider needs the user's culture: 0,5 on a German machine.
        string format = "F" + Math.Max(0, element.DecimalPlaces).ToString(CultureInfo.InvariantCulture);

        slider.ValueChanged += (_, _) =>
        {
            if (readout is not null)
            {
                readout.Text = slider.Value.ToString(format, CultureInfo.CurrentCulture);
            }

            context.ReportValue(element, slider.Value);
        };

        layout.Tag = slider;
        return layout;
    }

    public override void ApplyState(FrameworkElement control, ElementRuntimeState state)
        => control.IsEnabled = state.IsEnabled;

    public override object? ReadValue(FrameworkElement control) => Inner(control).Value;

    public override void WriteValue(FrameworkElement control, object? value)
        => Inner(control).Value = ValueOps.ToDouble(value);

    private static Slider Inner(FrameworkElement control) => (Slider)((Grid)control).Tag;
}

/// <summary>A drop-down list.</summary>
[IsVisibleInDynamoLibrary(false)]
internal sealed class DropdownRenderer : ControlRenderer<DropdownElement>
{
    protected override FrameworkElement BuildCore(DropdownElement element, RenderContext context)
    {
        ComboBox combo = new()
        {
            IsEditable = element.IsEditable,
            IsTextSearchEnabled = true,
            VerticalContentAlignment = VerticalAlignment.Center,
        };

        if (!string.IsNullOrEmpty(element.Placeholder))
        {
            FieldState.SetPlaceholder(combo, element.Placeholder!);
        }

        foreach (OptionItem option in element.Options)
        {
            combo.Items.Add(new ComboBoxItem
            {
                Content = option.Display,
                Tag = option,
                IsEnabled = option.IsEnabled,
                ToolTip = option.Description,
            });
        }

        combo.SelectionChanged += (_, _) =>
        {
            if (combo.SelectedItem is ComboBoxItem { Tag: OptionItem selected })
            {
                context.ReportValue(element, selected.Value);
            }
        };

        if (element.IsEditable)
        {
            // An editable drop-down is half text box, so typed text is a real answer too.
            combo.LostFocus += (_, _) =>
            {
                if (combo.SelectedItem is null)
                {
                    context.ReportValue(element, combo.Text);
                }
            };
        }

        return combo;
    }

    public override object? ReadValue(FrameworkElement control)
    {
        ComboBox combo = (ComboBox)control;
        return combo.SelectedItem is ComboBoxItem { Tag: OptionItem option } ? option.Value : combo.Text;
    }

    public override void WriteValue(FrameworkElement control, object? value)
    {
        ComboBox combo = (ComboBox)control;

        ComboBoxItem? match = combo.Items
            .OfType<ComboBoxItem>()
            .FirstOrDefault(item => item.Tag is OptionItem option && ValueOps.AreEqual(option.Value, value));

        combo.SelectedItem = match;

        if (match is null && combo.IsEditable)
        {
            combo.Text = ValueOps.ToStringInvariant(value);
        }
    }
}

/// <summary>Mutually exclusive radio buttons.</summary>
[IsVisibleInDynamoLibrary(false)]
internal sealed class RadioGroupRenderer : ControlRenderer<RadioGroupElement>
{
    protected override FrameworkElement BuildCore(RadioGroupElement element, RenderContext context)
    {
        // Every group needs its own name, or two radio groups in one window fight each other.
        string groupName = "Interlude_" + (string.IsNullOrEmpty(element.Key)
            ? Guid.NewGuid().ToString("N")
            : element.Key);

        Panel host = element.Columns > 1
            ? new UniformGrid { Columns = element.Columns }
            : new StackPanel { Orientation = element.Orientation.ToWpf() };

        foreach (OptionItem option in element.Options)
        {
            RadioButton button = new()
            {
                Content = option.Display,
                Tag = option,
                GroupName = groupName,
                IsEnabled = option.IsEnabled,
                Margin = element.Orientation == LayoutOrientation.Horizontal
                    ? new Thickness(0, 2, 12, 2)
                    : new Thickness(0, 2, 0, 2),
                ToolTip = option.Description,
            };

            button.Checked += (_, _) => context.ReportValue(element, option.Value);
            host.Children.Add(button);
        }

        return host;
    }

    public override object? ReadValue(FrameworkElement control)
        => Buttons(control).FirstOrDefault(button => button.IsChecked == true)?.Tag is OptionItem option
            ? option.Value
            : null;

    public override void WriteValue(FrameworkElement control, object? value)
    {
        foreach (RadioButton button in Buttons(control))
        {
            bool shouldBeChecked = button.Tag is OptionItem option && ValueOps.AreEqual(option.Value, value);

            if (button.IsChecked != shouldBeChecked)
            {
                button.IsChecked = shouldBeChecked;
            }
        }
    }

    private static IEnumerable<RadioButton> Buttons(FrameworkElement control)
        => ((Panel)control).Children.OfType<RadioButton>();
}

/// <summary>A tick box.</summary>
[IsVisibleInDynamoLibrary(false)]
internal sealed class CheckBoxRenderer : ControlRenderer<CheckBoxElement>
{
    protected override FrameworkElement BuildCore(CheckBoxElement element, RenderContext context)
    {
        CheckBox box = new()
        {
            Content = element.Content,
            VerticalAlignment = VerticalAlignment.Center,
            MinHeight = context.ControlHeight,
        };

        box.Checked += (_, _) => context.ReportValue(element, true);
        box.Unchecked += (_, _) => context.ReportValue(element, false);
        return box;
    }

    public override object? ReadValue(FrameworkElement control) => ((CheckBox)control).IsChecked == true;

    public override void WriteValue(FrameworkElement control, object? value)
    {
        CheckBox box = (CheckBox)control;
        bool next = ValueOps.ToBool(value);

        if (box.IsChecked != next)
        {
            box.IsChecked = next;
        }
    }
}

/// <summary>An on/off switch.</summary>
[IsVisibleInDynamoLibrary(false)]
internal sealed class ToggleRenderer : ControlRenderer<ToggleElement>
{
    protected override FrameworkElement BuildCore(ToggleElement element, RenderContext context)
    {
        StackPanel host = new()
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
            MinHeight = context.ControlHeight,
        };

        ToggleButton toggle = new() { VerticalAlignment = VerticalAlignment.Center };
        toggle.SetResourceReference(FrameworkElement.StyleProperty, "Interlude.ToggleSwitch");

        TextBlock caption = new()
        {
            Margin = new Thickness(8, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
        };
        caption.SetResourceReference(TextBlock.ForegroundProperty, ThemeKeys.ForegroundMuted);

        void UpdateCaption()
            => caption.Text = toggle.IsChecked == true ? element.OnText ?? string.Empty : element.OffText ?? string.Empty;

        toggle.Checked += (_, _) =>
        {
            UpdateCaption();
            context.ReportValue(element, true);
        };

        toggle.Unchecked += (_, _) =>
        {
            UpdateCaption();
            context.ReportValue(element, false);
        };

        UpdateCaption();

        host.Children.Add(toggle);
        host.Children.Add(caption);
        host.Tag = toggle;
        return host;
    }

    public override object? ReadValue(FrameworkElement control) => Inner(control).IsChecked == true;

    public override void WriteValue(FrameworkElement control, object? value)
    {
        ToggleButton toggle = Inner(control);
        bool next = ValueOps.ToBool(value);

        if (toggle.IsChecked != next)
        {
            toggle.IsChecked = next;
        }
    }

    private static ToggleButton Inner(FrameworkElement control) => (ToggleButton)((StackPanel)control).Tag;
}

/// <summary>A list, single or multi select.</summary>
[IsVisibleInDynamoLibrary(false)]
internal sealed class ListSelectionRenderer : ControlRenderer<ListSelectionElement>
{
    protected override FrameworkElement BuildCore(ListSelectionElement element, RenderContext context)
    {
        SelectionList list = new(element, context);
        list.SelectionChanged += (_, _) => context.ReportValue(element, list.Read());
        return list;
    }

    public override object? ReadValue(FrameworkElement control) => ((SelectionList)control).Read();

    public override void WriteValue(FrameworkElement control, object? value)
        => ((SelectionList)control).Write(value);
}

/// <summary>A hierarchy, single or multi select.</summary>
[IsVisibleInDynamoLibrary(false)]
internal sealed class TreeSelectionRenderer : ControlRenderer<TreeSelectionElement>
{
    protected override FrameworkElement BuildCore(TreeSelectionElement element, RenderContext context)
    {
        SelectionTree tree = new(element, context);
        tree.SelectionChanged += (_, _) => context.ReportValue(element, tree.Read());
        return tree;
    }

    public override object? ReadValue(FrameworkElement control) => ((SelectionTree)control).Read();

    public override void WriteValue(FrameworkElement control, object? value)
        => ((SelectionTree)control).Write(value);
}

/// <summary>A calendar field.</summary>
[IsVisibleInDynamoLibrary(false)]
internal sealed class DatePickerRenderer : ControlRenderer<DatePickerElement>
{
    protected override FrameworkElement BuildCore(DatePickerElement element, RenderContext context)
    {
        DateTimeField field = new(element);
        field.DateChanged += (_, _) => context.ReportValue(element, field.Read());
        return field;
    }

    public override object? ReadValue(FrameworkElement control) => ((DateTimeField)control).Read();

    public override void WriteValue(FrameworkElement control, object? value)
        => ((DateTimeField)control).Write(ValueOps.TryToDateTime(value, out DateTime date) ? date : null);
}

/// <summary>A colour field.</summary>
[IsVisibleInDynamoLibrary(false)]
internal sealed class ColorPickerRenderer : ControlRenderer<ColorPickerElement>
{
    protected override FrameworkElement BuildCore(ColorPickerElement element, RenderContext context)
    {
        ColorField field = new(element);
        field.ColorChanged += (_, _) => context.ReportValue(element, field.Value);
        return field;
    }

    public override object? ReadValue(FrameworkElement control) => ((ColorField)control).Value;

    public override void WriteValue(FrameworkElement control, object? value)
    {
        ColorField field = (ColorField)control;

        RgbColor color = value switch
        {
            RgbColor rgb => rgb,
            string text when RgbColor.TryParse(text, out RgbColor parsed) => parsed,
            _ => RgbColor.Black,
        };

        field.Write(color);
    }
}

/// <summary>A file path with a Browse button.</summary>
[IsVisibleInDynamoLibrary(false)]
internal sealed class FilePickerRenderer : ControlRenderer<FilePickerElement>
{
    protected override FrameworkElement BuildCore(FilePickerElement element, RenderContext context)
    {
        PathBox box = new(element, element.InitialDirectory, isFolder: false);

        box.PathChanged += (_, _) => context.ReportValue(
            element,
            element.AllowMultiple ? box.Paths.Cast<object?>().ToList() : box.Text);

        return box;
    }

    public override object? ReadValue(FrameworkElement control) => ((PathBox)control).Text;

    public override void WriteValue(FrameworkElement control, object? value)
    {
        PathBox box = (PathBox)control;

        if (ValueOps.TryAsSequence(value, out IReadOnlyList<object?> paths))
        {
            box.SetPaths(paths.Select(ValueOps.ToStringInvariant));
            return;
        }

        box.Text = ValueOps.ToStringInvariant(value);
    }
}

/// <summary>A folder path with a Browse button.</summary>
[IsVisibleInDynamoLibrary(false)]
internal sealed class FolderPickerRenderer : ControlRenderer<FolderPickerElement>
{
    protected override FrameworkElement BuildCore(FolderPickerElement element, RenderContext context)
    {
        PathBox box = new(null, element.InitialDirectory, isFolder: true);
        box.PathChanged += (_, _) => context.ReportValue(element, box.Text);
        return box;
    }

    public override object? ReadValue(FrameworkElement control) => ((PathBox)control).Text;

    public override void WriteValue(FrameworkElement control, object? value)
        => ((PathBox)control).Text = ValueOps.ToStringInvariant(value);
}
