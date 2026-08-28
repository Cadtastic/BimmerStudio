using BimmerStudio.Application.Help;

namespace BimmerStudio.Infrastructure.Help;

/// <summary>
/// Indexes the authored help set once, then answers lookups, F1 resolution and search from memory.
/// </summary>
public sealed class HelpService(IHelpContentStore store, JobHelpComposer jobHelpComposer) : IHelpService
{
    private readonly SemaphoreSlim _loadGate = new(1, 1);
    private IReadOnlyList<HelpTopic>? _topics;
    private Dictionary<HelpTopicId, HelpTopic>? _byId;

    public async Task<IReadOnlyList<HelpTopic>> GetTableOfContentsAsync(
        CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
        return _topics!;
    }

    public async Task<HelpTopic?> GetTopicAsync(
        HelpTopicId id,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(id);
        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);

        return _byId!.GetValueOrDefault(id);
    }

    public async Task<HelpTopic?> ResolveAsync(
        HelpContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        // A selected job wins: it is the most specific thing on screen, and its help is composed
        // from that ECU's own documentation rather than authored.
        if (context.SelectedJob is { } job)
        {
            return jobHelpComposer.Compose(job, context.Sgbd);
        }

        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);

        // Focused element first, walking up its ancestors, then the view.
        var candidates = (context.ElementTopicId?.WithAncestors() ?? [])
            .Concat(context.ViewTopicId?.WithAncestors() ?? []);

        foreach (var candidate in candidates)
        {
            if (_byId!.TryGetValue(candidate, out var topic))
            {
                return topic;
            }
        }

        return null;
    }

    public async Task<IReadOnlyList<HelpTopic>> SearchAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(query))
        {
            return _topics!;
        }

        var terms = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return _topics!
            .Select(topic => (Topic: topic, Score: Score(topic, terms)))
            .Where(match => match.Score > 0)
            .OrderByDescending(match => match.Score)
            .ThenBy(match => match.Topic.Title, StringComparer.CurrentCultureIgnoreCase)
            .Select(match => match.Topic)
            .ToList();
    }

    /// <summary>
    /// Title and keyword hits outrank body hits, so searching "ZCS" surfaces the glossary entry
    /// above every page that merely mentions it.
    /// </summary>
    private static int Score(HelpTopic topic, string[] terms)
    {
        var score = 0;

        foreach (var term in terms)
        {
            if (topic.Title.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                score += 10;
            }

            if (topic.Keywords.Any(keyword => keyword.Contains(term, StringComparison.OrdinalIgnoreCase)))
            {
                score += 5;
            }

            if (topic.Markdown.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                score += 1;
            }
        }

        return score;
    }

    private async Task EnsureLoadedAsync(CancellationToken cancellationToken)
    {
        if (_topics is not null)
        {
            return;
        }

        await _loadGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_topics is not null)
            {
                return;
            }

            var loaded = await store.LoadAllAsync(cancellationToken).ConfigureAwait(false);

            _byId = loaded
                .GroupBy(topic => topic.Id)
                .ToDictionary(group => group.Key, group => group.First());
            _topics = [.. loaded.OrderBy(topic => topic.Id.Value, StringComparer.OrdinalIgnoreCase)];
        }
        finally
        {
            _loadGate.Release();
        }
    }
}
