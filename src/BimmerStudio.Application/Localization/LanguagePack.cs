namespace BimmerStudio.Application.Localization;

/// <summary>
/// One language: the app's UI strings plus a phrase dictionary for text that arrives from the
/// vehicle data at runtime.
/// </summary>
/// <param name="Ui">UI strings by key, for example <c>Setup_Connect</c> → "Connect".</param>
/// <param name="DataPhrases">
/// Translations of German text read out of SGBDs (job comments, argument comments), keyed by the
/// whitespace-normalised original. These cannot be resource keys because the text is data: it
/// comes from the description files, not from this application. Exact-match works because the
/// corpus is highly repetitive — the most common comment alone appears more than eight thousand
/// times.
/// </param>
public sealed record LanguagePack(
    string Id,
    string DisplayName,
    IReadOnlyDictionary<string, string> Ui,
    IReadOnlyDictionary<string, string> DataPhrases)
{
    public static LanguagePack Empty(string id, string displayName) => new(
        id,
        displayName,
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
        new Dictionary<string, string>(StringComparer.Ordinal));
}
