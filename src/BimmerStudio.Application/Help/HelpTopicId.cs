namespace BimmerStudio.Application.Help;

/// <summary>
/// Identifies a help topic, for example <c>sgbd-browser/run-continuous</c>.
/// </summary>
/// <remarks>
/// Hierarchical so that F1 can fall back: an unmatched
/// <c>sgbd-browser/job-list/argument-line</c> tries <c>sgbd-browser/job-list</c>, then
/// <c>sgbd-browser</c>. That way a control without its own topic still opens something useful.
/// </remarks>
public sealed record HelpTopicId
{
    public const char Separator = '/';

    private HelpTopicId(string value) => Value = value;

    public string Value { get; }

    public static HelpTopicId Parse(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return new HelpTopicId(value.Trim().Trim(Separator).ToLowerInvariant());
    }

    public static HelpTopicId? TryParse(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : Parse(value);

    /// <summary>
    /// This id and each ancestor, most specific first, for progressive F1 fallback.
    /// </summary>
    public IEnumerable<HelpTopicId> WithAncestors()
    {
        var segments = Value.Split(Separator, StringSplitOptions.RemoveEmptyEntries);

        for (var length = segments.Length; length > 0; length--)
        {
            yield return new HelpTopicId(string.Join(Separator, segments[..length]));
        }
    }

    public bool Equals(HelpTopicId? other) =>
        other is not null && string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

    public override int GetHashCode() => Value.GetHashCode(StringComparison.OrdinalIgnoreCase);

    public override string ToString() => Value;
}
