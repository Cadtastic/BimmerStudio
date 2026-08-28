using System.Reflection;
using BimmerStudio.Application.Help;

namespace BimmerStudio.Infrastructure.Help;

/// <summary>
/// Loads help topics embedded in an assembly, plus an optional local folder.
/// </summary>
/// <remarks>
/// <para>
/// Topics live in a per-language folder (<c>Help/Topics/en/…</c>), which resource names flatten
/// to <c>…Help.Topics.en.overview.md</c>. A requested language is overlaid on English, so a
/// language that translates only some topics still shows the rest rather than a gap.
/// </para>
/// <para>
/// The local folder exists so the legacy reference pack — job semantics, coding concepts, the
/// FSW/PSW and ZCS glossary, all derived from BMW's own documentation — can be added by a user
/// who has it, without that content ever being redistributed in the repository.
/// </para>
/// </remarks>
public sealed class EmbeddedHelpContentStore(
    Assembly assembly,
    string resourcePrefix,
    string? additionalContentPath = null) : IHelpContentStore
{
    private const string TopicExtension = ".md";
    private const string FallbackLanguageId = "en";

    public async Task<IReadOnlyList<HelpTopic>> LoadAllAsync(
        string languageId,
        CancellationToken cancellationToken = default)
    {
        // English first, then the requested language on top: the overlay is the fallback.
        var topics = new Dictionary<HelpTopicId, HelpTopic>();

        await LoadLanguageAsync(FallbackLanguageId, topics, cancellationToken).ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(languageId)
            && !languageId.Equals(FallbackLanguageId, StringComparison.OrdinalIgnoreCase))
        {
            await LoadLanguageAsync(languageId, topics, cancellationToken).ConfigureAwait(false);
        }

        await LoadAdditionalAsync(topics, cancellationToken).ConfigureAwait(false);

        return [.. topics.Values];
    }

    private async Task LoadLanguageAsync(
        string languageId,
        Dictionary<HelpTopicId, HelpTopic> topics,
        CancellationToken cancellationToken)
    {
        var languagePrefix = $"{resourcePrefix}.{languageId}.";

        foreach (var resourceName in assembly.GetManifestResourceNames())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!resourceName.StartsWith(languagePrefix, StringComparison.OrdinalIgnoreCase)
                || !resourceName.EndsWith(TopicExtension, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            await using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is null)
            {
                continue;
            }

            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);

            var id = resourceName[languagePrefix.Length..^TopicExtension.Length]
                .Replace('.', HelpTopicId.Separator);

            var topic = MarkdownTopicParser.Parse(id, content);
            topics[topic.Id] = topic;
        }
    }

    private async Task LoadAdditionalAsync(
        Dictionary<HelpTopicId, HelpTopic> topics,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(additionalContentPath) || !Directory.Exists(additionalContentPath))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(
                     additionalContentPath, "*" + TopicExtension, SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();

            var content = await File.ReadAllTextAsync(file, cancellationToken).ConfigureAwait(false);
            var relativeId = Path.GetRelativePath(additionalContentPath, file)
                .Replace(Path.DirectorySeparatorChar, HelpTopicId.Separator)
                .Replace(Path.AltDirectorySeparatorChar, HelpTopicId.Separator);

            var topic = MarkdownTopicParser.Parse(
                relativeId[..^TopicExtension.Length],
                content);

            topics[topic.Id] = topic;
        }
    }
}
