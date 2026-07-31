using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Navigation;
using Autodesk.DesignScript.Runtime;
using Interlude.Theming;

namespace Interlude.Rendering.Wpf.Controls;

/// <summary>
/// Renders a small, predictable subset of Markdown: headings, paragraphs, bullet and numbered
/// lists, horizontal rules, and inline bold, italic, code and links.
///
/// This is deliberately not CommonMark. A full implementation is either a dependency — which
/// this package does not take — or several thousand lines to support nested block quotes and
/// reference links inside a dialog that mostly needs a bold word and a link to a wiki page.
/// Anything unrecognised renders as its own literal text, so nothing is ever silently swallowed.
/// </summary>
[IsVisibleInDynamoLibrary(false)]
internal static class MarkdownView
{
    /// <summary>Builds the visual for a block of Markdown.</summary>
    internal static FrameworkElement Build(string? markdown, RenderContext context)
    {
        StackPanel panel = new() { Margin = new Thickness(0, 0, 0, context.Spacing / 2d) };

        if (string.IsNullOrWhiteSpace(markdown))
        {
            return panel;
        }

        string[] lines = markdown!.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n');

        List<string> paragraph = new();
        int orderedIndex = 0;

        void FlushParagraph()
        {
            if (paragraph.Count == 0)
            {
                return;
            }

            panel.Children.Add(BuildParagraph(string.Join(" ", paragraph), context));
            paragraph.Clear();
        }

        foreach (string raw in lines)
        {
            string line = raw.TrimEnd();
            string trimmed = line.TrimStart();

            if (trimmed.Length == 0)
            {
                FlushParagraph();
                orderedIndex = 0;
                continue;
            }

            if (IsHorizontalRule(trimmed))
            {
                FlushParagraph();
                orderedIndex = 0;
                panel.Children.Add(BuildRule(context));
                continue;
            }

            if (TryReadHeading(trimmed, out int level, out string headingText))
            {
                FlushParagraph();
                orderedIndex = 0;
                panel.Children.Add(BuildHeading(headingText, level, context));
                continue;
            }

            if (TryReadBullet(trimmed, out string bulletText))
            {
                FlushParagraph();
                orderedIndex = 0;
                panel.Children.Add(BuildListItem("•", bulletText, line.Length - trimmed.Length, context));
                continue;
            }

            if (TryReadOrdered(trimmed, out string orderedText))
            {
                FlushParagraph();
                orderedIndex++;
                string marker = orderedIndex.ToString(CultureInfo.CurrentCulture) + ".";
                panel.Children.Add(BuildListItem(marker, orderedText, line.Length - trimmed.Length, context));
                continue;
            }

            orderedIndex = 0;
            paragraph.Add(trimmed);
        }

        FlushParagraph();
        return panel;
    }

    private static bool IsHorizontalRule(string line)
        => line.Length >= 3 &&
           (line.TrimEnd('-').Length == 0 || line.TrimEnd('*').Length == 0 || line.TrimEnd('_').Length == 0);

    private static bool TryReadHeading(string line, out int level, out string text)
    {
        level = 0;
        while (level < line.Length && line[level] == '#')
        {
            level++;
        }

        if (level is < 1 or > 6 || level >= line.Length || line[level] != ' ')
        {
            level = 0;
            text = string.Empty;
            return false;
        }

        text = line.Substring(level + 1).Trim();
        return true;
    }

    private static bool TryReadBullet(string line, out string text)
    {
        if (line.Length > 2 && (line[0] == '-' || line[0] == '*' || line[0] == '+') && line[1] == ' ')
        {
            text = line.Substring(2).Trim();
            return true;
        }

        text = string.Empty;
        return false;
    }

    private static bool TryReadOrdered(string line, out string text)
    {
        int digits = 0;
        while (digits < line.Length && char.IsDigit(line[digits]))
        {
            digits++;
        }

        if (digits > 0 && digits + 1 < line.Length && line[digits] == '.' && line[digits + 1] == ' ')
        {
            text = line.Substring(digits + 2).Trim();
            return true;
        }

        text = string.Empty;
        return false;
    }

    private static FrameworkElement BuildHeading(string text, int level, RenderContext context)
    {
        double scale = level switch
        {
            1 => 1.5d,
            2 => 1.3d,
            3 => 1.15d,
            _ => 1.05d,
        };

        TextBlock heading = new()
        {
            FontSize = context.Theme.FontSize * scale,
            FontWeight = FontWeights.SemiBold,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, context.Spacing, 0, context.Spacing / 3d),
        };

