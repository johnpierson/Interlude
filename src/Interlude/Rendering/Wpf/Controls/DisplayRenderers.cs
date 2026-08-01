using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Autodesk.DesignScript.Runtime;
using Interlude.Model;
using Interlude.Runtime;
using Interlude.Theming;

namespace Interlude.Rendering.Wpf.Controls;

/// <summary>Static text, optionally as a heading.</summary>
[IsVisibleInDynamoLibrary(false)]
internal sealed class LabelRenderer : ControlRenderer<LabelElement>
{
    public override bool UsesFieldChrome => false;

    protected override FrameworkElement BuildCore(LabelElement element, RenderContext context)
    {
        TextBlock text = new()
        {
            Text = element.Text,
            TextWrapping = element.WrapText ? TextWrapping.Wrap : TextWrapping.NoWrap,
            Margin = new Thickness(0, 0, 0, context.Spacing / 2d),
        };

        text.SetResourceReference(
            TextBlock.ForegroundProperty,
            element.IsMuted ? ThemeKeys.ForegroundMuted : ThemeKeys.Foreground);

        if (element.HeadingLevel > 0)
        {
            // Four steps of scale, flattening as the level rises, so an h3 and an h4 stay
            // distinguishable without an h1 dominating a small dialog.
            double scale = element.HeadingLevel switch
            {
                1 => 1.6d,
                2 => 1.35d,
                3 => 1.15d,
                _ => 1.05d,
            };

            text.FontSize = context.Theme.FontSize * scale;
            text.SetResourceReference(TextBlock.FontWeightProperty, ThemeKeys.HeadingFontWeight);
            text.Margin = new Thickness(0, context.Spacing / 2d, 0, context.Spacing / 2d);

            // After the font size, never before: the tracking is a fraction of it. Headings take
            // the micro-label treatment; body text never does, because letter spacing hurts
            // readability over more than a few words.
            HeaderText.Apply(text, element.Text, context);
        }

        return text;
    }
}

/// <summary>A block of lightweight Markdown.</summary>
[IsVisibleInDynamoLibrary(false)]
internal sealed class MarkdownRenderer : ControlRenderer<MarkdownElement>
{
    public override bool UsesFieldChrome => false;

    protected override FrameworkElement BuildCore(MarkdownElement element, RenderContext context)
        => MarkdownView.Build(element.Text, context);
}

/// <summary>A picture from a path or from bytes.</summary>
[IsVisibleInDynamoLibrary(false)]
internal sealed class ImageRenderer : ControlRenderer<ImageElement>
{
    public override bool UsesFieldChrome => false;

    protected override FrameworkElement BuildCore(ImageElement element, RenderContext context)
    {
        BitmapImage? source = LoadImage(element);

        if (source is null)
        {
            TextBlock missing = new()
            {
                Text = element.AlternateText ?? "(image unavailable)",
                FontStyle = FontStyles.Italic,
            };
            missing.SetResourceReference(TextBlock.ForegroundProperty, ThemeKeys.ForegroundMuted);
            return missing;
        }

        Image image = new()
        {
            Source = source,
            Stretch = element.Fit.ToWpf(),
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 0, 0, context.Spacing / 2d),
        };

        if (!string.IsNullOrWhiteSpace(element.AlternateText))
        {
            System.Windows.Automation.AutomationProperties.SetName(image, element.AlternateText);
        }

        return image;
    }

    /// <summary>
    /// Loads eagerly and closes the stream: leaving a BitmapImage attached to a file would keep
    /// a lock on it for as long as the dialog is open, which surprises anyone trying to
    /// overwrite the image from the same graph.
    /// </summary>
    private static BitmapImage? LoadImage(ImageElement element)
    {
        try
        {
            if (element.Bytes is { Length: > 0 })
            {
                using MemoryStream memory = new(element.Bytes);
                return Decode(memory);
            }

            if (!string.IsNullOrWhiteSpace(element.Path) && File.Exists(element.Path))
            {
                using FileStream file = File.OpenRead(element.Path!);
                return Decode(file);
            }
        }
        catch (Exception ex) when (ex is IOException or NotSupportedException or UnauthorizedAccessException or ArgumentException)
        {
            // A broken image is a missing picture, not a broken form.
            return null;
        }

        return null;
    }

    private static BitmapImage Decode(Stream stream)
    {
        BitmapImage bitmap = new();
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.StreamSource = stream;
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }
}

