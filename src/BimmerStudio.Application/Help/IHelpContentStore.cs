namespace BimmerStudio.Application.Help;

/// <summary>
/// Supplies authored help topics for a language. Implemented over embedded resources, and
/// optionally over a local folder so the legacy reference pack can be added without shipping
/// BMW content.
/// </summary>
public interface IHelpContentStore
{
    /// <summary>
    /// Topics for <paramref name="languageId"/>. A topic with no translation falls back to the
    /// English original rather than disappearing: partial help is useful, missing help is not.
    /// </summary>
    Task<IReadOnlyList<HelpTopic>> LoadAllAsync(
        string languageId,
        CancellationToken cancellationToken = default);
}
