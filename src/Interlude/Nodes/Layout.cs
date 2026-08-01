using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.DesignScript.Runtime;
using Interlude.Model;

namespace Interlude;

/// <summary>
/// Arranging and decorating a form: sections, rows, grids, tabs, and the elements that show
/// something rather than ask something.
///
/// Every container takes a list of elements. None of them has a single-element overload, on
/// purpose: with both available, a graph that passes one element to a list port gets replication
/// instead of a container, and produces N containers of one child each rather than one container
/// of N. Pass a list, even a list of one.
/// </summary>
public class Layout
{
    private Layout()
    {
    }

    /// <summary>
    /// A titled section. Collapsible sections can be folded away by the user.
    /// </summary>
    /// <param name="header">The section's title.</param>
    /// <param name="elements">What goes inside.</param>
    /// <param name="collapsible">Let the user fold the section away.</param>
    /// <param name="expanded">Whether a collapsible section starts open.</param>
    /// <returns name="element">The form element.</returns>
    /// <search>section,group,box,fieldset,collapsible</search>
    public static FormElement Section(
        string header,
        List<FormElement> elements,
        bool collapsible = false,
        bool expanded = true)
    {
        IReadOnlyList<FormElement> children = NodeSupport.Elements(elements);

        return collapsible
            ? new ExpanderElement { Header = header, Children = children, IsExpanded = expanded }
            : new GroupBoxElement { Header = header, Children = children };
    }

    /// <summary>
    /// Elements laid out left to right.
    /// </summary>
    /// <param name="elements">What goes inside.</param>
    /// <param name="equalWidths">Give every element the same width.</param>
    /// <param name="wrap">Move onto a new line when the row runs out of width.</param>
    /// <param name="spacing">Gap between elements. Negative uses the theme's spacing.</param>
    /// <returns name="element">The form element.</returns>
    /// <search>row,horizontal,hstack,side by side</search>
    public static FormElement Row(
        List<FormElement> elements,
        bool equalWidths = false,
        bool wrap = false,
        double spacing = -1)
        => new HStackElement
        {
            Children = NodeSupport.Elements(elements),
            EqualWidths = equalWidths,
            Wrap = wrap,
            Spacing = spacing,
        };

    /// <summary>
    /// Elements stacked top to bottom.
    /// </summary>
    /// <param name="elements">What goes inside.</param>
    /// <param name="spacing">Gap between elements. Negative uses the theme's spacing.</param>
    /// <returns name="element">The form element.</returns>
    /// <search>column,vertical,vstack,stack</search>
    public static FormElement Column(List<FormElement> elements, double spacing = -1)
        => new VStackElement
        {
            Children = NodeSupport.Elements(elements),
            Spacing = spacing,
        };

    /// <summary>
    /// Elements arranged in a grid. Columns are described as a comma-separated list, where
    /// <c>auto</c> sizes to content, <c>*</c> takes a share of the leftover space, <c>2*</c>
    /// takes two shares, and a plain number is a pixel width: <c>"auto, *, 120"</c>.
    ///
    /// Elements fill the grid in order unless they were placed with <c>Layout.Cell</c>.
    /// </summary>
    /// <param name="elements">What goes inside.</param>
    /// <param name="columns">Column widths, comma separated.</param>
    /// <param name="columnSpacing">Gap between columns. Negative uses the theme's spacing.</param>
    /// <param name="rowSpacing">Gap between rows. Negative uses the theme's spacing.</param>
    /// <returns name="element">The form element.</returns>
    /// <search>grid,table,columns,rows,layout</search>
    public static FormElement Grid(
        List<FormElement> elements,
        string columns = "*, *",
        double columnSpacing = -1,
        double rowSpacing = -1)
        => new GridElement
        {
            Children = NodeSupport.Elements(elements),
            Columns = NodeSupport.Tracks(columns),
            ColumnSpacing = columnSpacing,
            RowSpacing = rowSpacing,
        };