        heading.SetResourceReference(TextBlock.ForegroundProperty, ThemeKeys.Foreground);
        AppendInlines(heading.Inlines, text, context);
        return heading;
    }

    private static FrameworkElement BuildParagraph(string text, RenderContext context)
    {
        TextBlock block = new()
        {
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, context.Spacing / 2d),
        };

        block.SetResourceReference(TextBlock.ForegroundProperty, ThemeKeys.Foreground);
        AppendInlines(block.Inlines, text, context);
        return block;
    }

    private static FrameworkElement BuildListItem(string marker, string text, int indent, RenderContext context)
    {
        Grid row = new() { Margin = new Thickness((indent / 2) * 16d, 0, 0, 2) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(20) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        TextBlock bullet = new() { Text = marker, VerticalAlignment = VerticalAlignment.Top };
        bullet.SetResourceReference(TextBlock.ForegroundProperty, ThemeKeys.ForegroundMuted);

        TextBlock body = new() { TextWrapping = TextWrapping.Wrap };
        body.SetResourceReference(TextBlock.ForegroundProperty, ThemeKeys.Foreground);
        AppendInlines(body.Inlines, text, context);

        Grid.SetColumn(bullet, 0);
        Grid.SetColumn(body, 1);
        row.Children.Add(bullet);
        row.Children.Add(body);
        return row;
    }

    private static FrameworkElement BuildRule(RenderContext context)
    {
        Border rule = new()
        {
            Height = 1,
            Margin = new Thickness(0, context.Spacing / 2d, 0, context.Spacing / 2d),
        };

        rule.SetResourceReference(Border.BackgroundProperty, ThemeKeys.Border);
        return rule;
    }

    /// <summary>
    /// Parses inline markup in one pass. Unclosed markers are emitted literally rather than
    /// swallowing the rest of the line, which is what makes a stray asterisk harmless.
    /// </summary>
    private static void AppendInlines(InlineCollection target, string text, RenderContext context)
    {
        StringBuilder literal = new();

        void FlushLiteral()
        {
            if (literal.Length > 0)
            {
                target.Add(new Run(literal.ToString()));
                literal.Clear();
            }
        }

        int index = 0;
        while (index < text.Length)
        {
            char current = text[index];

            if (current == '\\' && index + 1 < text.Length)
            {
                literal.Append(text[index + 1]);
                index += 2;
                continue;
            }

            if (current == '`' && TryReadDelimited(text, index, "`", out string code, out int afterCode))
            {
                FlushLiteral();

                Run run = new(code)
                {
                    FontFamily = new FontFamily("Consolas, Courier New, monospace"),
                };
                run.SetResourceReference(TextElement.ForegroundProperty, ThemeKeys.Accent);
                target.Add(run);

                index = afterCode;
                continue;
            }

            if (current == '*' && index + 1 < text.Length && text[index + 1] == '*' &&
                TryReadDelimited(text, index, "**", out string bold, out int afterBold))
            {
                FlushLiteral();
                Bold boldInline = new();
                AppendInlines(boldInline.Inlines, bold, context);
                target.Add(boldInline);
                index = afterBold;
                continue;
            }

            if ((current == '*' || current == '_') &&
                TryReadDelimited(text, index, current.ToString(), out string italic, out int afterItalic))
            {
                FlushLiteral();
                Italic italicInline = new();
                AppendInlines(italicInline.Inlines, italic, context);
                target.Add(italicInline);
                index = afterItalic;
                continue;
            }

            if (current == '[' && TryReadLink(text, index, out string label, out string url, out int afterLink))
            {
                FlushLiteral();
                target.Add(BuildLink(label, url, context));
                index = afterLink;
                continue;
            }

            literal.Append(current);
            index++;
        }

        FlushLiteral();
    }

    private static bool TryReadDelimited(string text, int start, string delimiter, out string content, out int next)
    {
        int contentStart = start + delimiter.Length;
        int close = text.IndexOf(delimiter, contentStart, StringComparison.Ordinal);

        if (close < 0 || close == contentStart)
        {
            content = string.Empty;
            next = start;
            return false;
        }

        content = text.Substring(contentStart, close - contentStart);
        next = close + delimiter.Length;
        return true;
    }

    private static bool TryReadLink(string text, int start, out string label, out string url, out int next)
    {
        label = string.Empty;
        url = string.Empty;
        next = start;

        int labelEnd = text.IndexOf(']', start + 1);
        if (labelEnd < 0 || labelEnd + 1 >= text.Length || text[labelEnd + 1] != '(')
        {
            return false;
        }

        int urlEnd = text.IndexOf(')', labelEnd + 2);
        if (urlEnd < 0)
        {
            return false;
        }

        label = text.Substring(start + 1, labelEnd - start - 1);
        url = text.Substring(labelEnd + 2, urlEnd - labelEnd - 2).Trim();
        next = urlEnd + 1;
        return true;
    }

    private static Inline BuildLink(string label, string url, RenderContext context)
    {
        Hyperlink link = new(new Run(string.IsNullOrEmpty(label) ? url : label))
        {
            ToolTip = url,
        };

        link.SetResourceReference(TextElement.ForegroundProperty, ThemeKeys.Accent);

        link.RequestNavigate += (_, e) => OpenExternal(e.Uri?.ToString() ?? url);
        link.Click += (_, _) => OpenExternal(url);

        if (Uri.TryCreate(url, UriKind.Absolute, out Uri? parsed))
        {
            link.NavigateUri = parsed;
        }

        return link;
    }

    /// <summary>
    /// Opens a link in the user's browser. Restricted to http and https on purpose: a form
    /// definition can arrive from a downloaded package, and a link is not a licence to launch
    /// an arbitrary executable.
    /// </summary>
    internal static void OpenExternal(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) ||
            !Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or System.IO.FileNotFoundException)
        {
            // No browser, or the shell refused. Not worth interrupting the form over.
        }
    }
}
