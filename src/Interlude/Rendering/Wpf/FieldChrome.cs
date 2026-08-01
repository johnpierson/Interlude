using System.Windows;
using System.Windows.Controls;
using Autodesk.DesignScript.Runtime;
using Interlude.Model;
using Interlude.Theming;

namespace Interlude.Rendering.Wpf;

/// <summary>
/// Draws the furniture around an input: its label, its required marker, its help text and its
/// error line.
///
/// Doing this once, here, is what makes a form built from twenty different controls look like
/// one form. It also means a new control gets correct labelling, alignment and error display for
/// free — its renderer only has to produce the control itself.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
internal static class FieldChrome
{
    /// <summary>The pieces of chrome the view needs to keep hold of.</summary>
    [IsVisibleInDynamoLibrary(false)]
    public readonly struct Result
    {
        internal Result(FrameworkElement root, TextBlock? errorText, UIElement? requiredMarker)
        {
            Root = root;
            ErrorText = errorText;
            RequiredMarker = requiredMarker;
        }

        /// <summary>The outermost visual, which is what gets hidden when the field is not shown.</summary>
        public FrameworkElement Root { get; }

        public TextBlock? ErrorText { get; }

        public UIElement? RequiredMarker { get; }
    }

    /// <summary>Wraps a control in its label, help text and error line.</summary>
    public static Result Wrap(FormElement element, FrameworkElement control, RenderContext context)
    {
        double labelWidth = element.Style?.LabelWidth ?? context.Theme.LabelWidth;
        bool hasLabel = !string.IsNullOrWhiteSpace(element.Label);

        StackPanel body = new() { Orientation = Orientation.Vertical };
        body.Children.Add(control);

        if (!string.IsNullOrWhiteSpace(element.HelpText))
        {
            body.Children.Add(BuildHelpText(element.HelpText!, context));
        }

        TextBlock errorText = BuildErrorText(context);
        body.Children.Add(errorText);

        if (!hasLabel)
        {
            body.Margin = new Thickness(0, 0, 0, context.Spacing);
            return new Result(body, errorText, null);
        }

        // A zero label column means the author asked for stacked labels, which is the better
        // shape for narrow forms and for long captions.
        if (labelWidth <= 0d)
        {
            StackPanel stacked = new()
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(0, 0, 0, context.Spacing),
            };

            (FrameworkElement label, UIElement marker) = BuildLabel(element, context, stackedAbove: true);
            stacked.Children.Add(label);
            stacked.Children.Add(body);

            return new Result(stacked, errorText, marker);
        }

        Grid row = new() { Margin = new Thickness(0, 0, 0, context.Spacing) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(labelWidth) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        (FrameworkElement sideLabel, UIElement sideMarker) = BuildLabel(element, context, stackedAbove: false);
        Grid.SetColumn(sideLabel, 0);
        Grid.SetColumn(body, 1);

        row.Children.Add(sideLabel);
        row.Children.Add(body);

        return new Result(row, errorText, sideMarker);
    }

    private static (FrameworkElement Label, UIElement Marker) BuildLabel(
        FormElement element,
        RenderContext context,
        bool stackedAbove)
    {
        TextBlock marker = new()
        {
            Text = " *",
            Visibility = Visibility.Collapsed,
            VerticalAlignment = VerticalAlignment.Center,
        };
        marker.SetResourceReference(TextBlock.ForegroundProperty, ThemeKeys.Error);

        TextBlock caption = new()
        {
            Text = element.Label,
            TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center,
        };
        caption.SetResourceReference(TextBlock.ForegroundProperty, ThemeKeys.Foreground);

        StackPanel panel = new()
        {
            Orientation = Orientation.Horizontal,

            // Beside the control, the caption is nudged down so it sits on the control's first
            // line rather than floating against the top of a multi-line field.
            Margin = stackedAbove
                ? new Thickness(0, 0, 0, context.Spacing / 2d)
                : new Thickness(0, 4, context.Spacing, 0),
            VerticalAlignment = stackedAbove ? VerticalAlignment.Center : VerticalAlignment.Top,
        };

        panel.Children.Add(caption);
        panel.Children.Add(marker);

        return (panel, marker);
    }

    private static TextBlock BuildHelpText(string text, RenderContext context)
    {
        TextBlock help = new()
        {
            Text = text,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 2, 0, 0),
        };

        help.SetResourceReference(TextBlock.ForegroundProperty, ThemeKeys.ForegroundMuted);
        help.SetResourceReference(TextBlock.FontSizeProperty, ThemeKeys.FontSizeSmall);
        return help;
    }

    private static TextBlock BuildErrorText(RenderContext context)
    {
        TextBlock error = new()
        {
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 2, 0, 0),
            Visibility = Visibility.Collapsed,
        };

        error.SetResourceReference(TextBlock.ForegroundProperty, ThemeKeys.Error);
        error.SetResourceReference(TextBlock.FontSizeProperty, ThemeKeys.FontSizeSmall);
        return error;
    }
}