    /// <summary>
    /// Places an element in a specific cell of a <c>Layout.Grid</c>, optionally spanning several.
    /// </summary>
    /// <param name="element">The element to place.</param>
    /// <param name="row">Zero-based row.</param>
    /// <param name="column">Zero-based column.</param>
    /// <param name="rowSpan">How many rows to cover.</param>
    /// <param name="columnSpan">How many columns to cover.</param>
    /// <returns name="element">The placed element.</returns>
    /// <search>cell,grid,place,span,position</search>
    public static FormElement Cell(
        FormElement element,
        int row = 0,
        int column = 0,
        int rowSpan = 1,
        int columnSpan = 1)
        => Behavior.Restyle(element, style => style with
        {
            GridRow = Math.Max(0, row),
            GridColumn = Math.Max(0, column),
            GridRowSpan = Math.Max(1, rowSpan),
            GridColumnSpan = Math.Max(1, columnSpan),
        });

    /// <summary>
    /// A tab strip. Its children should be <c>Layout.TabPage</c> elements.
    /// </summary>
    /// <param name="pages">The pages, built with Layout.TabPage.</param>
    /// <param name="selectedIndex">Which page is shown first.</param>
    /// <returns name="element">The form element.</returns>
    /// <search>tabs,tabcontrol,pages,notebook</search>
    public static FormElement Tabs(List<FormElement> pages, int selectedIndex = 0)
        => new TabsElement
        {
            Children = NodeSupport.Elements(pages),
            SelectedIndex = Math.Max(0, selectedIndex),
        };

    /// <summary>
    /// One page of a <c>Layout.Tabs</c> strip.
    /// </summary>
    /// <param name="header">The tab's caption.</param>
    /// <param name="elements">What goes on the page.</param>
    /// <returns name="element">The form element.</returns>
    /// <search>tab,page,tabpage</search>
    public static FormElement TabPage(string header, List<FormElement> elements)
        => new TabPageElement
        {
            Header = header,
            Children = NodeSupport.Elements(elements),
        };

    /// <summary>
    /// A section the user can fold away.
    /// </summary>
    /// <param name="header">The section's title.</param>
    /// <param name="elements">What goes inside.</param>
    /// <param name="expanded">Whether it starts open.</param>
    /// <returns name="element">The form element.</returns>
    /// <search>expander,collapse,fold,accordion,disclosure</search>
    public static FormElement Expander(string header, List<FormElement> elements, bool expanded = true)
        => new ExpanderElement
        {
            Header = header,
            Children = NodeSupport.Elements(elements),
            IsExpanded = expanded,
        };

    /// <summary>
    /// A raised panel, for grouping something that deserves emphasis.
    /// </summary>
    /// <param name="elements">What goes inside.</param>
    /// <param name="header">Optional heading.</param>
    /// <param name="subheader">Optional second line under the heading.</param>
    /// <param name="shadow">Draw a drop shadow.</param>
    /// <returns name="element">The form element.</returns>
    /// <search>card,panel,tile,surface</search>
    public static FormElement Card(
        List<FormElement> elements,
        string header = "",
        string subheader = "",
        bool shadow = true)
        => new CardElement
        {
            Children = NodeSupport.Elements(elements),
            Header = NodeSupport.OrNull(header),
            Subheader = NodeSupport.OrNull(subheader),
            HasShadow = shadow,
        };

    /// <summary>
    /// A region that scrolls when its contents do not fit.
    /// </summary>
    /// <param name="elements">What goes inside.</param>
    /// <param name="maxHeight">Height at which scrolling starts.</param>
    /// <param name="allowHorizontal">Also scroll sideways.</param>
    /// <returns name="element">The form element.</returns>
    /// <search>scroll,scrollviewer,overflow</search>
    public static FormElement Scroll(
        List<FormElement> elements,
        double maxHeight = 300,
        bool allowHorizontal = false)
    {
        ScrollElement scroll = new()
        {
            Children = NodeSupport.Elements(elements),
            AllowHorizontal = allowHorizontal,
        };

        return maxHeight > 0
            ? scroll with { Style = new ElementStyle { MaxHeight = maxHeight } }
            : scroll;
    }

