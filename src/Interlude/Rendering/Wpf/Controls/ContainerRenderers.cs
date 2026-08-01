using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Autodesk.DesignScript.Runtime;
using Interlude.Model;
using Interlude.Runtime;
using Interlude.Theming;

namespace Interlude.Rendering.Wpf.Controls;

/// <summary>Shared plumbing for containers: build children, no field chrome of their own.</summary>
[IsVisibleInDynamoLibrary(false)]
internal abstract class ContainerRenderer<TElement> : ControlRenderer<TElement>
    where TElement : ContainerElement
{
    public override bool UsesFieldChrome => false;

    /// <summary>Resolves a container's spacing, where a negative value means "ask the theme".</summary>
    protected static double ResolveSpacing(double declared, RenderContext context)
        => declared < 0d ? context.Spacing : declared;
}

/// <summary>Children stacked top to bottom.</summary>
[IsVisibleInDynamoLibrary(false)]
internal sealed class VStackRenderer : ContainerRenderer<VStackElement>
{
    protected override FrameworkElement BuildCore(VStackElement element, RenderContext context)
    {
        StackPanel panel = new() { Orientation = Orientation.Vertical };
        double spacing = ResolveSpacing(element.Spacing, context);

        foreach (FrameworkElement child in context.BuildChildren(element.Children))
        {
            // Spacing is applied as a bottom margin rather than by the panel, so a child that
            // gets hidden takes its own gap with it instead of leaving a hole.
            child.Margin = new Thickness(
                child.Margin.Left,
                child.Margin.Top,
                child.Margin.Right,
                Math.Max(child.Margin.Bottom, spacing));

            panel.Children.Add(child);
        }

        return panel;
    }
}

/// <summary>Children laid out left to right.</summary>
[IsVisibleInDynamoLibrary(false)]
internal sealed class HStackRenderer : ContainerRenderer<HStackElement>
{
    protected override FrameworkElement BuildCore(HStackElement element, RenderContext context)
    {
        double spacing = ResolveSpacing(element.Spacing, context);
        IReadOnlyList<FrameworkElement> children = context.BuildChildren(element.Children);

        if (element.Wrap)
        {
            WrapPanel wrap = new() { Orientation = Orientation.Horizontal };
            foreach (FrameworkElement child in children)
            {
                child.Margin = new Thickness(0, 0, spacing, spacing);
                wrap.Children.Add(child);
            }

            return wrap;
        }

        if (element.EqualWidths)
        {
            UniformGrid uniform = new() { Rows = 1, Columns = children.Count };
            foreach (FrameworkElement child in children)
            {
                child.Margin = new Thickness(0, 0, spacing, 0);
                uniform.Children.Add(child);
            }

            return uniform;
        }

        // A Grid rather than a horizontal StackPanel: a stack gives every child its desired
        // width, so a text box in a row would shrink to nothing instead of taking the slack.
        Grid row = new();
        for (int i = 0; i < children.Count; i++)
        {
            FrameworkElement child = children[i];

            row.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = child.Width > 0d && !double.IsNaN(child.Width)
                    ? GridLength.Auto
                    : new GridLength(1, GridUnitType.Star),
            });

            if (i < children.Count - 1)
            {
                child.Margin = new Thickness(
                    child.Margin.Left, child.Margin.Top, spacing, child.Margin.Bottom);
            }

            Grid.SetColumn(child, i);
            row.Children.Add(child);
        }

        return row;
    }
}

/// <summary>Children arranged in rows and columns.</summary>
[IsVisibleInDynamoLibrary(false)]
internal sealed class GridRenderer : ContainerRenderer<GridElement>
{
    protected override FrameworkElement BuildCore(GridElement element, RenderContext context)
    {
        Grid grid = new();

        IReadOnlyList<GridTrack> columns = element.Columns.Count > 0
            ? element.Columns
            : new[] { GridTrack.Star };

        foreach (GridTrack column in columns)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = column.ToWpf() });
        }

        double columnSpacing = ResolveSpacing(element.ColumnSpacing, context);
        double rowSpacing = ResolveSpacing(element.RowSpacing, context);

        IReadOnlyList<FrameworkElement> children = context.BuildChildren(element.Children);
        List<FormElement> models = element.Children.ToList();

        // Children that name their own cell keep it; the rest fill the grid in reading order,
        // skipping cells already spoken for.
        HashSet<(int Row, int Column)> taken = new();
        int cursor = 0;

        for (int i = 0; i < children.Count; i++)
        {
            FrameworkElement child = children[i];
            ElementStyle? style = i < models.Count ? models[i].Style : null;

            int row;
            int column;

            if (style?.GridRow is int explicitRow && style.GridColumn is int explicitColumn)
            {
                row = explicitRow;
                column = explicitColumn;
            }
            else
            {
                while (taken.Contains((cursor / columns.Count, cursor % columns.Count)))
                {
                    cursor++;
                }

                row = cursor / columns.Count;
                column = cursor % columns.Count;
                cursor++;
            }

            taken.Add((row, column));

            while (grid.RowDefinitions.Count <= row)
            {
                int index = grid.RowDefinitions.Count;
                grid.RowDefinitions.Add(new RowDefinition
                {
                    Height = index < element.Rows.Count ? element.Rows[index].ToWpf() : GridLength.Auto,
                });
            }

            Grid.SetRow(child, row);
            Grid.SetColumn(child, column);

            child.Margin = new Thickness(
                child.Margin.Left,
                child.Margin.Top,
                column < columns.Count - 1 ? columnSpacing : child.Margin.Right,
                Math.Max(child.Margin.Bottom, rowSpacing));

            grid.Children.Add(child);
        }

        return grid;
    }
}

