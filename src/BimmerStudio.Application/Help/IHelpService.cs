namespace BimmerStudio.Application.Help;

/// <summary>
/// Resolves what F1 should show, and searches the help set.
/// </summary>
public interface IHelpService
{
    /// <summary>Every authored topic, for the table of contents.</summary>
    Task<IReadOnlyList<HelpTopic>> GetTableOfContentsAsync(CancellationToken cancellationToken = default);

    /// <summary>Exact lookup. Null when no such topic exists.</summary>
    Task<HelpTopic?> GetTopicAsync(HelpTopicId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// What F1 should open for the current context. Tries the focused element's topic and each
    /// ancestor, then the view, so a control with no topic of its own still opens something
    /// relevant rather than nothing.
    /// </summary>
    Task<HelpTopic?> ResolveAsync(HelpContext context, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<HelpTopic>> SearchAsync(string query, CancellationToken cancellationToken = default);
}