    /// <summary>
    /// Elements docked to the edges of the available space. Use <c>Layout.Docked</c> to choose
    /// each element's edge; the last one fills what is left.
    /// </summary>
    /// <param name="elements">What goes inside.</param>
    /// <param name="lastChildFills">Let the final element take the remaining space.</param>
    /// <returns name="element">The form element.</returns>
    /// <search>dock,dockpanel,edges,anchor</search>
    public static FormElement Dock(List<FormElement> elements, bool lastChildFills = true)
        => new DockElement
        {
            Children = NodeSupport.Elements(elements),
            LastChildFills = lastChildFills,
        };

    /// <summary>
    /// Attaches an element to one edge of a <c>Layout.Dock</c>.
    /// </summary>
    /// <param name="element">The element to place.</param>
    /// <param name="side">One of Left, Top, Right or Bottom.</param>
    /// <returns name="element">The placed element.</returns>
    /// <search>dock,side,edge,left,right,top,bottom</search>
    public static FormElement Docked(FormElement element, string side = "Left")
    {
        DockSide parsed = Enum.TryParse(side, ignoreCase: true, out DockSide value) ? value : DockSide.Left;
        return Behavior.Restyle(element, style => style with { Dock = parsed });
    }

    /// <summary>
    /// Two panes separated by a splitter the user can drag.
    /// </summary>
    /// <param name="first">The left or top pane.</param>
    /// <param name="second">The right or bottom pane.</param>
    /// <param name="horizontal">Split side by side rather than one above the other.</param>
    /// <param name="position">Share of the space given to the first pane, from 0 to 1.</param>
    /// <returns name="element">The form element.</returns>
    /// <search>split,splitter,panes,resize</search>
    public static FormElement Split(
        FormElement first,
        FormElement second,
        bool horizontal = true,
        double position = 0.5)
        => new SplitViewElement
        {
            Children = NodeSupport.Elements(new[] { first, second }),
            Orientation = horizontal ? LayoutOrientation.Horizontal : LayoutOrientation.Vertical,
            SplitterPosition = position,
        };

    /// <summary>
    /// A run of static text.
    /// </summary>
    /// <param name="text">What to show.</param>
    /// <param name="headingLevel">1 to 4 renders as a heading; 0 is body text.</param>
    /// <param name="muted">Draw in the secondary colour, for captions and asides.</param>
    /// <returns name="element">The form element.</returns>
    /// <search>label,text,caption,heading,title</search>
    public static FormElement Label(string text, int headingLevel = 0, bool muted = false)
        => new LabelElement
        {
            Text = text,
            HeadingLevel = Math.Max(0, Math.Min(4, headingLevel)),
            IsMuted = muted,
        };

    /// <summary>
    /// A block of Markdown. Supports headings, bold, italic, inline code, links, bullet and
    /// numbered lists, and horizontal rules.
    /// </summary>
    /// <param name="text">The Markdown source.</param>
    /// <returns name="element">The form element.</returns>
    /// <search>markdown,rich text,formatted,documentation</search>
    public static FormElement Markdown(string text) => new MarkdownElement { Text = text };

