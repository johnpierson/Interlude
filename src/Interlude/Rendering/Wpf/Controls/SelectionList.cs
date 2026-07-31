using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Autodesk.DesignScript.Runtime;
using Interlude.Conditions;
using Interlude.Model;
using Interlude.Theming;

namespace Interlude.Rendering.Wpf.Controls;

/// <summary>
/// A list the user picks from, with an optional filter box and, when several answers are
/// allowed, tick boxes and select-all buttons.
///
/// Controls are built directly rather than through item templates and a view model. Forms hold
/// tens of options, not tens of thousands, so virtualisation buys nothing, and the direct
/// approach removes a whole layer — templates, converters, change notification — from something
/// that only ever has to show a list and remember what is ticked.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
internal sealed class SelectionList : Grid
{
    private readonly ListSelectionElement _element;
    private readonly List<CheckBox> _checkBoxes = new();
    private readonly ListBox? _singleList;
    private readonly StackPanel? _multiPanel;
    private readonly TextBox? _search;

    private bool _isWriting;

    internal SelectionList(ListSelectionElement element, RenderContext context)
    {
        _element = element;

        RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        if (element.ShowSearch && element.Options.Count > 0)
        {
            _search = new TextBox { Margin = new Thickness(0, 0, 0, 4) };
            FieldState.SetPlaceholder(_search, "Filter…");
            _search.TextChanged += (_, _) => ApplyFilter(_search!.Text);
            SetRow(_search, 0);
            Children.Add(_search);
        }

        // A rough row height keeps the list from collapsing to nothing or swallowing the form.
        double rowHeight = context.ControlHeight - 4d;
        double listHeight = Math.Max(2, element.VisibleRows) * rowHeight;

        if (element.AllowMultiple)
        {
            _multiPanel = new StackPanel { Orientation = Orientation.Vertical };

            foreach (OptionItem option in element.Options)
            {
                CheckBox box = new()
                {
                    Content = option.Display,
                    Tag = option,
                    IsEnabled = option.IsEnabled,
                    Margin = new Thickness(2),
                    ToolTip = option.Description,
                };

                box.Checked += OnCheckChanged;
                box.Unchecked += OnCheckChanged;

                _checkBoxes.Add(box);
                _multiPanel.Children.Add(box);
            }

            ScrollViewer scroller = new()
            {
                Content = _multiPanel,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                MaxHeight = listHeight,
                Padding = new Thickness(4),
            };
            scroller.SetResourceReference(StyleProperty, "Interlude.ListSurface");

            SetRow(scroller, 1);
            Children.Add(scroller);

            if (element.ShowSelectAll && element.Options.Count > 1)
            {
                StackPanel strip = BuildSelectAllStrip();
                SetRow(strip, 2);
                Children.Add(strip);
            }
        }
        else
        {
            _singleList = new ListBox { MaxHeight = listHeight };

            foreach (OptionItem option in element.Options)
            {
                _singleList.Items.Add(new ListBoxItem
                {
                    Content = option.Display,
                    Tag = option,
                    IsEnabled = option.IsEnabled,
                    ToolTip = option.Description,
                });
            }

            _singleList.SelectionChanged += (_, _) =>
            {
                if (!_isWriting)
                {
                    SelectionChanged?.Invoke(this, EventArgs.Empty);
                }
            };

            SetRow(_singleList, 1);
            Children.Add(_singleList);
        }
    }

    /// <summary>Raised when the user changes the selection.</summary>
    internal event EventHandler? SelectionChanged;

    /// <summary>
    /// The current answer: the chosen object for a single-select list, a list of objects for a
    /// multi-select one. Matching the element's own storage shape means the session never has to
    /// unwrap a one-item list.
    /// </summary>
    internal object? Read()
    {
        if (!_element.AllowMultiple)
        {
            return (_singleList?.SelectedItem as ListBoxItem)?.Tag is OptionItem option
                ? option.Value
                : null;
        }

        return _checkBoxes
            .Where(box => box.IsChecked == true)
            .Select(box => ((OptionItem)box.Tag).Value)
            .ToList();
    }

    /// <summary>Sets the selection without raising <see cref="SelectionChanged"/>.</summary>
    internal void Write(object? value)
    {
        _isWriting = true;
        try
        {
            if (!_element.AllowMultiple)
            {
                if (_singleList is null)
                {
                    return;
                }

                _singleList.SelectedItem = _singleList.Items
                    .OfType<ListBoxItem>()
                    .FirstOrDefault(item => ValueOps.AreEqual(((OptionItem)item.Tag).Value, value));
                return;
            }

            IReadOnlyList<object?> selected = ValueOps.AsList(value);
            foreach (CheckBox box in _checkBoxes)
            {
                object? optionValue = ((OptionItem)box.Tag).Value;
                box.IsChecked = selected.Any(item => ValueOps.AreEqual(item, optionValue));
            }
        }
        finally
        {
            _isWriting = false;
        }
    }

    private StackPanel BuildSelectAllStrip()
    {
        StackPanel strip = new()
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 4, 0, 0),
        };

        Button all = new() { Content = "Select all", Margin = new Thickness(0, 0, 6, 0) };
        Button none = new() { Content = "Select none" };

        all.SetResourceReference(StyleProperty, "Interlude.LinkButton");
        none.SetResourceReference(StyleProperty, "Interlude.LinkButton");

        all.Click += (_, _) => SetAll(true);
        none.Click += (_, _) => SetAll(false);

        strip.Children.Add(all);
        strip.Children.Add(none);
        return strip;
    }

    /// <summary>Applies to the filtered set only, so "select all" never ticks what is hidden.</summary>
    private void SetAll(bool isChecked)
    {
        _isWriting = true;
        try
        {
            foreach (CheckBox box in _checkBoxes)
            {
                if (box.Visibility == Visibility.Visible && box.IsEnabled)
                {
                    box.IsChecked = isChecked;
                }
            }
        }
        finally
        {
            _isWriting = false;
        }

        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnCheckChanged(object sender, RoutedEventArgs e)
    {
        if (!_isWriting)
        {
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void ApplyFilter(string filter)
    {
        bool showAll = string.IsNullOrWhiteSpace(filter);

        if (_element.AllowMultiple)
        {
            foreach (CheckBox box in _checkBoxes)
            {
                box.Visibility = showAll || Matches(((OptionItem)box.Tag).Display, filter)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }

            return;
        }

        foreach (ListBoxItem item in _singleList!.Items.OfType<ListBoxItem>())
        {
            item.Visibility = showAll || Matches(((OptionItem)item.Tag).Display, filter)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
    }

    private static bool Matches(string display, string filter)
        => display.IndexOf(filter, StringComparison.CurrentCultureIgnoreCase) >= 0;
}
