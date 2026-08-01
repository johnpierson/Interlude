using System;
using System.Windows;
using System.Windows.Media;

namespace Interlude.Preview;

/// <summary>
/// The shapes a node icon can be drawn from.
///
/// This is the vocabulary half of the family system: the plate colour says which category a node
/// belongs to, and one of these says what it does. Deliberately shared across categories — a date
/// field, a date rule and "get the date back out" are the same drawing in three colours, which is
/// the point. Twenty-odd marks reused ninety-odd times reads better in a library tree than a
/// hundred and twelve marks nobody can tell apart at sixteen pixels.
/// </summary>
internal enum Glyph
{
    Field,
    Lines,
    Dots,
    Stepper,
    StepperDecimal,
    Slider,
    Sliders,
    CheckBox,
    UncheckedBox,
    Toggle,
    DropChevron,
    Stack,
    Radio,
    Calendar,
    Droplet,
    Document,
    Folder,
    Tree,
    TreeLeaf,
    Columns,
    Rows,
    Grid,
    GridCell,
    Card,
    HeaderPanel,
    Expander,
    Tabs,
    TabPage,
    Split,
    Frame,
    DockedEdge,
    Scroll,
    Separator,
    Spacer,
    Markdown,
    Picture,
    ProgressBar,
    Button,
    Reset,
    Asterisk,
    Lock,
    Eye,
    Power,
    Key,
    Bubble,
    Resize,
    Shield,
    EqualsSign,
    FilledDisc,
    VennAnd,
    VennOr,
    Greater,
    GreaterOrEqual,
    Less,
    LessOrEqual,
    Inside,
    Member,
    AnchorStart,
    AnchorEnd,
    Wildcard,
    EmptyBox,
    FullBox,
    Operators,
    Sigma,
    Braces,
    FieldRef,
    Pin,
    Table,
    Fork,
    Range,
    Ruler,
    Compare,
    Sun,
    Moon,
    Monitor,
    Contrast,
    HardShadow,
    Palette,
    Swatches,
    Window,
    CircleTick,
    CircleCross,
    Cursor,
}

/// <summary>
/// A small mark in the bottom-right corner, or an overlay across the whole plate for negation.
///
/// Badges are where legibility runs out: at 32 pixels a badge is about four pixels of usable
/// interior. They are used only where two nodes would otherwise be the same drawing, never to
/// carry meaning on their own.
/// </summary>
internal enum Badge
{
    None,
    Tick,
    Cross,
    Plus,
    Play,
    ArrowOut,
    ArrowIn,
    Fork,
    Slash,
}