    /// <summary>
    /// A picture.
    /// </summary>
    /// <param name="path">Path to the image file.</param>
    /// <param name="width">Fixed width. Null sizes to the image.</param>
    /// <param name="height">Fixed height. Null sizes to the image.</param>
    /// <param name="alternateText">Description used when the image cannot be loaded.</param>
    /// <returns name="element">The form element.</returns>
    /// <search>image,picture,logo,graphic,photo</search>
    public static FormElement Image(
        string path,
        [DefaultArgument("null")] object? width = null,
        [DefaultArgument("null")] object? height = null,
        string alternateText = "")
    {
        ImageElement image = new()
        {
            Path = path,
            AlternateText = NodeSupport.OrNull(alternateText),
        };

        double? w = NodeSupport.OptionalDouble(width);
        double? h = NodeSupport.OptionalDouble(height);

        return w.HasValue || h.HasValue
            ? image with { Style = new ElementStyle { Width = w, Height = h } }
            : image;
    }

    /// <summary>
    /// A dividing line, optionally with a caption on it.
    /// </summary>
    /// <param name="caption">Optional text drawn on the line.</param>
    /// <returns name="element">The form element.</returns>
    /// <search>separator,divider,rule,line,hr</search>
    public static FormElement Separator(string caption = "")
        => new SeparatorElement { Caption = NodeSupport.OrNull(caption) };

    /// <summary>
    /// Blank space.
    /// </summary>
    /// <param name="size">How much space, in pixels.</param>
    /// <returns name="element">The form element.</returns>
    /// <search>spacer,gap,space,padding</search>
    public static FormElement Spacer(double size = 8) => new SpacerElement { Size = size };

    /// <summary>
    /// A progress bar. It shows a fixed value: nothing in the form updates it while it is open.
    /// </summary>
    /// <param name="value">How far along, between 0 and the maximum.</param>
    /// <param name="maximum">The value that counts as complete.</param>
    /// <param name="indeterminate">Show a looping animation instead of a fixed amount.</param>
    /// <param name="segments">
    /// Draw the bar as this many discrete cells rather than one continuous fill. Zero is
    /// continuous. Segments are for counting rather than measuring: "five of seven days" reads
    /// off a segmented bar at a glance, where a continuous bar at 71% does not.
    /// </param>
    /// <returns name="element">The form element.</returns>
    /// <search>progress,bar,percent,loading,segments,steps</search>
    public static FormElement Progress(
        double value = 0,
        double maximum = 100,
        bool indeterminate = false,
        int segments = 0)
        => new ProgressElement
        {
            Value = value,
            Maximum = maximum <= 0 ? 100 : maximum,
            IsIndeterminate = indeterminate,
            Segments = Math.Max(0, segments),
        };

    /// <summary>
    /// A button that closes the form and reports its tag, which is how one form offers several
    /// outcomes such as "Place" and "Place and continue".
    /// </summary>
    /// <param name="text">The button's caption.</param>
    /// <param name="tag">Reported as buttonClicked. Falls back to the caption.</param>
    /// <param name="primary">Draw in the accent colour, as the main action.</param>
    /// <returns name="element">The form element.</returns>
    /// <search>button,action,submit,command</search>
    public static FormElement Button(string text, string tag = "", bool primary = false)
        => new ButtonElement
        {
            Text = text,
            Tag = NodeSupport.OrNull(tag) ?? text,
            Action = ButtonAction.SubmitWithTag,
            IsPrimary = primary,
        };

    /// <summary>
    /// A button that opens a web page. The form stays open.
    /// </summary>
    /// <param name="text">The button's caption.</param>
    /// <param name="url">The address to open. Only http and https are opened.</param>
    /// <returns name="element">The form element.</returns>
    /// <search>link,url,web,browser,help</search>
    public static FormElement LinkButton(string text, string url)
        => new ButtonElement
        {
            Text = text,
            Url = url,
            Action = ButtonAction.OpenUrl,
        };

    /// <summary>
    /// A button that puts every field back to its default. The form stays open.
    /// </summary>
    /// <param name="text">The button's caption.</param>
    /// <returns name="element">The form element.</returns>
    /// <search>reset,clear,defaults,revert</search>
    public static FormElement ResetButton(string text = "Reset")
        => new ButtonElement { Text = text, Action = ButtonAction.Reset };
}
