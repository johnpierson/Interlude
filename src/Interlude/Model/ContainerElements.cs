using System;
using System.Collections.Generic;
using Autodesk.DesignScript.Runtime;

namespace Interlude.Model;

/// <summary>Stacks its children top to bottom.</summary>
[IsVisibleInDynamoLibrary(false)]
public sealed record VStackElement : ContainerElement
{
    /// <summary>Gap between children. Negative means "use the theme's spacing".</summary>
    public double Spacing { get; init; } = -1d;
}

/// <summary>Lays its children out left to right.</summary>
[IsVisibleInDynamoLibrary(false)]
public sealed record HStackElement : ContainerElement
{
    /// <summary>Gap between children. Negative means "use the theme's spacing".</summary>
    public double Spacing { get; init; } = -1d;

    /// <summary>Gives every child the same width instead of sizing each to its content.</summary>
    public bool EqualWidths { get; init; }

    /// <summary>Wraps onto a new line when the row runs out of width.</summary>
    public bool Wrap { get; init; }
}

/// <summary>
/// Arranges children in rows and columns. Children choose their cell through
/// <see cref="ElementStyle.GridRow"/> and <see cref="ElementStyle.GridColumn"/>; children that
/// do not are filled in reading order.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
public sealed record GridElement : ContainerElement
{
    public IReadOnlyList<GridTrack> Columns { get; init; } = new[] { GridTrack.Star };

    /// <summary>Rows are added automatically as children need them when this is empty.</summary>
    public IReadOnlyList<GridTrack> Rows { get; init; } = Array.Empty<GridTrack>();

    /// <summary>Gap between columns. Negative means "use the theme's spacing".</summary>
    public double ColumnSpacing { get; init; } = -1d;

    /// <summary>Gap between rows. Negative means "use the theme's spacing".</summary>
    public double RowSpacing { get; init; } = -1d;
}

/// <summary>A titled box drawn around its children.</summary>
[IsVisibleInDynamoLibrary(false)]
public sealed record GroupBoxElement : ContainerElement
{
    public string Header { get; init; } = string.Empty;
}

/// <summary>
/// A tab strip. Children are expected to be <see cref="TabPageElement"/>; anything else is
/// wrapped into a page of its own so a mistyped graph still shows a usable form.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
public sealed record TabsElement : ContainerElement
{
    /// <summary>Index of the tab shown first.</summary>
    public int SelectedIndex { get; init; }

    /// <summary>Tabs run down the left edge instead of across the top.</summary>
    public bool IsVertical { get; init; }
}

/// <summary>One page of a <see cref="TabsElement"/>.</summary>
[IsVisibleInDynamoLibrary(false)]
public sealed record TabPageElement : ContainerElement
{
    public string Header { get; init; } = string.Empty;

    public string? IconPath { get; init; }
}

/// <summary>A section the user can collapse.</summary>
[IsVisibleInDynamoLibrary(false)]
public sealed record ExpanderElement : ContainerElement
{
    public string Header { get; init; } = string.Empty;

    public bool IsExpanded { get; init; } = true;
}

/// <summary>A raised panel, optionally with a heading and a footer strip.</summary>
[IsVisibleInDynamoLibrary(false)]
public sealed record CardElement : ContainerElement
{
    public string? Header { get; init; }

    public string? Subheader { get; init; }

    /// <summary>Draws a drop shadow to lift the card off the background.</summary>
    public bool HasShadow { get; init; } = true;
}

/// <summary>Scrolls its children when they do not fit.</summary>
[IsVisibleInDynamoLibrary(false)]
public sealed record ScrollElement : ContainerElement
{
    public bool AllowHorizontal { get; init; }

    /// <summary>Gap between children. Negative means "use the theme's spacing".</summary>
    public double Spacing { get; init; } = -1d;
}

/// <summary>
/// Docks children to the edges of the available space. Each child picks its edge through
/// <see cref="ElementStyle.Dock"/>; the last child fills what is left.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
public sealed record DockElement : ContainerElement
{
    public bool LastChildFills { get; init; } = true;
}

/// <summary>
/// Two panes separated by a splitter the user can drag. Exactly two children are expected;
/// extra children are ignored and a missing second pane renders empty.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
public sealed record SplitViewElement : ContainerElement
{
    public LayoutOrientation Orientation { get; init; } = LayoutOrientation.Horizontal;

    /// <summary>Share of the space given to the first pane, from 0 to 1.</summary>
    public double SplitterPosition { get; init; } = 0.5d;
}
