using System.Reflection;
using System.Text.Json;
using BimmerStudio.Application.Localization;

namespace BimmerStudio.Infrastructure.Localization;

/// <summary>
/// Loads language packs from embedded resources and from a folder next to the executable.
/// </summary>
/// <remarks>
/// The folder is what makes languages extensible after shipping: a new language is one JSON file
/// dropped into <c>languages/</c>, no rebuild involved. A folder pack with the same id as an
/// embedded one replaces it, so a user can also override shipped translations they disagree with.
/// </remarks>
public sealed class JsonLanguagePackProvider(
    Assembly assembly,
    string resourcePrefix,
    string? packDirectory = null) : ILanguagePackProvider
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public async Task<IReadOnlyList<LanguagePack>> LoadAllAsync(
        CancellationToken cancellationToken = default)
    {
        var packs = new Dictionary<string, LanguagePack>(StringComparer.OrdinalIgnoreCase);

        foreach (var resourceName in assembly.GetManifestResourceNames())
        {
            if (!resourceName.StartsWith(resourcePrefix, StringComparison.Ordinal)
                || !resourceName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            await using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream is null)
            {
                continue;
            }

            if (await ParseAsync(stream, cancellationToken).ConfigureAwait(false) is { } pack)
            {
                packs[pack.Id] = pack;
            }
        }

        if (!string.IsNullOrWhiteSpace(packDirectory) && Directory.Exists(packDirectory))
        {
            foreach (var file in Directory.EnumerateFiles(packDirectory, "*.json"))
            {
                cancellationToken.ThrowIfCancellationRequested();

                await using var stream = File.OpenRead(file);
                if (await ParseAsync(stream, cancellationToken).ConfigureAwait(false) is { } pack)
                {
                    packs[pack.Id] = pack;
                }
            }
        }

        return packs.Values
            .OrderBy(pack => pack.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static async Task<LanguagePack?> ParseAsync(
        Stream stream,
        CancellationToken cancellationToken)
    {
        try
        {
            var raw = await JsonSerializer
                .DeserializeAsync<RawPack>(stream, Options, cancellationToken)
                .ConfigureAwait(false);

            if (raw is null || string.IsNullOrWhiteSpace(raw.Id))
            {
                return null;
            }

            return new LanguagePack(
                raw.Id.Trim(),
                string.IsNullOrWhiteSpace(raw.DisplayName) ? raw.Id.Trim() : raw.DisplayName.Trim(),
                new Dictionary<string, string>(
                    raw.Ui ?? [], StringComparer.OrdinalIgnoreCase),
                NormaliseKeys(raw.DataPhrases));
        }
        catch (JsonException)
        {
            // A malformed pack (usually hand-edited) removes one language, not the feature.
            return null;
        }
    }

    /// <summary>
    /// Phrase keys are whitespace-normalised, because the same comment appears in SGBDs with
    /// varying interior spacing ("Modus  : Default" and "Modus   : Default" are one phrase).
    /// </summary>
    private static Dictionary<string, string> NormaliseKeys(Dictionary<string, string>? phrases)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (key, value) in phrases ?? [])
        {
            result[TextNormaliser.NormaliseWhitespace(key)] = value;
        }

        return result;
    }

    private sealed record RawPack(
        string? Id,
        string? DisplayName,
        Dictionary<string, string>? Ui,
        Dictionary<string, string>? DataPhrases);
}
