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
    public async Task English_ships_a_data_phrase_dictionary()
    {
        var packs = await CreateProvider().LoadAllAsync();
        var english = packs.Single(pack => pack.Id == "en");

        // The number is arbitrary; the point is that the mechanism ships seeded, not empty.
        english.DataPhrases.Count.ShouldBeGreaterThan(20);
        english.DataPhrases.ShouldContainKey("Standard Codierjob");
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
