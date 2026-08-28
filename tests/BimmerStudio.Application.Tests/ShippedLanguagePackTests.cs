using BimmerStudio.Infrastructure.Localization;

namespace BimmerStudio.Application.Tests;

/// <summary>
/// Keeps the shipped language packs honest: they must parse, and every pack must define the same
/// UI keys as English, so a language switch can never produce a mixed-language window.
/// </summary>
public sealed class ShippedLanguagePackTests
{
    private static JsonLanguagePackProvider CreateProvider() => new(
        typeof(App.ViewLocator).Assembly,
        "BimmerStudio.App.Assets.Languages");

    [Fact]
    public async Task Both_shipped_packs_load()
    {
        var packs = await CreateProvider().LoadAllAsync();

        packs.Select(pack => pack.Id).ShouldBe(["de", "en"], ignoreOrder: true);
        packs.ShouldAllBe(pack => !string.IsNullOrWhiteSpace(pack.DisplayName));
    }

    [Fact]
    public async Task Every_pack_defines_the_full_english_key_set()
    {
        var packs = await CreateProvider().LoadAllAsync();
        var english = packs.Single(pack => pack.Id == "en");

        foreach (var pack in packs.Where(pack => pack.Id != "en"))
        {
            var missing = english.Ui.Keys
                .Where(key => !pack.Ui.ContainsKey(key))
                .OrderBy(key => key)
                .ToList();

            missing.ShouldBeEmpty(
                $"Pack '{pack.Id}' is missing UI keys: {string.Join(", ", missing)}");
        }
    }

    [Fact]
    public async Task No_pack_defines_keys_english_does_not_have()
    {
        var packs = await CreateProvider().LoadAllAsync();
        var english = packs.Single(pack => pack.Id == "en");

        foreach (var pack in packs.Where(pack => pack.Id != "en"))
        {
            var orphaned = pack.Ui.Keys
                .Where(key => !english.Ui.ContainsKey(key))
                .OrderBy(key => key)
                .ToList();

            // A key only a translation defines is dead: nothing references it.
            orphaned.ShouldBeEmpty(
                $"Pack '{pack.Id}' defines keys English lacks: {string.Join(", ", orphaned)}");
        }
    }

    [Fact]
    public async Task English_ships_the_corpus_derived_phrase_dictionary()
    {
        var packs = await CreateProvider().LoadAllAsync();
        var english = packs.Single(pack => pack.Id == "en");

        // Machine-assisted from the frequency inventory; a sharp drop here means the
        // dictionary was accidentally truncated.
        english.DataPhrases.Count.ShouldBeGreaterThan(350);
        english.DataPhrases.ShouldContainKey("Standard Codierjob");
        english.DataPhrases.ShouldContainKey("Hex-Antwort von SG");
        english.DataPhrases.ShouldContainKey("OKAY, wenn fehlerfrei");
    }

    [Fact]
    public void Phrase_keys_are_unique_after_whitespace_normalisation()
    {
        // Dictionary deserialisation silently keeps the last duplicate, so a collision in the
        // source file loses a translation without any error. Enumerate the raw JSON instead.
        using var stream = typeof(App.ViewLocator).Assembly.GetManifestResourceStream(
            "BimmerStudio.App.Assets.Languages.en.json");
        stream.ShouldNotBeNull();

        using var document = System.Text.Json.JsonDocument.Parse(
            stream,
            new System.Text.Json.JsonDocumentOptions
            {
                CommentHandling = System.Text.Json.JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            });

        var seen = new Dictionary<string, string>(StringComparer.Ordinal);
        var duplicates = new List<string>();

        foreach (var property in document.RootElement.GetProperty("dataPhrases").EnumerateObject())
        {
            var normalised = TextNormaliser.NormaliseWhitespace(property.Name);
            if (!seen.TryAdd(normalised, property.Name))
            {
                duplicates.Add($"'{property.Name}' collides with '{seen[normalised]}'");
            }
        }

        duplicates.ShouldBeEmpty(string.Join("; ", duplicates));
    }

    [Fact]
    public async Task Phrase_dictionary_never_translates_protocol_identifiers()
    {
        var packs = await CreateProvider().LoadAllAsync();
        var english = packs.Single(pack => pack.Id == "en");

        // A key that is a bare ALL_CAPS identifier would translate a job or result NAME, which
        // must render verbatim everywhere.
        var identifierLike = english.DataPhrases.Keys
            .Where(key => key.Length > 2
                && key.All(c => char.IsAsciiLetterUpper(c) || c == '_' || char.IsAsciiDigit(c))
                && key.Contains('_'))
            .ToList();

        identifierLike.ShouldBeEmpty(string.Join(", ", identifierLike));
    }

    [Fact]
    public async Task Format_placeholders_match_between_languages()
    {
        var packs = await CreateProvider().LoadAllAsync();
        var english = packs.Single(pack => pack.Id == "en");

        foreach (var pack in packs.Where(pack => pack.Id != "en"))
        {
            foreach (var (key, englishText) in english.Ui)
            {
                if (!pack.Ui.TryGetValue(key, out var translated))
                {
                    continue;
                }

                for (var placeholder = 0; placeholder < 4; placeholder++)
                {
                    var token = "{" + placeholder + "}";
                    englishText.Contains(token).ShouldBe(
                        translated.Contains(token),
                        $"Placeholder {token} of '{key}' differs between en and {pack.Id}");
                }
            }
        }
    }
}