/// <summary>A titled box.</summary>
[IsVisibleInDynamoLibrary(false)]
internal sealed class GroupBoxRenderer : ContainerRenderer<GroupBoxElement>
{
    protected override FrameworkElement BuildCore(GroupBoxElement element, RenderContext context)
    {
        StackPanel body = new();
        foreach (FrameworkElement child in context.BuildChildren(element.Children))
        {
            body.Children.Add(child);
        }

        return new GroupBox
        {
            Header = HeaderText.Build(element.Header, context),
            Content = body,
            Padding = new Thickness(context.Spacing),
            Margin = new Thickness(0, 0, 0, context.Spacing),
        };
    }
}

/// <summary>A tab strip.</summary>
[IsVisibleInDynamoLibrary(false)]
internal sealed class TabsRenderer : ContainerRenderer<TabsElement>
{
    protected override FrameworkElement BuildCore(TabsElement element, RenderContext context)
    {
        TabControl tabs = new()
        {
            TabStripPlacement = element.IsVertical ? Dock.Left : Dock.Top,
            Margin = new Thickness(0, 0, 0, context.Spacing),
        };

        int index = 0;
        foreach (FormElement child in element.Children)
        {
            index++;

            if (child is TabPageElement)
            {
                tabs.Items.Add(context.BuildChild(child));
                continue;
            }

            // A child that is not a page still deserves to be shown, so it gets a page of its
            // own rather than vanishing because the graph nested things one level differently.
            tabs.Items.Add(context.BuildChild(new TabPageElement
            {
                Header = string.IsNullOrWhiteSpace(child.Label) ? $"Page {index}" : child.Label!,
                Children = new[] { child },
            }));
        }

        if (element.SelectedIndex >= 0 && element.SelectedIndex < tabs.Items.Count)
        {
            tabs.SelectedIndex = element.SelectedIndex;
        }

        return tabs;
    }
}

/// <summary>One page of a tab strip.</summary>
[IsVisibleInDynamoLibrary(false)]
internal sealed class TabPageRenderer : ContainerRenderer<TabPageElement>
{
    protected override FrameworkElement BuildCore(TabPageElement element, RenderContext context)
    {
        StackPanel body = new() { Margin = new Thickness(context.Spacing) };
        foreach (FrameworkElement child in context.BuildChildren(element.Children))
        {
            body.Children.Add(child);
        }

        return new TabItem
        {
            Header = HeaderText.Build(element.Header, context),
            Content = new ScrollViewer
            {
                Content = body,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            },
        };
    }
}

/// <summary>A collapsible section.</summary>
[IsVisibleInDynamoLibrary(false)]
internal sealed class ExpanderRenderer : ContainerRenderer<ExpanderElement>
{
    protected override FrameworkElement BuildCore(ExpanderElement element, RenderContext context)
    {
        StackPanel body = new() { Margin = new Thickness(context.Spacing, context.Spacing, 0, 0) };
        foreach (FrameworkElement child in context.BuildChildren(element.Children))
        {
            body.Children.Add(child);
        }

        return new Expander
        {
            Header = HeaderText.Build(element.Header, context),
            IsExpanded = element.IsExpanded,
            Content = body,
            Margin = new Thickness(0, 0, 0, context.Spacing),
        };
    }
}

