namespace BimmerStudio.Application.Help;

/// <summary>
/// Supplies authored help topics. Implemented over embedded resources, and optionally over a
/// local folder so the legacy reference pack can be added without shipping BMW content.
/// </summary>
public interface IHelpContentStore
{
    Task<IReadOnlyList<HelpTopic>> LoadAllAsync(CancellationToken cancellationToken = default);
}
