using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Autodesk.DesignScript.Runtime;
using Interlude.Theming;

namespace Interlude.Rendering.Wpf;

/// <summary>
/// Builds the header text for sections, cards, tabs and headings, applying the theme's
/// "micro-label" treatment: capitals, and space between the letters.
///
/// About that spacing: <b>WPF has no letter-spacing.</b> There is no property for it on
/// <see cref="TextBlock"/>, on <see cref="Run"/>, or anywhere in the text stack. The options are
/// to lay out one <see cref="TextBlock"/> per character — exact, but it breaks wrapping and text
/// selection — or to insert a thin space between characters and scale its font size. This takes
/// the second: the spacing is approximate to within a fraction of a pixel, and in exchange the
/// header stays a single wrappable, selectable run of text.
///
/// Headers only. Body text is left alone, where tracking would hurt readability rather than help
/// it.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
internal static class HeaderText
{
    /// <summary>
    /// A thin space is about a fifth of an em wide, so a spacer at five times the wanted gap
    /// renders at roughly that gap. Approximate by design; see the class remarks.
    /// </summary>
    private const double ThinSpaceEmWidth = 0.2d;

    /// <summary>Applies the theme's capitalisation to a header, without touching its spacing.</summary>
    internal static string Transform(string? text, ThemeDefinition theme)
    {
        string value = text ?? string.Empty;

        // Culture-aware on purpose: this is display text a person reads, not a key. Turkish
        // capitalises "i" as "İ", and a reader on a Turkish machine expects to see that.
        return theme.UppercaseHeaders ? value.ToUpper(CultureInfo.CurrentCulture) : value;
    }

    /// <summary>
    /// Builds a header as a value suitable for a <c>Header</c> property. Returns a plain string
    /// when no tracking is wanted, so the common case stays a simple content presenter.
    /// </summary>
    internal static object Build(string? text, RenderContext context)
    {
        string value = Transform(text, context.Theme);

        if (context.Theme.HeaderTracking <= 0d)
        {
            return value;
        }

        TextBlock block = new() { TextWrapping = TextWrapping.Wrap };
        Fill(block, value, context);
        return block;
    }

    /// <summary>Applies the treatment to an existing text block, replacing its content.</summary>
    internal static void Apply(TextBlock block, string? text, RenderContext context)
    {
        string value = Transform(text, context.Theme);

        if (context.Theme.HeaderTracking <= 0d)
        {
            block.Text = value;
            return;
        }

        block.Text = string.Empty;
        Fill(block, value, context);
    }

    private static void Fill(TextBlock block, string value, RenderContext context)
    {
        double fontSize = block.FontSize > 0d && !double.IsNaN(block.FontSize)
            ? block.FontSize
            : context.Theme.FontSize;

        double spacerSize = context.Theme.HeaderTracking * fontSize / ThinSpaceEmWidth;

        for (int i = 0; i < value.Length; i++)
        {
            block.Inlines.Add(new Run(value[i].ToString()));

            // No trailing spacer: it would push a centred header off centre.
            if (i < value.Length - 1)
            {
                block.Inlines.Add(new Run(" ") { FontSize = spacerSize });
            }
        }
    }
}
