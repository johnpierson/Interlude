using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Autodesk.DesignScript.Runtime;
using Interlude.Model;

namespace Interlude.Rendering.Wpf;

/// <summary>
/// The single boundary where Interlude's own presentation values become WPF ones.
///
/// Keeping every conversion here is what lets the model define its own colour, spacing and
/// alignment types: the cost of not using <c>System.Windows.Media.Color</c> everywhere is this
/// one file, and the benefit is a model that serializes, tests and evolves without a UI stack.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
public static class WpfConversions
{
    /// <summary>Converts to a WPF colour.</summary>
    public static Color ToColor(this RgbColor color)
        => Color.FromArgb(color.Alpha, color.Red, color.Green, color.Blue);

    /// <summary>Converts to a frozen brush. Frozen brushes are cheaper and thread-safe to share.</summary>
    public static SolidColorBrush ToBrush(this RgbColor color)
    {
        SolidColorBrush brush = new(color.ToColor());
        brush.Freeze();
        return brush;
    }

    /// <summary>Converts to WPF's thickness.</summary>
    public static Thickness ToThickness(this Edges edges)
        => new(edges.Left, edges.Top, edges.Right, edges.Bottom);

    /// <summary>Converts to WPF's horizontal alignment.</summary>
    public static HorizontalAlignment ToWpf(this HorizontalPlacement placement) => placement switch
    {
        HorizontalPlacement.Left => HorizontalAlignment.Left,
        HorizontalPlacement.Center => HorizontalAlignment.Center,
        HorizontalPlacement.Right => HorizontalAlignment.Right,
        _ => HorizontalAlignment.Stretch,
    };

    /// <summary>Converts to WPF's vertical alignment.</summary>
    public static VerticalAlignment ToWpf(this VerticalPlacement placement) => placement switch
    {
        VerticalPlacement.Top => VerticalAlignment.Top,
        VerticalPlacement.Center => VerticalAlignment.Center,
        VerticalPlacement.Bottom => VerticalAlignment.Bottom,
        _ => VerticalAlignment.Stretch,
    };

    /// <summary>Converts to WPF's orientation.</summary>
    public static Orientation ToWpf(this LayoutOrientation orientation)
        => orientation == LayoutOrientation.Horizontal ? Orientation.Horizontal : Orientation.Vertical;

    /// <summary>Converts to WPF's dock side.</summary>
    public static Dock ToWpf(this DockSide side) => side switch
    {
        DockSide.Top => Dock.Top,
        DockSide.Right => Dock.Right,
        DockSide.Bottom => Dock.Bottom,
        _ => Dock.Left,
    };

    /// <summary>Converts to WPF's font weight.</summary>
    public static FontWeight ToWpf(this TextWeight weight) => weight switch
    {
        TextWeight.Medium => FontWeights.Medium,
        TextWeight.SemiBold => FontWeights.SemiBold,
        TextWeight.Bold => FontWeights.Bold,
        _ => FontWeights.Normal,
    };

    /// <summary>Converts to WPF's image stretch.</summary>
    public static Stretch ToWpf(this ImageFit fit) => fit switch
    {
        ImageFit.Cover => Stretch.UniformToFill,
        ImageFit.Fill => Stretch.Fill,
        ImageFit.None => Stretch.None,
        _ => Stretch.Uniform,
    };

    /// <summary>Converts to WPF's grid sizing.</summary>
    public static GridLength ToWpf(this GridTrack track) => track.Kind switch
    {
        GridTrackKind.Pixel => new GridLength(track.Value, GridUnitType.Pixel),
        GridTrackKind.Star => new GridLength(track.Value <= 0d ? 1d : track.Value, GridUnitType.Star),
        _ => GridLength.Auto,
    };

    /// <summary>
    /// Applies an element's style overrides to the control that was built for it. Anything the
    /// style leaves unset is left alone, so the theme keeps its say.
    /// </summary>
    public static void ApplyStyle(this FrameworkElement target, ElementStyle? style)
    {
        if (style is null || target is null)
        {
            return;
        }

        if (style.Width.HasValue)
        {
            target.Width = style.Width.Value;
        }

        if (style.Height.HasValue)
        {
            target.Height = style.Height.Value;
        }

        if (style.MinWidth.HasValue)
        {
            target.MinWidth = style.MinWidth.Value;
        }

        if (style.MinHeight.HasValue)
        {
            target.MinHeight = style.MinHeight.Value;
        }

        if (style.MaxWidth.HasValue)
        {
            target.MaxWidth = style.MaxWidth.Value;
        }

        if (style.MaxHeight.HasValue)
        {
            target.MaxHeight = style.MaxHeight.Value;
        }

        if (style.Margin.HasValue)
        {
            target.Margin = style.Margin.Value.ToThickness();
        }

        if (style.HorizontalPlacement.HasValue)
        {
            target.HorizontalAlignment = style.HorizontalPlacement.Value.ToWpf();
        }

        if (style.VerticalPlacement.HasValue)
        {
            target.VerticalAlignment = style.VerticalPlacement.Value.ToWpf();
        }

        if (target is Control control)
        {
            if (style.Padding.HasValue)
            {
                control.Padding = style.Padding.Value.ToThickness();
            }

            if (style.FontSize.HasValue)
            {
                control.FontSize = style.FontSize.Value;
            }

            if (style.FontWeight.HasValue)
            {
                control.FontWeight = style.FontWeight.Value.ToWpf();
            }

            if (!string.IsNullOrWhiteSpace(style.FontFamily))
            {
                control.FontFamily = new FontFamily(style.FontFamily);
            }

            if (style.Foreground.HasValue)
            {
                control.Foreground = style.Foreground.Value.ToBrush();
            }

            if (style.Background.HasValue)
            {
                control.Background = style.Background.Value.ToBrush();
            }
        }
        else if (target is TextBlock textBlock)
        {
            if (style.Padding.HasValue)
            {
                textBlock.Padding = style.Padding.Value.ToThickness();
            }

            if (style.FontSize.HasValue)
            {
                textBlock.FontSize = style.FontSize.Value;
            }

            if (style.FontWeight.HasValue)
            {
                textBlock.FontWeight = style.FontWeight.Value.ToWpf();
            }

            if (style.Foreground.HasValue)
            {
                textBlock.Foreground = style.Foreground.Value.ToBrush();
            }
        }
        else if (target is Panel panel && style.Background.HasValue)
        {
            panel.Background = style.Background.Value.ToBrush();
        }

        // Grid and Dock placement are attached properties, so they are set unconditionally:
        // the parent container ignores them when it is not a Grid or a DockPanel.
        if (style.GridRow.HasValue)
        {
            Grid.SetRow(target, style.GridRow.Value);
        }

        if (style.GridColumn.HasValue)
        {
            Grid.SetColumn(target, style.GridColumn.Value);
        }

        if (style.GridRowSpan.HasValue)
        {
            Grid.SetRowSpan(target, style.GridRowSpan.Value);
        }

        if (style.GridColumnSpan.HasValue)
        {
            Grid.SetColumnSpan(target, style.GridColumnSpan.Value);
        }

        if (style.Dock.HasValue)
        {
            DockPanel.SetDock(target, style.Dock.Value.ToWpf());
        }
    }
}