/// <summary>A dividing line, optionally captioned.</summary>
[IsVisibleInDynamoLibrary(false)]
internal sealed class SeparatorRenderer : ControlRenderer<SeparatorElement>
{
    public override bool UsesFieldChrome => false;

    protected override FrameworkElement BuildCore(SeparatorElement element, RenderContext context)
    {
        if (element.Orientation == LayoutOrientation.Vertical)
        {
            Border vertical = new()
            {
                Width = context.Theme.BorderWidth,
                Margin = new Thickness(context.Spacing, 0, context.Spacing, 0),
            };
            vertical.SetResourceReference(Border.BackgroundProperty, ThemeKeys.Border);
            return vertical;
        }

        Border line = new()
        {
            Height = context.Theme.BorderWidth,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, context.Spacing, 0, context.Spacing),
        };
        line.SetResourceReference(Border.BackgroundProperty, ThemeKeys.Border);

        if (string.IsNullOrWhiteSpace(element.Caption))
        {
            return line;
        }

        Grid captioned = new() { Margin = new Thickness(0, context.Spacing, 0, context.Spacing / 2d) };
        captioned.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        captioned.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        TextBlock caption = new()
        {
            Text = element.Caption,
            Margin = new Thickness(0, 0, context.Spacing, 0),
        };
        caption.SetResourceReference(TextBlock.FontWeightProperty, ThemeKeys.HeadingFontWeight);
        caption.SetResourceReference(TextBlock.ForegroundProperty, ThemeKeys.ForegroundMuted);

        line.Margin = new Thickness(0);
        Grid.SetColumn(caption, 0);
        Grid.SetColumn(line, 1);

        captioned.Children.Add(caption);
        captioned.Children.Add(line);
        return captioned;
    }
}

/// <summary>Blank space.</summary>
[IsVisibleInDynamoLibrary(false)]
internal sealed class SpacerRenderer : ControlRenderer<SpacerElement>
{
    public override bool UsesFieldChrome => false;

    protected override FrameworkElement BuildCore(SpacerElement element, RenderContext context)
        => new Border { Height = element.Size, Width = element.Size, Focusable = false };
}

/// <summary>A progress bar.</summary>
[IsVisibleInDynamoLibrary(false)]
internal sealed class ProgressRenderer : ControlRenderer<ProgressElement>
{
    public override bool UsesFieldChrome => false;

    protected override FrameworkElement BuildCore(ProgressElement element, RenderContext context)
    {
        StackPanel host = new()
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 0, 0, context.Spacing / 2d),
        };

        if (element.Segments > 0 && !element.IsIndeterminate)
        {
            host.Children.Add(BuildSegments(element, context));
        }
        else
        {
            host.Children.Add(new ProgressBar
            {
                Minimum = element.Minimum,
                Maximum = element.Maximum,
                Value = element.Value,
                IsIndeterminate = element.IsIndeterminate,

                // The bar is outlined, so its height has to grow with the outline or a heavy
                // theme leaves nothing but border where the fill should be.
                Height = 8d + (context.Theme.BorderWidth * 2d),
                MinWidth = 120,
                VerticalAlignment = VerticalAlignment.Center,
            });
        }

        if (element.ShowPercentage && !element.IsIndeterminate)
        {
            double span = element.Maximum - element.Minimum;
            double fraction = span <= 0d ? 0d : (element.Value - element.Minimum) / span;

            TextBlock readout = new()
            {
                Text = fraction.ToString("P0", CultureInfo.CurrentCulture),
                Margin = new Thickness(8, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
            };
            readout.SetResourceReference(TextBlock.ForegroundProperty, ThemeKeys.ForegroundMuted);
            host.Children.Add(readout);
        }

        return host;
    }

    /// <summary>
    /// Draws the bar as discrete cells. Cells are filled by rounding, not by proportion: five of
    /// seven days is five full cells, because a partly-filled cell would invite the reader to
    /// wonder what a partial day was.
    /// </summary>
    private static FrameworkElement BuildSegments(ProgressElement element, RenderContext context)
    {
        int count = Math.Min(60, element.Segments);
        double span = element.Maximum - element.Minimum;
        double fraction = span <= 0d ? 0d : (element.Value - element.Minimum) / span;
        int filled = (int)Math.Round(Math.Clamp(fraction, 0d, 1d) * count, MidpointRounding.AwayFromZero);

        StackPanel cells = new()
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
        };

        for (int i = 0; i < count; i++)
        {
            Border cell = new()
            {
                Width = 14,
                Height = 10,
                Margin = new Thickness(0, 0, 3, 0),
            };

            cell.SetResourceReference(Border.CornerRadiusProperty, ThemeKeys.SmallCornerRadius);
            cell.SetResourceReference(Border.BorderThicknessProperty, ThemeKeys.BorderThickness);
            cell.SetResourceReference(Border.BorderBrushProperty, ThemeKeys.BorderStrong);
            cell.SetResourceReference(
                Border.BackgroundProperty,
                i < filled ? ThemeKeys.Accent : ThemeKeys.ControlBackground);

            cells.Children.Add(cell);
        }

        return cells;
    }
}

