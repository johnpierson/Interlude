using System;
using System.Windows;
using System.Windows.Media;

namespace Interlude.Preview;

/// <summary>
/// The drawing primitives every glyph is built from.
///
/// Everything is expressed in a 32-unit square regardless of the pixel size being rendered, so a
/// glyph is written once and comes out right at 32 and at 128. Coordinates in the glyph routines
/// are absolute in that space rather than relative to a passed-in rectangle: absolute numbers are
/// easier to reason about when the whole drawing is eighteen units wide, and the glyph box never
/// moves.
/// </summary>
internal sealed class IconGeometry
{
    /// <summary>The design grid. Every coordinate in this file is in these units.</summary>
    internal const double Grid = 32d;

    /// <summary>The glyph box: the plate inset far enough to clear its own border.</summary>
    internal const double GlyphMin = 6d;
    internal const double GlyphMax = 24d;
    internal const double Centre = 15d;

    private readonly DrawingContext dc;

    internal IconGeometry(DrawingContext context, Brush ink, Brush paper)
    {
        dc = context;
        Ink = ink;
        Paper = paper;

        Stroke = MakePen(ink, 2.1d);
        Heavy = MakePen(ink, 2.8d);
        Hairline = MakePen(ink, 1.6d);
        Knockout = MakePen(paper, 2.1d);
    }

    /// <summary>The outline and fill colour: black on every plate but Form's, which inverts.</summary>
    internal Brush Ink { get; }

    /// <summary>What a glyph's interior is filled with, so a shape reads against a loud plate.</summary>
    internal Brush Paper { get; }

    internal Pen Stroke { get; }

    internal Pen Heavy { get; }

    internal Pen Hairline { get; }

    /// <summary>An outline in the interior colour, for drawing detail back out of a solid fill.</summary>
    internal Pen Knockout { get; }

    /// <summary>
    /// Square caps and mitred joins throughout.
    ///
    /// Round caps would soften every terminal, and softness is the one thing this style has no use
    /// for. It also keeps a horizontal rule the same height end to end at 32 pixels, where half a
    /// pixel of rounding is a visible taper.
    /// </summary>
    private static Pen MakePen(Brush brush, double thickness)
    {
        Pen pen = new(brush, thickness)
        {
            StartLineCap = PenLineCap.Square,
            EndLineCap = PenLineCap.Square,
            LineJoin = PenLineJoin.Miter,
            MiterLimit = 4d,
        };

        pen.Freeze();
        return pen;
    }

    internal void Line(double x1, double y1, double x2, double y2, Pen? pen = null)
        => dc.DrawLine(pen ?? Stroke, new Point(x1, y1), new Point(x2, y2));

    /// <summary>An outlined box. Pass <c>null</c> for <paramref name="fill"/> to leave it hollow.</summary>
    internal void Box(double x, double y, double width, double height, Brush? fill = null, Pen? pen = null)
        => dc.DrawRectangle(fill ?? Paper, pen ?? Stroke, new Rect(x, y, width, height));

    /// <summary>A solid block with no outline: bars, fills, filled quadrants.</summary>
    internal void Block(double x, double y, double width, double height, Brush? fill = null)
        => dc.DrawRectangle(fill ?? Ink, null, new Rect(x, y, width, height));

    internal void Pill(double x, double y, double width, double height, Brush? fill = null)
        => dc.DrawRoundedRectangle(fill ?? Paper, Stroke, new Rect(x, y, width, height), height / 2d, height / 2d);

    internal void Circle(double cx, double cy, double radius, Brush? fill = null, Pen? pen = null)
        => dc.DrawEllipse(fill ?? Paper, pen ?? Stroke, new Point(cx, cy), radius, radius);

    internal void Disc(double cx, double cy, double radius, Brush? fill = null)
        => dc.DrawEllipse(fill ?? Ink, null, new Point(cx, cy), radius, radius);

    /// <summary>An open polyline: chevrons, ticks, brackets, sigma.</summary>
    internal void Polyline(Pen? pen, params double[] coordinates)
        => dc.DrawGeometry(null, pen ?? Stroke, Path(false, coordinates));

