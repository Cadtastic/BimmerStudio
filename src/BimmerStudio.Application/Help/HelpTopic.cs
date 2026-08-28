namespace BimmerStudio.Application.Help;

/// <summary>
/// One help page: Markdown plus enough metadata to find it.
/// </summary>
/// <param name="Markdown">
/// Body text. Composed topics (job help, transport settings) are generated at request time from
/// live data rather than read from a file.
/// </param>
public sealed record HelpTopic(
    HelpTopicId Id,
    string Title,
    string Markdown,
    IReadOnlyList<string> Keywords)
{
    public static HelpTopic Create(string id, string title, string markdown, params string[] keywords) =>
        new(HelpTopicId.Parse(id), title, markdown, keywords);
}