/// <summary>A raised panel.</summary>
[IsVisibleInDynamoLibrary(false)]
internal sealed class CardRenderer : ContainerRenderer<CardElement>
{
    protected override FrameworkElement BuildCore(CardElement element, RenderContext context)
    {
        StackPanel body = new();

        if (!string.IsNullOrWhiteSpace(element.Header))
        {
            TextBlock header = new()
            {
                FontSize = context.Theme.FontSize * 1.1d,
                TextWrapping = TextWrapping.Wrap,
            };
            header.SetResourceReference(TextBlock.FontWeightProperty, ThemeKeys.HeadingFontWeight);
            header.SetResourceReference(TextBlock.ForegroundProperty, ThemeKeys.Foreground);
            HeaderText.Apply(header, element.Header, context);
            body.Children.Add(header);
        }

        if (!string.IsNullOrWhiteSpace(element.Subheader))
        {
            TextBlock sub = new()
            {
                Text = element.Subheader,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 2, 0, context.Spacing / 2d),
            };
            sub.SetResourceReference(TextBlock.ForegroundProperty, ThemeKeys.ForegroundMuted);
            sub.SetResourceReference(TextBlock.FontSizeProperty, ThemeKeys.FontSizeSmall);
            body.Children.Add(sub);
        }

        if (body.Children.Count > 0)
        {
            body.Children.Add(new Border { Height = context.Spacing / 2d });
        }

        foreach (FrameworkElement child in context.BuildChildren(element.Children))
        {
            body.Children.Add(child);
        }

        Border card = new()
        {
            Child = body,
            Padding = new Thickness(context.Spacing * 1.5d),
            Margin = new Thickness(0, 0, 0, context.Spacing),
        };

        card.SetResourceReference(Border.BorderThicknessProperty, ThemeKeys.BorderThickness);
        card.SetResourceReference(Border.BackgroundProperty, ThemeKeys.Surface);
        card.SetResourceReference(Border.BorderBrushProperty, ThemeKeys.Border);
        card.SetResourceReference(FrameworkElement.StyleProperty,
            element.HasShadow ? "Interlude.CardWithShadow" : "Interlude.Card");

        return card;
    }
}

/// <summary>A scrolling region.</summary>
[IsVisibleInDynamoLibrary(false)]
internal sealed class ScrollRenderer : ContainerRenderer<ScrollElement>
{
    protected override FrameworkElement BuildCore(ScrollElement element, RenderContext context)
    {
        StackPanel body = new();
        double spacing = ResolveSpacing(element.Spacing, context);

        foreach (FrameworkElement child in context.BuildChildren(element.Children))
        {
            child.Margin = new Thickness(
                child.Margin.Left,
                child.Margin.Top,
                child.Margin.Right,
                Math.Max(child.Margin.Bottom, spacing));

            body.Children.Add(child);
        }

        return new ScrollViewer
        {
            Content = body,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = element.AllowHorizontal
                ? ScrollBarVisibility.Auto
                : ScrollBarVisibility.Disabled,
        };
    }
}

/// <summary>Children docked to the edges.</summary>
[IsVisibleInDynamoLibrary(false)]
internal sealed class DockRenderer : ContainerRenderer<DockElement>
{
    protected override FrameworkElement BuildCore(DockElement element, RenderContext context)
    {
        DockPanel dock = new() { LastChildFill = element.LastChildFills };

        foreach (FrameworkElement child in context.BuildChildren(element.Children))
        {
            dock.Children.Add(child);
        }

        return dock;
    }
}

/// <summary>Two panes with a draggable splitter.</summary>
[IsVisibleInDynamoLibrary(false)]
internal sealed class SplitViewRenderer : ContainerRenderer<SplitViewElement>
{
    private const double SplitterThickness = 6d;

    protected override FrameworkElement BuildCore(SplitViewElement element, RenderContext context)
    {
        IReadOnlyList<FrameworkElement> panes = context.BuildChildren(element.Children.Take(2));

        FrameworkElement first = panes.Count > 0 ? panes[0] : new Border();
        FrameworkElement second = panes.Count > 1 ? panes[1] : new Border();

        double share = Math.Min(0.95d, Math.Max(0.05d, element.SplitterPosition));
        bool horizontal = element.Orientation == LayoutOrientation.Horizontal;

        Grid grid = new();
        GridSplitter splitter = new();

        if (horizontal)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(share, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(SplitterThickness) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1d - share, GridUnitType.Star) });

            Grid.SetColumn(first, 0);
            Grid.SetColumn(splitter, 1);
            Grid.SetColumn(second, 2);

            splitter.HorizontalAlignment = HorizontalAlignment.Stretch;
            splitter.VerticalAlignment = VerticalAlignment.Stretch;
        }
        else
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(share, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(SplitterThickness) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1d - share, GridUnitType.Star) });

            Grid.SetRow(first, 0);
            Grid.SetRow(splitter, 1);
            Grid.SetRow(second, 2);

            splitter.HorizontalAlignment = HorizontalAlignment.Stretch;
            splitter.VerticalAlignment = VerticalAlignment.Stretch;
        }

        splitter.SetResourceReference(FrameworkElement.StyleProperty, "Interlude.Splitter");

        grid.Children.Add(first);
        grid.Children.Add(splitter);
        grid.Children.Add(second);
        return grid;
    }
}
