using BimmerStudio.Application.Help;

namespace BimmerStudio.Infrastructure.Help;

/// <summary>
/// Parses a help topic file: optional YAML-ish front matter followed by Markdown.
/// </summary>
/// <remarks>
/// Deliberately a hand-rolled reader for a three-key header (<c>id</c>, <c>title</c>,
/// <c>keywords</c>) rather than a YAML dependency. Anything missing is inferred, so a topic file
/// with no front matter at all still loads.
/// </remarks>
public static class MarkdownTopicParser
{
    private const string Delimiter = "---";

    public static HelpTopic Parse(string fallbackId, string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fallbackId);
        content ??= string.Empty;

        var id = fallbackId;
        string? title = null;
        var keywords = new List<string>();
        var body = content;

        if (TrySplitFrontMatter(content, out var frontMatter, out var remainder))
        {
            body = remainder;

            foreach (var line in frontMatter.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var separator = line.IndexOf(':');
                if (separator <= 0)
                {
                    continue;
                }

                var key = line[..separator].Trim();
                var value = line[(separator + 1)..].Trim();

                if (key.Equals("id", StringComparison.OrdinalIgnoreCase) && value.Length > 0)
                {
                    id = value;
                }
                else if (key.Equals("title", StringComparison.OrdinalIgnoreCase))
                {
                    title = value;
                }
                else if (key.Equals("keywords", StringComparison.OrdinalIgnoreCase))
                {
                    keywords.AddRange(value.Split(
                        ',',
                        StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                }
            }
        }

        return new HelpTopic(
            HelpTopicId.Parse(id),
            title ?? FirstHeading(body) ?? id,
            body.Trim(),
            keywords);
    }

    private static bool TrySplitFrontMatter(string content, out string frontMatter, out string body)
    {
        frontMatter = string.Empty;
        body = content;

        var trimmed = content.TrimStart();
        if (!trimmed.StartsWith(Delimiter, StringComparison.Ordinal))
        {
            return false;
        }

        var afterOpening = trimmed[Delimiter.Length..].TrimStart('\r', '\n');
        var closing = afterOpening.IndexOf(
            Delimiter + "\n", StringComparison.Ordinal);

        if (closing < 0)
        {
            closing = afterOpening.IndexOf(Delimiter + "\r\n", StringComparison.Ordinal);
        }

        if (closing < 0)
        {
            return false;
        }

        frontMatter = afterOpening[..closing];
        body = afterOpening[(closing + Delimiter.Length)..];
        return true;
    }

    private static string? FirstHeading(string body)
    {
        foreach (var line in body.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("# ", StringComparison.Ordinal))
            {
                return trimmed[2..].Trim();
            }
        }

        return null;
    }
}
