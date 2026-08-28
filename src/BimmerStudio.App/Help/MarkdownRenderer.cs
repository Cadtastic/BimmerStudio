using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Media;

namespace BimmerStudio.App.Help;

/// <summary>
/// Renders the Markdown subset the help content uses into Avalonia controls.
/// </summary>
/// <remarks>
/// Hand-written rather than taken from a package. The help text is authored in this repository,
/// so the subset is known and closed — headings, paragraphs, lists, fenced code, tables,
/// blockquotes, and inline bold and code. Writing it here also keeps the app free of a renderer
/// pinned to a particular Avalonia major version, which is the usual reason these dependencies
/// become a problem.
/// </remarks>
public static class MarkdownRenderer
{
    private static readonly FontFamily Monospace = new("Consolas,Menlo,DejaVu Sans Mono,monospace");

    public static Control Render(string markdown)
    {
        var root = new StackPanel { Spacing = 8 };
        var lines = (markdown ?? string.Empty).Replace("\r\n", "\n").Split('\n');

        var paragraph = new List<string>();

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            if (line.StartsWith("```", StringComparison.Ordinal))
            {
                FlushParagraph(root, paragraph);
                root.Children.Add(RenderCodeBlock(lines, ref i));
                continue;
            }

            if (IsTableRow(line) && i + 1 < lines.Length && IsTableSeparator(lines[i + 1]))
            {
                FlushParagraph(root, paragraph);
                root.Children.Add(RenderTable(lines, ref i));
                continue;
            }

            if (line.StartsWith('#'))
            {
                FlushParagraph(root, paragraph);
                root.Children.Add(RenderHeading(line));
                continue;
            }

            if (line.StartsWith("> ", StringComparison.Ordinal))
            {
                FlushParagraph(root, paragraph);
                root.Children.Add(RenderQuote(line[2..]));
                continue;
            }

            if (IsListItem(line))
            {
                FlushParagraph(root, paragraph);
                root.Children.Add(RenderListItem(line));
                continue;
            }

            if (line.Trim() is "---" or "***")
            {
                FlushParagraph(root, paragraph);
                root.Children.Add(new Border
                {
                    Height = 1,
                    Background = new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x2A)),
                    Margin = new Thickness(0, 8),
                });
                continue;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                FlushParagraph(root, paragraph);
                continue;
            }

            paragraph.Add(line.Trim());
        }

        FlushParagraph(root, paragraph);
        return root;
    }

    private static void FlushParagraph(Panel root, List<string> paragraph)
    {
        if (paragraph.Count == 0)
        {
            return;
        }

        root.Children.Add(InlineText(string.Join(' ', paragraph)));
        paragraph.Clear();
    }

    private static Control RenderHeading(string line)
    {
        var level = 0;
        while (level < line.Length && line[level] == '#')
        {
            level++;
        }

        return new TextBlock
        {
            Text = line[level..].Trim(),
            FontSize = level switch { 1 => 22, 2 => 17, _ => 14 },
            FontWeight = FontWeight.SemiBold,
            Margin = new Thickness(0, level == 1 ? 0 : 10, 0, 2),
            TextWrapping = TextWrapping.Wrap,
        };
    }

    private static Control RenderQuote(string text) =>
        new Border
        {
            BorderThickness = new Thickness(3, 0, 0, 0),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x4A, 0x4A, 0x4A)),
            Padding = new Thickness(10, 2, 0, 2),
            Child = InlineText(text, italic: true),
        };

    private static Control RenderListItem(string line)
    {
        var trimmed = line.TrimStart();
        var indent = line.Length - trimmed.Length;
        var content = trimmed.Length > 2 ? trimmed[2..] : string.Empty;

        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(8 + indent * 8, 0, 0, 0),
        };

        panel.Children.Add(new TextBlock
        {
            Text = "•",
            Margin = new Thickness(0, 0, 8, 0),
            Opacity = 0.6,
        });
        panel.Children.Add(InlineText(content));

        return panel;
    }

    private static Control RenderCodeBlock(string[] lines, ref int index)
    {
        var body = new List<string>();
        index++;

        while (index < lines.Length && !lines[index].StartsWith("```", StringComparison.Ordinal))
        {
            body.Add(lines[index]);
            index++;
        }

        return new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0x0D, 0x0D, 0x0D)),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(12, 8),
            Child = new SelectableTextBlock
            {
                Text = string.Join('\n', body),
                FontFamily = Monospace,
                FontSize = 12,
            },
        };
    }

    private static Control RenderTable(string[] lines, ref int index)
    {
        var headers = SplitRow(lines[index]);
        index += 2; // header plus separator

        var rows = new List<string[]>();
        while (index < lines.Length && IsTableRow(lines[index]))
        {
            rows.Add(SplitRow(lines[index]));
            index++;
        }

        index--; // the outer loop advances

        var grid = new Grid { Margin = new Thickness(0, 4) };
        for (var column = 0; column < headers.Length; column++)
        {
            grid.ColumnDefinitions.Add(new ColumnDefinition(
                column == headers.Length - 1 ? GridLength.Star : GridLength.Auto));
        }

        grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        for (var column = 0; column < headers.Length; column++)
        {
            var header = new TextBlock
            {
                Text = headers[column],
                FontWeight = FontWeight.SemiBold,
                Margin = new Thickness(0, 4, 16, 6),
                TextWrapping = TextWrapping.Wrap,
                Opacity = 0.85,
            };
            Grid.SetColumn(header, column);
            grid.Children.Add(header);
        }

        for (var row = 0; row < rows.Count; row++)
        {
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

            for (var column = 0; column < headers.Length && column < rows[row].Length; column++)
            {
                var cell = InlineText(rows[row][column]);
                cell.Margin = new Thickness(0, 2, 16, 2);
                Grid.SetRow(cell, row + 1);
                Grid.SetColumn(cell, column);
                grid.Children.Add(cell);
            }
        }

        return grid;
    }

    /// <summary>
    /// Renders one span of text, honouring inline <c>**bold**</c> and <c>`code`</c>.
    /// Link syntax is flattened to its label: help topics link to each other by id, and the
    /// table of contents is how the user navigates.
    /// </summary>
    private static TextBlock InlineText(string text, bool italic = false)
    {
        var block = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
            FontStyle = italic ? FontStyle.Italic : FontStyle.Normal,
            LineHeight = 19,
        };

        foreach (var (content, kind) in ParseInline(StripLinks(text)))
        {
            block.Inlines!.Add(kind switch
            {
                InlineKind.Bold => new Run(content) { FontWeight = FontWeight.SemiBold },
                InlineKind.Code => new Run(content) { FontFamily = Monospace, Foreground = Brushes.LightSkyBlue },
                _ => new Run(content),
            });
        }

        return block;
    }

    private static string StripLinks(string text)
    {
        // [label](target) -> label
        var result = text;
        int open;

        while ((open = result.IndexOf('[')) >= 0)
        {
            var close = result.IndexOf(']', open);
            if (close < 0 || close + 1 >= result.Length || result[close + 1] != '(')
            {
                break;
            }

            var end = result.IndexOf(')', close);
            if (end < 0)
            {
                break;
            }

            result = result[..open] + result[(open + 1)..close] + result[(end + 1)..];
        }

        return result;
    }

    private enum InlineKind { Plain, Bold, Code }

    private static IEnumerable<(string Content, InlineKind Kind)> ParseInline(string text)
    {
        var buffer = new System.Text.StringBuilder();
        var i = 0;

        while (i < text.Length)
        {
            if (text[i] == '`')
            {
                var end = text.IndexOf('`', i + 1);
                if (end > i)
                {
                    if (buffer.Length > 0)
                    {
                        yield return (buffer.ToString(), InlineKind.Plain);
                        buffer.Clear();
                    }

                    yield return (text[(i + 1)..end], InlineKind.Code);
                    i = end + 1;
                    continue;
                }
            }

            if (text[i] == '*' && i + 1 < text.Length && text[i + 1] == '*')
            {
                var end = text.IndexOf("**", i + 2, StringComparison.Ordinal);
                if (end > i)
                {
                    if (buffer.Length > 0)
                    {
                        yield return (buffer.ToString(), InlineKind.Plain);
                        buffer.Clear();
                    }

                    yield return (text[(i + 2)..end], InlineKind.Bold);
                    i = end + 2;
                    continue;
                }
            }

            buffer.Append(text[i]);
            i++;
        }

        if (buffer.Length > 0)
        {
            yield return (buffer.ToString(), InlineKind.Plain);
        }
    }

    private static bool IsListItem(string line)
    {
        var trimmed = line.TrimStart();
        return trimmed.StartsWith("- ", StringComparison.Ordinal)
            || trimmed.StartsWith("* ", StringComparison.Ordinal);
    }

    private static bool IsTableRow(string line) =>
        line.TrimStart().StartsWith('|');

    private static bool IsTableSeparator(string line) =>
        IsTableRow(line) && line.Replace("|", string.Empty).Trim() is { Length: > 0 } body
        && body.All(c => c is '-' or ':' or ' ');

    private static string[] SplitRow(string line) =>
        line.Trim().Trim('|').Split('|', StringSplitOptions.TrimEntries);
}