/// <summary>A button inside the form body.</summary>
[IsVisibleInDynamoLibrary(false)]
internal sealed class ButtonRenderer : ControlRenderer<ButtonElement>
{
    public override bool UsesFieldChrome => false;

    protected override FrameworkElement BuildCore(ButtonElement element, RenderContext context)
    {
        Button button = new()
        {
            Content = element.Text,
            MinWidth = 88,
            MinHeight = context.ControlHeight,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 0, 0, context.Spacing / 2d),
        };

        if (element.IsPrimary)
        {
            button.SetResourceReference(FrameworkElement.StyleProperty, "Interlude.PrimaryButton");
        }

        button.Click += (_, _) => context.RequestAction(
            element.Action,
            string.IsNullOrEmpty(element.Tag) ? element.Text : element.Tag,
            element.Url);

        return button;
    }
}

/// <summary>
/// Draws an element no renderer claims.
///
/// A form containing one control this build does not know about is still worth showing: the
/// other twenty fields work, and the placeholder says exactly what is missing. Throwing instead
/// would turn "this graph needs a newer Interlude" into "this graph is broken".
/// </summary>
[IsVisibleInDynamoLibrary(false)]
internal sealed class FallbackRenderer : IControlRenderer
{
    public Type ElementType => typeof(FormElement);

    public bool UsesFieldChrome => false;

    public FrameworkElement Build(FormElement element, RenderContext context)
    {
        Border frame = new()
        {
            CornerRadius = new CornerRadius(3),
            Padding = new Thickness(8),
            Margin = new Thickness(0, 0, 0, context.Spacing / 2d),
        };
        frame.SetResourceReference(Border.BorderThicknessProperty, ThemeKeys.BorderThickness);
        frame.SetResourceReference(Border.BorderBrushProperty, ThemeKeys.Warning);

        StackPanel body = new();

        TextBlock heading = new()
        {
            Text = string.IsNullOrWhiteSpace(element.Label)
                ? $"Unsupported control: {element.GetType().Name}"
                : element.Label,
            TextWrapping = TextWrapping.Wrap,
        };
        heading.SetResourceReference(TextBlock.FontWeightProperty, ThemeKeys.HeadingFontWeight);
        heading.SetResourceReference(TextBlock.ForegroundProperty, ThemeKeys.Warning);

        TextBlock detail = new()
        {
            Text = $"This build of Interlude has no renderer for {element.GetType().Name}. " +
                   "Update the package to see this control.",
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 2, 0, 0),
        };
        detail.SetResourceReference(TextBlock.ForegroundProperty, ThemeKeys.ForegroundMuted);

        body.Children.Add(heading);
        body.Children.Add(detail);
        frame.Child = body;
        return frame;
    }

    public void ApplyState(FrameworkElement control, ElementRuntimeState state)
        => control.IsEnabled = state.IsEnabled;

    public object? ReadValue(FrameworkElement control) => null;

    public void WriteValue(FrameworkElement control, object? value)
    {
    }
}