internal static class IconGlyphs
{
    /// <summary>Draws one glyph into the 18-unit glyph box.</summary>
    internal static void Draw(IconGeometry g, Glyph glyph)
    {
        switch (glyph)
        {
            // ---- text entry -------------------------------------------------------------------
            case Glyph.Field:
                g.Box(6, 11, 18, 9);
                g.Line(9.5, 13.2, 9.5, 17.8, g.Heavy);
                break;

            case Glyph.Lines:
                g.Line(6, 9.5, 24, 9.5, g.Heavy);
                g.Line(6, 15, 24, 15, g.Heavy);
                g.Line(6, 20.5, 18.5, 20.5, g.Heavy);
                break;

            case Glyph.Dots:
                g.Box(6, 11, 18, 9);
                g.Disc(10.5, 15.5, 1.6);
                g.Disc(15, 15.5, 1.6);
                g.Disc(19.5, 15.5, 1.6);
                break;

            case Glyph.Stepper:
            case Glyph.StepperDecimal:
                g.Box(6, 11, 18, 9);
                g.Polygon(g.Ink, 19.2, 14.6, 22.8, 14.6, 21, 12.4);
                g.Polygon(g.Ink, 19.2, 16.4, 22.8, 16.4, 21, 18.6);
                if (glyph == Glyph.StepperDecimal)
                {
                    g.Disc(10, 18, 1.3);
                }

                break;

            // ---- choosing ---------------------------------------------------------------------
            case Glyph.Slider:
                g.Line(6, 15.5, 24, 15.5, g.Heavy);
                g.Box(14, 11, 5, 9);
                break;

            case Glyph.Sliders:
                g.Line(6, 11, 24, 11, g.Heavy);
                g.Box(9, 8, 4.5, 6);
                g.Line(6, 19, 24, 19, g.Heavy);
                g.Box(16, 16, 4.5, 6);
                break;

            case Glyph.CheckBox:
            case Glyph.UncheckedBox:
                g.Box(7, 7, 16, 16);
                if (glyph == Glyph.CheckBox)
                {
                    g.Polyline(g.Heavy, 10.2, 15.2, 13.4, 18.4, 20, 10.6);
                }

                break;

            case Glyph.Toggle:
                g.Pill(6, 11, 18, 9);
                g.Disc(19.4, 15.5, 2.7);
                break;

            case Glyph.DropChevron:
                g.Box(6, 11, 18, 9);
                g.Polyline(g.Stroke, 17, 13.8, 19.6, 17, 22.2, 13.8);
                break;

            case Glyph.Stack:
                g.Box(6, 6.5, 18, 5.6);
                g.Box(6, 13.2, 18, 5.6, g.Ink);
                g.Box(6, 19.9, 18, 5.6);
                break;

            case Glyph.Radio:
                g.Circle(9.5, 10.5, 3.2);
                g.Disc(9.5, 10.5, 1.5);
                g.Line(14.5, 10.5, 23.5, 10.5, g.Heavy);
                g.Circle(9.5, 19.5, 3.2);
                g.Line(14.5, 19.5, 23.5, 19.5, g.Heavy);
                break;

            case Glyph.Calendar:
                g.Line(10.5, 5.5, 10.5, 9, g.Heavy);
                g.Line(19.5, 5.5, 19.5, 9, g.Heavy);
                g.Box(6, 8, 18, 16);
                g.Block(6, 8, 18, 4.4);
                g.Disc(10.5, 16, 1.5);
                g.Disc(15, 16, 1.5);
                g.Disc(19.5, 16, 1.5);
                g.Disc(10.5, 20.5, 1.5);
                break;

            case Glyph.Droplet:
                // A tear: straight shoulders down from the point, closed off by a half circle.
                g.Draw(g.Paper, g.Stroke, DropletPath());
                break;

            // ---- files ------------------------------------------------------------------------
            case Glyph.Document:
                g.Shape(g.Paper, g.Stroke, 8, 6, 18, 6, 22, 10, 22, 24, 8, 24);
                g.Polyline(g.Stroke, 18, 6, 18, 10, 22, 10);
                break;

            case Glyph.Folder:
                g.Shape(g.Paper, g.Stroke, 6, 22, 6, 8, 12.5, 8, 14.5, 10.5, 24, 10.5, 24, 22);
                break;

            case Glyph.Tree:
            case Glyph.TreeLeaf:
                if (glyph == Glyph.Tree)
                {
                    g.Box(6, 6.5, 7.5, 5.5);
                    g.Polyline(g.Stroke, 9.5, 12, 9.5, 21.5, 15, 21.5);
                    g.Line(9.5, 15.5, 15, 15.5);
                    g.Box(15, 12.8, 9, 5.5);
                    g.Box(15, 18.8, 9, 5.5);
                }
                else
                {
                    g.Polyline(g.Stroke, 9.5, 6.5, 9.5, 18, 15, 18);
                    g.Box(15, 15.2, 9, 5.6);
                }

                break;

            // ---- layout -----------------------------------------------------------------------
            case Glyph.Columns:
                g.Box(6, 7, 4.8, 16);
                g.Box(12.6, 7, 4.8, 16);
                g.Box(19.2, 7, 4.8, 16);
                break;

            case Glyph.Rows:
                g.Box(6, 7, 18, 4.8);
                g.Box(6, 13.6, 18, 4.8);
                g.Box(6, 20.2, 18, 4.8);
                break;

            case Glyph.Grid:
            case Glyph.GridCell:
                if (glyph == Glyph.GridCell)
                {
                    g.Block(6, 6, 9, 9);
                }

                g.Box(6, 6, 18, 18, glyph == Glyph.GridCell ? Brushes.Transparent : g.Paper);
                g.Line(15, 6, 15, 24);
                g.Line(6, 15, 24, 15);
                break;

            case Glyph.Card:
                g.Block(10, 10, 14, 14);
                g.Box(6, 6, 14, 14);
                break;

            case Glyph.HeaderPanel:
                g.Box(6, 7, 18, 16);
                g.Block(6, 7, 18, 5);
                break;

            case Glyph.Expander:
                g.Box(6, 7.5, 18, 7);
                g.Polyline(g.Stroke, 17, 9.6, 19.4, 12.4, 21.8, 9.6);
                g.Box(6, 16.5, 18, 7);
                break;

            case Glyph.Tabs:
            case Glyph.TabPage:
                g.Box(6, 6.5, 8, 5.5, glyph == Glyph.TabPage ? g.Ink : g.Paper);
                g.Box(15, 6.5, 8, 5.5);
                g.Box(6, 12, 18, 11.5);
                break;

            case Glyph.Split:
                g.Box(6, 7, 18, 16);
                g.Block(13.9, 7, 2.2, 16);
                break;

            case Glyph.Frame:
            case Glyph.DockedEdge:
                g.Box(6, 6, 18, 18);
                if (glyph == Glyph.DockedEdge)
                {
                    g.Block(6, 6, 5.5, 18);
                }

                g.Box(11.5, 11.5, 7, 7, Brushes.Transparent);
                break;

            case Glyph.Scroll:
                g.Box(6, 6, 14, 18);
                g.Box(21, 6, 3, 18);
                g.Block(21, 8, 3, 7.5);
                break;

            case Glyph.Separator:
                g.Block(6, 13.7, 18, 3);
                break;

            case Glyph.Spacer:
                g.Box(6, 6.5, 18, 4.5);
                g.Box(6, 21, 18, 4.5);
                g.Line(15, 13, 15, 19, g.Hairline);
                g.Polygon(g.Ink, 13.2, 13.6, 16.8, 13.6, 15, 11.6);
                g.Polygon(g.Ink, 13.2, 18.4, 16.8, 18.4, 15, 20.4);
                break;

            case Glyph.Markdown:
                g.Block(6, 6.5, 11, 4.6);
                g.Line(6, 15, 24, 15, g.Heavy);
                g.Line(6, 20.5, 19, 20.5, g.Heavy);
                break;

            case Glyph.Picture:
                g.Box(6, 8, 18, 15);
                g.Disc(11, 12.6, 2);
                g.Polygon(g.Ink, 7.1, 21.9, 12.5, 15.4, 16, 19.2, 19.6, 14.4, 22.9, 21.9);
                break;

            case Glyph.ProgressBar:
                g.Box(6, 11.5, 18, 8);
                g.Block(6, 11.5, 10.5, 8);
                break;

            case Glyph.Button:
                // A card is a plain panel with a shadow, so a button needs the label bar to tell
                // the two apart. Without it they are the same drawing in the same colour.
                g.Block(9.5, 14, 14.5, 7.5);
                g.Box(6, 10.5, 14.5, 7.5);
                g.Block(8.8, 13.4, 8.9, 1.9);
                break;

            case Glyph.Reset:
                // Three sides of a square with a head on the end: a turn back to the start.
                g.Polyline(g.Heavy, 19.5, 22, 19.5, 10.5, 12, 10.5);
                g.Polygon(g.Ink, 12.8, 6.2, 12.8, 14.8, 6.5, 10.5);
                break;

            // ---- behaviour --------------------------------------------------------------------
            case Glyph.Asterisk:
                g.Line(15, 6.5, 15, 23.5, g.Heavy);
                g.Line(7.6, 10.8, 22.4, 19.2, g.Heavy);
                g.Line(7.6, 19.2, 22.4, 10.8, g.Heavy);
                break;

            case Glyph.Lock:
                g.Arc(15, 13.2, 4.6, 180, 360, g.Heavy);
                g.Line(10.4, 13.2, 10.4, 14.5, g.Heavy);
                g.Line(19.6, 13.2, 19.6, 14.5, g.Heavy);
                g.Box(7.5, 14, 15, 9.5);
                g.Disc(15, 18.7, 1.8);
                break;

            case Glyph.Eye:
                g.Draw(g.Paper, g.Stroke, IconGeometry.Curve(
                    true,
                    new Point(5.8, 15),
                    new Point(15, 6.4), new Point(24.2, 15),
                    new Point(15, 23.6), new Point(5.8, 15)));
                g.Disc(15, 15, 3);
                break;

            case Glyph.Power:
                g.Arc(15, 16, 7, 300, 600, g.Heavy);
                g.Line(15, 6.5, 15, 15, g.Heavy);
                break;

            case Glyph.Key:
                g.Circle(10.2, 15, 4.4);
                g.Disc(10.2, 15, 1.5);
                g.Line(14.6, 15, 24, 15, g.Heavy);
                g.Line(19.5, 15, 19.5, 19.5, g.Heavy);
                g.Line(23, 15, 23, 18.4, g.Heavy);
                break;

            case Glyph.Bubble:
                g.Shape(g.Paper, g.Stroke, 6, 6, 24, 6, 24, 18, 14.5, 18, 10, 23.5, 10, 18, 6, 18);
                g.Disc(10.5, 12, 1.5);
                g.Disc(15, 12, 1.5);
                g.Disc(19.5, 12, 1.5);
                break;

            case Glyph.Resize:
                g.Polyline(g.Stroke, 6, 12, 6, 6, 12, 6);
                g.Polyline(g.Stroke, 24, 18, 24, 24, 18, 24);
                g.Line(9.5, 9.5, 20.5, 20.5, g.Stroke);
                g.Polygon(g.Ink, 8.5, 14.5, 8.5, 8.5, 14.5, 8.5);
                g.Polygon(g.Ink, 21.5, 15.5, 21.5, 21.5, 15.5, 21.5);
                break;

            case Glyph.Shield:
                g.Shape(g.Paper, g.Stroke, 15, 6, 23, 9.4, 23, 16, 15, 24, 7, 16, 7, 9.4);
                g.Polyline(g.Heavy, 11, 14.6, 14, 17.6, 19.4, 11);
                break;

            case Glyph.EqualsSign:
                g.Block(7, 11.7, 16, 2.8);
                g.Block(7, 17.5, 16, 2.8);
                break;

            // ---- conditions -------------------------------------------------------------------
            case Glyph.FilledDisc:
                g.Disc(15, 15, 8.2);
                break;

            case Glyph.VennAnd:
            case Glyph.VennOr:
                Geometry left = IconGeometry.Ellipse(11.6, 15, 6.6);
                Geometry right = IconGeometry.Ellipse(18.4, 15, 6.6);
                g.Draw(
                    g.Ink,
                    null,
                    IconGeometry.Combine(
                        glyph == Glyph.VennAnd ? GeometryCombineMode.Intersect : GeometryCombineMode.Union,
                        left,
                        right));

                // Or fills both circles, so its outlines have to be drawn back out in the interior
                // colour or the whole thing collapses into one anonymous blob.
                Pen outline = glyph == Glyph.VennOr ? g.Knockout : g.Stroke;
                g.Circle(11.6, 15, 6.6, Brushes.Transparent, outline);
                g.Circle(18.4, 15, 6.6, Brushes.Transparent, outline);
                break;

            case Glyph.Greater:
                g.Polyline(g.Heavy, 11, 7.5, 20, 15, 11, 22.5);
                break;

            case Glyph.GreaterOrEqual:
                g.Polyline(g.Heavy, 11, 6.5, 20, 13.5, 11, 20.5);
                g.Block(10, 22.4, 11, 2.4);
                break;

            case Glyph.Less:
                g.Polyline(g.Heavy, 19, 7.5, 10, 15, 19, 22.5);
                break;

            case Glyph.LessOrEqual:
                g.Polyline(g.Heavy, 19, 6.5, 10, 13.5, 19, 20.5);
                g.Block(9, 22.4, 11, 2.4);
                break;

            case Glyph.Inside:
                g.Box(6, 6, 18, 18);
                g.Block(11, 11, 8, 8);
                break;

            case Glyph.Member:
                g.Polyline(g.Stroke, 11, 6.5, 7.5, 6.5, 7.5, 23.5, 11, 23.5);
                g.Polyline(g.Stroke, 19, 6.5, 22.5, 6.5, 22.5, 23.5, 19, 23.5);
                g.Disc(15, 15, 3.2);
                break;

            case Glyph.AnchorStart:
                g.Block(6, 7, 3.6, 16);
                g.Line(12, 15, 24, 15, g.Heavy);
                break;

            case Glyph.AnchorEnd:
                g.Line(6, 15, 18, 15, g.Heavy);
                g.Block(20.4, 7, 3.6, 16);
                break;

            case Glyph.Wildcard:
                // An asterisk with a trailing pair of dots: "anything, then more of it".
                g.Line(13, 7, 13, 17, g.Stroke);
                g.Line(8.7, 9.5, 17.3, 14.5, g.Stroke);
                g.Line(8.7, 14.5, 17.3, 9.5, g.Stroke);
                g.Disc(19, 21.5, 1.8);
                g.Disc(23, 21.5, 1.8);
                break;

            case Glyph.EmptyBox:
            case Glyph.FullBox:
                g.Box(7, 7, 16, 16);
                if (glyph == Glyph.FullBox)
                {
                    g.Block(10.5, 10.6, 11, 2.4);
                    g.Block(10.5, 14.3, 11, 2.4);
                    g.Block(10.5, 18, 7.5, 2.4);
                }

                break;

            // ---- computation ------------------------------------------------------------------
            case Glyph.Operators:
                g.Line(6.5, 11, 15.5, 11, g.Heavy);
                g.Line(11, 6.5, 11, 15.5, g.Heavy);
                g.Line(17.5, 17.5, 24, 24, g.Heavy);
                g.Line(24, 17.5, 17.5, 24, g.Heavy);
                break;

            case Glyph.Sigma:
                g.Polyline(g.Heavy, 22, 7, 8, 7, 15, 15, 8, 23, 22, 23);
                break;

            case Glyph.Braces:
                g.Polyline(g.Stroke, 13, 6.5, 10, 6.5, 10, 13, 7, 15, 10, 17, 10, 23.5, 13, 23.5);
                g.Polyline(g.Stroke, 17, 6.5, 20, 6.5, 20, 13, 23, 15, 20, 17, 20, 23.5, 17, 23.5);
                break;

            case Glyph.FieldRef:
                g.Box(6, 10, 11.5, 10);
                g.Line(17.5, 15, 21, 15, g.Heavy);
                g.Polygon(g.Ink, 20, 11.4, 24.5, 15, 20, 18.6);
                break;

            case Glyph.Pin:
                g.Circle(15, 11.8, 5);
                g.Disc(15, 11.8, 2);
                g.Line(15, 16.8, 15, 24, g.Heavy);
                break;

            case Glyph.Table:
                g.Box(6, 7, 18, 16);
                g.Block(6, 7, 18, 5);
                g.Line(15, 12, 15, 23);
                g.Line(6, 17.5, 24, 17.5);
                break;

            case Glyph.Fork:
                g.Polyline(g.Stroke, 6.5, 15, 13, 15);
                g.Polyline(g.Stroke, 13, 15, 17, 9, 24, 9);
                g.Polyline(g.Stroke, 13, 15, 17, 21, 24, 21);
                g.Disc(13, 15, 2.2);
                break;

            // ---- rules ------------------------------------------------------------------------
            case Glyph.Range:
                g.Line(6.5, 15, 23.5, 15, g.Stroke);
                g.Line(6.5, 9, 6.5, 21, g.Heavy);
                g.Line(23.5, 9, 23.5, 21, g.Heavy);
                g.Block(11, 12.8, 8, 4.4);
                break;

            case Glyph.Ruler:
                g.Box(6, 11, 18, 9);
                g.Line(10, 11, 10, 16);
                g.Line(14, 11, 14, 14);
                g.Line(18, 11, 18, 16);
                g.Line(22, 11, 22, 14);
                break;

            case Glyph.Compare:
                g.Polyline(g.Heavy, 10.5, 6.5, 18.5, 11.5, 10.5, 16.5);
                g.Block(9.5, 20, 12, 2.6);
                break;

            // ---- appearance -------------------------------------------------------------------
            case Glyph.Sun:
                g.Disc(15, 15, 4.8);
                for (int i = 0; i < 8; i++)
                {
                    Point from = IconGeometry.OnCircle(15, 15, 6.8, i * 45d);
                    Point to = IconGeometry.OnCircle(15, 15, 9.4, i * 45d);
                    g.Line(from.X, from.Y, to.X, to.Y, g.Stroke);
                }

                break;

            case Glyph.Moon:
                g.Draw(
                    g.Ink,
                    null,
                    IconGeometry.Combine(
                        GeometryCombineMode.Exclude,
                        IconGeometry.Ellipse(15, 15, 8.6),
                        IconGeometry.Ellipse(20, 11.4, 7.6)));
                break;

            case Glyph.Monitor:
                g.Box(6, 7, 18, 12.5);
                g.Line(15, 19.5, 15, 22.5, g.Heavy);
                g.Line(10, 22.8, 20, 22.8, g.Heavy);
                break;

            case Glyph.Contrast:
                g.Circle(15, 15, 8.2);
                g.Draw(
                    g.Ink,
                    null,
                    IconGeometry.Combine(
                        GeometryCombineMode.Intersect,
                        IconGeometry.Ellipse(15, 15, 8.2),
                        IconGeometry.Rectangle(15, 6, 9, 18)));
                break;

            case Glyph.HardShadow:
                g.Block(12.5, 12.5, 11.5, 11.5);
                g.Box(6, 6, 11.5, 11.5);
                break;

            case Glyph.Palette:
                g.Draw(g.Paper, g.Stroke, PalettePath());
                g.Disc(11, 11.5, 1.7);
                g.Disc(17.5, 10.4, 1.7);
                g.Disc(21, 15.5, 1.7);
                break;

            case Glyph.Swatches:
                g.Box(13.5, 13.5, 10.5, 10.5, g.Ink);
                g.Box(9.75, 9.75, 10.5, 10.5);
                g.Box(6, 6, 10.5, 10.5);
                break;

            // ---- forms and answers ------------------------------------------------------------
            case Glyph.Window:
                g.Box(6, 7, 18, 16);
                g.Block(6, 7, 18, 5);
                g.Disc(9, 9.5, 1.2, g.Paper);
                g.Disc(12.4, 9.5, 1.2, g.Paper);
                break;

            case Glyph.CircleTick:
                g.Circle(15, 15, 8.4);
                g.Polyline(g.Heavy, 10.4, 15.2, 13.6, 18.4, 20, 11);
                break;

            case Glyph.CircleCross:
                g.Circle(15, 15, 8.4);
                g.Line(11.2, 11.2, 18.8, 18.8, g.Heavy);
                g.Line(18.8, 11.2, 11.2, 18.8, g.Heavy);
                break;

            case Glyph.Cursor:
                g.Box(6, 8, 14, 7.5);
                g.Polygon(g.Ink, 13.5, 12.5, 13.5, 24.5, 16.4, 21.6, 18.4, 25.5, 20.6, 24.4, 18.6, 20.6, 22.5, 20.2);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(glyph), glyph, "No drawing for this glyph.");
        }
    }

    /// <summary>Draws the badge, either in the corner or straight across the plate.</summary>
    internal static void DrawBadge(IconGeometry g, Badge badge)
    {
        if (badge == Badge.None)
        {
            return;
        }

        if (badge == Badge.Slash)
        {
            // Negation reads best as a bar right across everything, so it is drawn twice: once
            // thick in the interior colour to knock a channel through the glyph underneath, then
            // thinner on top. Without the knockout it disappears into whatever it crosses.
            g.Line(5, 27, 27, 5, new Pen(g.Paper, 5.4d) { StartLineCap = PenLineCap.Square, EndLineCap = PenLineCap.Square });
            g.Line(5, 27, 27, 5, g.Heavy);
            return;
        }

        g.Box(18.5, 18.5, 11, 11);

        switch (badge)
        {
            case Badge.Tick:
                g.Polyline(g.Stroke, 21.2, 24.2, 23.1, 26.1, 26.8, 21.6);
                break;

            case Badge.Cross:
                g.Line(21.6, 21.6, 26.4, 26.4, g.Stroke);
                g.Line(26.4, 21.6, 21.6, 26.4, g.Stroke);
                break;

            case Badge.Plus:
                g.Line(21, 24, 27, 24, g.Stroke);
                g.Line(24, 21, 24, 27, g.Stroke);
                break;

            case Badge.Play:
                g.Polygon(g.Ink, 22.2, 21, 27, 24, 22.2, 27);
                break;

            case Badge.ArrowOut:
                g.Line(21.2, 26.8, 25.6, 22.4, g.Stroke);
                g.Polygon(g.Ink, 22.6, 21, 27, 21, 27, 25.4);
                break;

            case Badge.ArrowIn:
                g.Line(26.8, 21.2, 22.4, 25.6, g.Stroke);
                g.Polygon(g.Ink, 25.4, 27, 21, 27, 21, 22.6);
                break;

            case Badge.Fork:
                g.Polyline(g.Stroke, 20.8, 24, 23.4, 24);
                g.Polyline(g.Stroke, 23.4, 24, 27, 21.2);
                g.Polyline(g.Stroke, 23.4, 24, 27, 26.8);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(badge), badge, "No drawing for this badge.");
        }
    }

    /// <summary>A tear: two straight shoulders from the point, closed by a half circle.</summary>
    private static Geometry DropletPath()
    {
        PathFigure figure = new() { StartPoint = new Point(15, 5.8), IsClosed = true, IsFilled = true };
        figure.Segments.Add(new LineSegment(new Point(21.6, 16.4), isStroked: true));
        figure.Segments.Add(new ArcSegment(
            new Point(8.4, 16.4),
            new Size(6.6, 6.6),
            0d,
            isLargeArc: false,
            SweepDirection.Clockwise,
            isStroked: true));

        PathGeometry geometry = new();
        geometry.Figures.Add(figure);
        geometry.Freeze();
        return geometry;
    }

    /// <summary>A painter's palette: a rounded blob with a bite taken out at the bottom.</summary>
    private static Geometry PalettePath()
    {
        Geometry blob = new EllipseGeometry(new Point(15, 15), 9, 8.2);
        Geometry thumb = new EllipseGeometry(new Point(14, 20.4), 2.6, 2.6);
        return IconGeometry.Combine(GeometryCombineMode.Exclude, blob, thumb);
    }
}
