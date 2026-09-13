using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Autodesk.DesignScript.Runtime;
using Interlude.Conditions;
using Interlude.Model;

namespace Interlude.Rendering.Wpf.Controls;

/// <summary>
/// A hierarchy the user picks from, with optional tick boxes and a filter.
///
/// Like <see cref="SelectionList"/>, the tree is built as real controls rather than through a
/// hierarchical template over a view model. Filtering keeps a node visible when it matches or
/// when any descendant does, which is the only behaviour that makes searching a tree useful:
/// hiding a matching leaf's parents would hide the match.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
internal sealed class SelectionTree : Grid
{
    private readonly TreeSelectionElement _element;
    private readonly TreeView _tree;
    private readonly List<Entry> _entries = new();

    private bool _isWriting;

    internal SelectionTree(TreeSelectionElement element, RenderContext context)
    {
        _element = element;

        RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        if (element.ShowSearch && element.Roots.Count > 0)
        {
            TextBox search = new() { Margin = new Thickness(0, 0, 0, 4) };
            FieldState.SetPlaceholder(search, "Filter…");
            search.TextChanged += (_, _) => ApplyFilter(search.Text);
            SetRow(search, 0);
            Children.Add(search);
        }

        _tree = new TreeView { MaxHeight = 240d, BorderThickness = new Thickness(1) };

        foreach (TreeNode root in element.Roots)
        {
            _tree.Items.Add(BuildItem(root));
        }

        if (!element.AllowMultiple)
        {
            _tree.SelectedItemChanged += (_, _) =>
            {
                if (!_isWriting)
                {
                    SelectionChanged?.Invoke(this, EventArgs.Empty);
                }
            };
        }

        SetRow(_tree, 1);
        Children.Add(_tree);
    }

    /// <summary>Raised when the user changes the selection.</summary>
    internal event EventHandler? SelectionChanged;

    /// <summary>Whether tick boxes are shown, which follows from allowing several answers.</summary>
    private bool UsesCheckBoxes => _element.AllowMultiple || _element.ShowCheckBoxes;

    /// <summary>The chosen object, or the list of chosen objects for a multi-select tree.</summary>
    internal object? Read()
    {
        if (!_element.AllowMultiple)
        {
            if (UsesCheckBoxes)
            {
                Entry? ticked = _entries.FirstOrDefault(entry => entry.CheckBox?.IsChecked == true);
                return ticked?.Node.Value;
            }

            return (_tree.SelectedItem as TreeViewItem)?.Tag is TreeNode node && node.IsSelectable
                ? node.Value
                : null;
        }

        return _entries
            .Where(entry => entry.CheckBox?.IsChecked == true && entry.Node.IsSelectable)
            .Select(entry => entry.Node.Value)
            .ToList();
    }

    /// <summary>Sets the selection without raising <see cref="SelectionChanged"/>.</summary>
    internal void Write(object? value)
    {
        _isWriting = true;
        try
        {
            IReadOnlyList<object?> selected = ValueOps.AsList(value);

            if (UsesCheckBoxes)
            {
                foreach (Entry entry in _entries)
                {
                    if (entry.CheckBox is not null)
                    {
                        entry.CheckBox.IsChecked =
                            selected.Any(item => ValueOps.AreStateEqual(item, entry.Node.Value));
                    }
                }

                return;
            }

            // Clear first so null and unmatched values cannot leave the previous answer selected.
            foreach (Entry entry in _entries)
            {
                entry.Item.IsSelected = false;
            }

            foreach (Entry entry in _entries)
            {
                if (selected.Any(item => ValueOps.AreStateEqual(item, entry.Node.Value)))
                {
                    entry.Item.IsSelected = true;
                    ExpandAncestors(entry.Item);
                    return;
                }
            }
        }
        finally
        {
            _isWriting = false;
        }
    }

    private TreeViewItem BuildItem(TreeNode node)
    {
        TreeViewItem item = new()
        {
            Tag = node,
            IsExpanded = node.IsExpanded || _element.ExpandAll,
            IsEnabled = node.IsEnabled,
        };

        CheckBox? checkBox = null;

        if (UsesCheckBoxes && node.IsSelectable)
        {
            checkBox = new CheckBox { Content = node.Display, Margin = new Thickness(0, 1, 0, 1) };
            checkBox.Checked += OnCheckChanged;
            checkBox.Unchecked += OnCheckChanged;
            item.Header = checkBox;
        }
        else
        {
            item.Header = new TextBlock { Text = node.Display, Margin = new Thickness(0, 2, 0, 2) };
        }

        _entries.Add(new Entry(node, item, checkBox));

        foreach (TreeNode child in node.Children)
        {
            item.Items.Add(BuildItem(child));
        }

        return item;
    }

    private void OnCheckChanged(object sender, RoutedEventArgs e)
    {
        if (_isWriting)
        {
            return;
        }

        if (sender is CheckBox box && _element.CheckChildrenWithParent && _element.AllowMultiple)
        {
            Entry? entry = _entries.FirstOrDefault(candidate => ReferenceEquals(candidate.CheckBox, box));
            if (entry is not null)
            {
                CascadeToDescendants(entry, box.IsChecked == true);
            }
        }
        else if (sender is CheckBox single && !_element.AllowMultiple && single.IsChecked == true)
        {
            // A single-answer tree with tick boxes behaves like a radio group: ticking one
            // clears the rest, rather than silently keeping only the first.
            _isWriting = true;
            try
            {
                foreach (Entry other in _entries.Where(entry => !ReferenceEquals(entry.CheckBox, single)))
                {
                    if (other.CheckBox is not null)
                    {
                        other.CheckBox.IsChecked = false;
                    }
                }
            }
            finally
            {
                _isWriting = false;
            }
        }

        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    private void CascadeToDescendants(Entry entry, bool isChecked)
    {
        _isWriting = true;
        try
        {
            foreach (TreeNode descendant in entry.Node.Descend().Skip(1))
            {
                Entry? match = _entries.FirstOrDefault(candidate => ReferenceEquals(candidate.Node, descendant));
                if (match?.CheckBox is not null && match.CheckBox.IsEnabled)
                {
                    match.CheckBox.IsChecked = isChecked;
                }
            }
        }
        finally
        {
            _isWriting = false;
        }
    }

    private void ApplyFilter(string filter)
    {
        bool showAll = string.IsNullOrWhiteSpace(filter);

        foreach (Entry entry in _entries)
        {
            bool matches = showAll || entry.Node.Descend()
                .Any(node => node.Display.IndexOf(filter, StringComparison.CurrentCultureIgnoreCase) >= 0);

            entry.Item.Visibility = matches ? Visibility.Visible : Visibility.Collapsed;

            if (matches && !showAll)
            {
                entry.Item.IsExpanded = true;
            }
        }
    }

    private static void ExpandAncestors(TreeViewItem item)
    {
        for (DependencyObject? parent = ItemsControl.ItemsControlFromItemContainer(item);
             parent is TreeViewItem ancestor;
             parent = ItemsControl.ItemsControlFromItemContainer(ancestor))
        {
            ancestor.IsExpanded = true;
        }
    }

    private sealed class Entry
    {
        internal Entry(TreeNode node, TreeViewItem item, CheckBox? checkBox)
        {
            Node = node;
            Item = item;
            CheckBox = checkBox;
        }

        internal TreeNode Node { get; }

        internal TreeViewItem Item { get; }

        internal CheckBox? CheckBox { get; }
    }
}