    /// <summary>A closed filled polygon with no outline: arrowheads, cursors, mountains.</summary>
    internal void Polygon(Brush? fill, params double[] coordinates)
        => dc.DrawGeometry(fill ?? Ink, null, Path(true, coordinates));

    /// <summary>A closed outlined shape: shields, folders, document pages.</summary>
    internal void Shape(Brush? fill, Pen? pen, params double[] coordinates)
        => dc.DrawGeometry(fill ?? Paper, pen ?? Stroke, Path(true, coordinates));

    internal void Draw(Brush? fill, Pen? pen, Geometry geometry)
        => dc.DrawGeometry(fill, pen, geometry);

    /// <summary>
    /// An arc between two angles about a centre, as an open stroked path. Angles are in degrees,
    /// measured clockwise from three o'clock, because y grows downwards here.
    /// </summary>
    internal void Arc(double cx, double cy, double radius, double fromDegrees, double toDegrees, Pen? pen = null)
    {
        Point start = OnCircle(cx, cy, radius, fromDegrees);
        Point end = OnCircle(cx, cy, radius, toDegrees);

        PathFigure figure = new() { StartPoint = start, IsClosed = false, IsFilled = false };
        figure.Segments.Add(new ArcSegment(
            end,
            new Size(radius, radius),
            0d,
            Math.Abs(toDegrees - fromDegrees) > 180d,
            toDegrees > fromDegrees ? SweepDirection.Clockwise : SweepDirection.Counterclockwise,
            isStroked: true));

        PathGeometry geometry = new();
        geometry.Figures.Add(figure);
        geometry.Freeze();

        dc.DrawGeometry(null, pen ?? Stroke, geometry);
    }

    internal static Point OnCircle(double cx, double cy, double radius, double degrees)
    {
        double radians = degrees * Math.PI / 180d;
        return new Point(cx + (radius * Math.Cos(radians)), cy + (radius * Math.Sin(radians)));
    }

    internal static Geometry Ellipse(double cx, double cy, double radius)
    {
        EllipseGeometry geometry = new(new Point(cx, cy), radius, radius);
        geometry.Freeze();
        return geometry;
    }

    internal static Geometry Rectangle(double x, double y, double width, double height)
    {
        RectangleGeometry geometry = new(new Rect(x, y, width, height));
        geometry.Freeze();
        return geometry;
    }

    internal static Geometry Combine(GeometryCombineMode mode, Geometry first, Geometry second)
    {
        CombinedGeometry geometry = new(mode, first, second);
        geometry.Freeze();
        return geometry;
    }

    internal static Geometry Path(bool closed, params double[] coordinates)
    {
        if (coordinates.Length < 4 || coordinates.Length % 2 != 0)
        {
            throw new ArgumentException("A path needs at least two points, as x,y pairs.", nameof(coordinates));
        }

        PathFigure figure = new()
        {
            StartPoint = new Point(coordinates[0], coordinates[1]),
            IsClosed = closed,
            IsFilled = closed,
        };

        for (int i = 2; i < coordinates.Length; i += 2)
        {
            figure.Segments.Add(new LineSegment(new Point(coordinates[i], coordinates[i + 1]), isStroked: true));
        }

        PathGeometry geometry = new();
        geometry.Figures.Add(figure);
        geometry.Freeze();
        return geometry;
    }

    /// <summary>A quadratic curve pair, used for the eye and the droplet.</summary>
    internal static Geometry Curve(bool closed, Point start, params Point[] controlsAndPoints)
    {
        PathFigure figure = new() { StartPoint = start, IsClosed = closed, IsFilled = closed };

        for (int i = 0; i + 1 < controlsAndPoints.Length; i += 2)
        {
            figure.Segments.Add(new QuadraticBezierSegment(
                controlsAndPoints[i], controlsAndPoints[i + 1], isStroked: true));
        }

        PathGeometry geometry = new();
        geometry.Figures.Add(figure);
        geometry.Freeze();
        return geometry;
    }
}
