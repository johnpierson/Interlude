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
    /// A titled section: a heading with a bordered panel of elements under it.
    ///
    /// The first reach for structure in a long form. Grouping six fields under "Naming" and four
    /// under "Output" turns a wall of inputs into two things to read, and costs one node.
    ///
    /// With <c>collapsible</c> the user can fold it away. A folded section still submits every
    /// field inside it — folding hides, it does not exclude. To actually leave fields out of the
    /// answers, attach <c>Behavior.VisibleIf</c>: hidden fields are never validated and never
    /// block submission.
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
    /// Elements laid out left to right, each taking the width it asks for.
    ///
    /// The right shape for things that belong together on one line — a width beside a height, a
    /// path beside its Browse button. Each element keeps its own label, so a row of three inputs
    /// reads as three fields rather than one.
    ///
    /// A row does not wrap. When the contents outgrow the form's width they are squeezed rather
    /// than moved to a second line, so use <c>Layout.Grid</c> when the columns need to be told how
    /// to share the space.
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
    ///
    /// This is what a form already does with the elements handed to <c>Form.Show</c>, so a column
    /// at the top level changes nothing. It earns its place *inside* something else: one pane of
    /// a <c>Layout.Split</c>, one cell of a <c>Layout.Grid</c>, one side of a <c>Layout.Row</c> —
    /// anywhere a single slot has to hold several elements.
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
    ///
    /// Only needed when the automatic left-to-right, top-to-bottom filling is not what you want —
    /// to leave a cell empty, or to let one element run across the full width above the rest.
    /// Mixing placed and unplaced elements in one grid works, but the unplaced ones keep flowing
    /// into the next free cell and it gets hard to predict; place all of them or none.
    ///
    /// Rows and columns are numbered from zero.
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
    /// A tab strip, for a form with more in it than fits on one screen.
    ///
    /// Its children should be <c>Layout.TabPage</c> elements; anything else lands on an unnamed
    /// page. Every field on every page is part of the same form and comes back in the same
    /// answers — tabs divide the screen, not the results.
    ///
    /// Worth knowing before choosing tabs over sections: a field failing validation on a page the
    /// user is not looking at will block submission, and the error is on the other page. Keep
    /// fields that validate against each other on the same tab.
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
    ///
    /// Like <c>Input.TreeItem</c>, this is material rather than a standalone element: it only
    /// means anything fed into a <c>Layout.Tabs</c> node. The header is what the user clicks.
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
    /// A section the user can fold away, starting folded unless told otherwise.
    ///
    /// The place to put the advanced settings — visible to whoever wants them, out of the way of
    /// everybody else. That is the difference from a collapsible <c>Layout.Section</c>, which
    /// starts open: this one hides by default, and what is inside should be the things most users
    /// never touch.
    ///
    /// Folded is not hidden. Fields inside still submit, still validate and still block. If a
    /// required field is folded away, the user is stopped by an error they cannot see — put
    /// nothing required in here, or reveal it with <c>Behavior.VisibleIf</c> instead.
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
    /// A raised panel with an optional heading and subheading, for something that deserves
    /// emphasis.
    ///
    /// Where <c>Layout.Section</c> divides a form into parts, a card lifts one part out of it —
    /// a summary of what is about to happen, a warning, the totals at the end of a takeoff. Used
    /// once or twice in a form it draws the eye; used for every group it stops meaning anything.
    ///
    /// <c>hasShadow</c> lifts it further. In a theme that offsets shadows the shadow is hard and
    /// flat; in one that does not it is soft and blurred.
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
    ///
    /// Rarely needed: the form window already scrolls as a whole once it passes <c>maxHeight</c>.
    /// Reach for this only when one part should scroll while the rest stays put — a long list of
    /// options above a fixed summary, say.
    ///
    /// A scrolling region nested inside the window's own scrolling is a trap worth avoiding. Two
    /// scrollbars in one dialog leave the user rolling the wheel over the wrong half.
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
    ///
    /// Order decides the corners. Each docked element takes its whole edge out of what remains,
    /// so a Top followed by a Left gives a banner across the full width with the sidebar beneath
    /// it, while a Left followed by a Top gives a full-height sidebar with the banner beside it.
    /// </summary>
    /// <param name="element">The element to place.</param>
    /// <param name="side">One of Left, Top, Right or Bottom.</param>
    /// <returns name="element">The placed element.</returns>
    /// <search>dock,side,edge,left,right,top,bottom</search>
    public static FormElement Docked(FormElement element, string side = "Left")
    {
        DockSide parsed = NodeSupport.ParseEnum(side, nameof(side), DockSide.Left);
        return Behavior.Restyle(element, style => style with { Dock = parsed });
    }

    /// <summary>
    /// Two panes separated by a splitter the user can drag.
    ///
    /// For the case where the right balance depends on the data rather than on the designer — a
    /// list of things beside the settings for the selected one, where one project has four items
    /// and the next has four hundred.
    ///
    /// The split position is where it starts, not where it stays: dragging it is the point, and
    /// the position is not remembered between runs.
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
    /// A run of static text: an instruction, a note, a heading.
    ///
    /// Shows something rather than asking something, so it contributes nothing to the answers and
    /// needs no key.
    ///
    /// <c>headingLevel</c> above zero makes it a heading — 1 is largest, 4 smallest — and headings
    /// take the theme's capitals and letter-spacing where body text never does. <c>isMuted</c>
    /// greys it for an aside.
    ///
    /// For a sentence explaining one field, <c>Behavior.WithHelp</c> puts it under that field
    /// where it belongs, instead of leaving the reader to work out which one it refers to.
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
    /// A value the form works out and shows back, live, as the fields it reads are edited.
    ///
    /// <c>value</c> is usually a template — <c>"{prefix}{sample_name}{suffix}"</c> — where each
    /// name in braces is a field's key. It also accepts any <c>Compute</c> node, for a preview
    /// that has to choose between two forms:
    ///
    /// <code>
    /// Layout.Preview("New name",
    ///     Compute.If(Condition.IsChecked("add_number"),
    ///                Compute.Format("{prefix}{sample_name} {start_number:000}"),
    ///                Compute.Format("{prefix}{sample_name}")))
    /// </code>
    ///
    /// A placeholder may carry a format specifier after a colon: <c>{start_number:000}</c> pads
    /// to three digits, <c>{total:F2}</c> fixes two decimals, <c>{due:yyyy-MM-dd}</c> writes a
    /// date the way a file name wants it.
    ///
    /// A preview answers nothing. It has no key, never appears in a form's results, and is never
    /// validated — which is what separates it from a read-only field carrying a computed value.
    /// Reach for that instead when you need the value back out of the form.
    ///
    /// Everything a preview shows must already be on the form. Interlude knows nothing about the
    /// items a graph is about to work on, so a form renaming fifty views previews one sample name
    /// the author supplies — most naturally as the default value of a field the user can edit,
    /// which doubles as a way to try the rule against an awkward name.
    /// </summary>
    /// <param name="label">The caption, shown in the same column as the fields' labels.</param>
    /// <param name="value">A template string, or a computation from the <c>Compute</c> nodes.</param>
    /// <param name="placeholder">Shown while the value is empty.</param>
    /// <param name="monospaced">Render in a fixed-width face, for names, codes and paths.</param>
    /// <returns name="element">The form element.</returns>
    /// <search>preview,live,derived,computed,summary,result,format,template</search>
    public static FormElement Preview(
        string label,
        object value,
        string placeholder = "",
        bool monospaced = false)
        => new PreviewElement
        {
            Label = label,
            Value = NodeSupport.AsOperand(value),
            Placeholder = NodeSupport.OrNull(placeholder),
            IsMonospaced = monospaced,
        };

    /// <summary>
    /// A block of Markdown, for anything longer than a sentence.
    ///
    /// Supports headings, bold, italic, inline code, links, bullet and numbered lists, and
    /// horizontal rules. Not a full Markdown implementation — no tables, images or block quotes —
    /// because the alternative was a dependency, and the package ships no code it did not write.
    /// Anything unrecognised is shown as the plain text it was written as, so an unsupported
    /// construct degrades into something readable rather than into markup on screen.
    ///
    /// This is where instructions belong when they need structure. For a single line,
    /// <c>Layout.Label</c> is less machinery.
    /// </summary>
    /// <param name="text">The Markdown source.</param>
    /// <returns name="element">The form element.</returns>
    /// <search>markdown,rich text,formatted,documentation</search>
    public static FormElement Markdown(string text) => new MarkdownElement { Text = text };

    /// <summary>
    /// A picture, loaded from a file on disk.
    ///
    /// A diagram of what the graph is about to do explains it better than three paragraphs above
    /// the fields. Worth the space for anything spatial.
    ///
    /// The path is read when the form opens, from wherever the machine running the graph can see —
    /// so a path on your own desktop breaks the moment somebody else runs it. Put shared images on
    /// a network location, or beside the graph.
    ///
    /// A file that is missing or unreadable shows the <c>alternateText</c> rather than taking the
    /// form down: an image is decoration, and decoration is never worth losing the dialog over.
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
    /// A dividing line, optionally with a caption sitting on it.
    ///
    /// The lightest way to group: it separates what is above from what is below without the
    /// border, heading and indentation a <c>Layout.Section</c> brings. A captioned separator gives
    /// the group a name for the price of one line.
    /// </summary>
    /// <param name="caption">Optional text drawn on the line.</param>
    /// <returns name="element">The form element.</returns>
    /// <search>separator,divider,rule,line,hr</search>
    public static FormElement Separator(string caption = "")
        => new SeparatorElement { Caption = NodeSupport.OrNull(caption) };

    /// <summary>
    /// Blank space, for when two things need to be further apart than the theme puts them.
    ///
    /// The escape hatch, not the tool. Spacing between fields is the theme's job — set it once
    /// with <c>Theme.Create</c>'s density and every form in the office agrees — and a form held
    /// together by hand-placed gaps has to be re-tuned whenever anything above it changes.
    ///
    /// Its real use is horizontal: inside a <c>Layout.Row</c>, a spacer pushes what follows it to
    /// the right.
    /// </summary>
    /// <param name="size">How much space, in pixels.</param>
    /// <returns name="element">The form element.</returns>
    /// <search>spacer,gap,space,padding</search>
    public static FormElement Spacer(double size = 8) => new SpacerElement { Size = size };

    /// <summary>
    /// A progress bar showing a **fixed** value.
    ///
    /// Read that twice before using it. Nothing in the form moves this bar: a form runs while the
    /// graph waits, so there is no work going on behind it to report. It is for showing a figure
    /// that was already worked out — twelve of twenty sheets issued, sixty per cent of the budget
    /// spent — and for that it is a clearer picture than the number alone.
    ///
    /// It is not a way to show a long operation running. That happens after the form closes, when
    /// there is no form left to draw in.
    ///
    /// <c>segments</c> above zero draws discrete cells instead of a continuous fill, and cells
    /// fill by rounding — five of seven days is five whole cells, because a part-filled cell
    /// invites the reader to wonder what a partial day was. Use it for counting, and the
    /// continuous bar for measuring.
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
    /// A button that closes the form and reports its tag on <c>buttonClicked</c>, which is how one
    /// form offers several outcomes such as "Place" and "Place and continue".
    ///
    /// A closing button counts as submitting: the answers come back filled in and
    /// <c>wasSubmitted</c> is true, so branch on <c>Result.ButtonClicked</c> to tell which way the
    /// user went. Give every button a distinct <c>tag</c>; the caption is for the reader and can
    /// be reworded without breaking the graph, the tag is what the graph tests.
    ///
    /// Validation still applies. A form with an invalid field will not close on any of them.
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
    /// A button that opens a web page in the machine's browser. The form stays open.
    ///
    /// For sending the user to the office standard, the wiki page explaining the naming
    /// convention, or the issue tracker — without them losing what they have typed.
    ///
    /// The URL opens in whatever handles it outside Dynamo; nothing is shown inside the form, and
    /// the answers are untouched.
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
    ///
    /// Worth adding to any form with remembered answers, where what the user sees is what they
    /// typed last time and getting back to a clean start would otherwise mean clearing a dozen
    /// fields by hand.
    ///
    /// It resets the fields on screen. It does not clear what was remembered — that is
    /// <c>Form.Forget</c> — so cancelling after a reset leaves the previous answers intact.
    /// </summary>
    /// <param name="text">The button's caption.</param>
    /// <returns name="element">The form element.</returns>
    /// <search>reset,clear,defaults,revert</search>
    public static FormElement ResetButton(string text = "Reset")
        => new ButtonElement { Text = text, Action = ButtonAction.Reset };
}
