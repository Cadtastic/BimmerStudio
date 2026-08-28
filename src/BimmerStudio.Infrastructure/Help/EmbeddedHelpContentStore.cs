using System.Reflection;
using BimmerStudio.Application.Help;

namespace BimmerStudio.Infrastructure.Help;

/// <summary>
/// Loads help topics embedded in an assembly, plus an optional local folder.
/// </summary>
/// <remarks>
/// The local folder exists so the legacy reference pack — job semantics, coding concepts, the
/// FSW/PSW and ZCS glossary, all derived from BMW's own documentation — can be added by a user
/// who has it, without that content ever being redistributed in the repository.
/// </remarks>
public sealed class EmbeddedHelpContentStore(
    Assembly assembly,
    string resourcePrefix,
    string? additionalContentPath = null) : IHelpContentStore
{
    private const string TopicExtension = ".md";

    public async Task<IReadOnlyList<HelpTopic>> LoadAllAsync(
        CancellationToken cancellationToken = default)
    {
        var topics = new List<HelpTopic>();

        foreach (var resourceName in assembly.GetManifestResourceNames())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!resourceName.StartsWith(resourcePrefix, StringComparison.Ordinal)
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

            topics.Add(MarkdownTopicParser.Parse(ToTopicId(resourceName), content));
        }

        if (!string.IsNullOrWhiteSpace(additionalContentPath) && Directory.Exists(additionalContentPath))
        {
            foreach (var file in Directory.EnumerateFiles(
                         additionalContentPath, "*" + TopicExtension, SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var content = await File.ReadAllTextAsync(file, cancellationToken).ConfigureAwait(false);
                var relativeId = Path.GetRelativePath(additionalContentPath, file)
                    .Replace(Path.DirectorySeparatorChar, HelpTopicId.Separator)
                    .Replace(Path.AltDirectorySeparatorChar, HelpTopicId.Separator);

                topics.Add(MarkdownTopicParser.Parse(
                    relativeId[..^TopicExtension.Length],
                    content));
            }
        }

        return topics;
    }

    /// <summary>
    /// Turns <c>BimmerStudio.App.Help.Topics.sgbd-browser.job-list.md</c> into
    /// <c>sgbd-browser/job-list</c>. Resource names flatten directories to dots, so the last dot
    /// (the extension) is dropped and the rest become path separators.
    /// </summary>
    private string ToTopicId(string resourceName)
    {
        var withoutPrefix = resourceName[resourcePrefix.Length..].TrimStart('.');
        var withoutExtension = withoutPrefix[..^TopicExtension.Length];

        return withoutExtension.Replace('.', HelpTopicId.Separator);
    }
}
