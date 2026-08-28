using BimmerStudio.Application.Localization;
using BimmerStudio.Infrastructure.Localization;

namespace BimmerStudio.Application.Tests;

public sealed class LocalizationTests
{
    private sealed class FakePackProvider(params LanguagePack[] packs) : ILanguagePackProvider
    {
        public Task<IReadOnlyList<LanguagePack>> LoadAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<LanguagePack>>(packs);
    }

    private static LanguagePack English() => new(
        "en",
        "English",
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Setup_Connect"] = "Connect",
            ["Status_JobsIn"] = "{0} jobs in {1}.",
            ["OnlyInEnglish"] = "English only",
        },
        new Dictionary<string, string>
        {
            ["Fehlerspeicher lesen"] = "Reads the fault memory",
            ["Modus : Default"] = "Mode: default",
        });

    private static LanguagePack German() => new(
        "de",
        "Deutsch",
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Setup_Connect"] = "Verbinden",
        },
        new Dictionary<string, string>());

    private static async Task<Localizer> CreateAsync(string? preferred = "en")
    {
        var localizer = new Localizer(new FakePackProvider(English(), German()));
        await localizer.InitialiseAsync(preferred);
        return localizer;
    }

    [Fact]
    public async Task Resolves_ui_strings_from_the_active_pack()
    {
        var localizer = await CreateAsync("de");

        localizer["Setup_Connect"].ShouldBe("Verbinden");
    }

    [Fact]
    public async Task Falls_back_to_english_then_to_the_key()
    {
        var localizer = await CreateAsync("de");

        // Missing from the German pack, present in English.
        localizer["OnlyInEnglish"].ShouldBe("English only");

        // Missing everywhere: the key itself is the bug report.
        localizer["Nowhere_Defined"].ShouldBe("Nowhere_Defined");
    }

    [Fact]
    public async Task Switching_language_raises_the_indexer_change_and_the_event()
    {
        var localizer = await CreateAsync("en");
        var raisedProperties = new List<string?>();
        var eventRaised = false;

        localizer.PropertyChanged += (_, args) => raisedProperties.Add(args.PropertyName);
        localizer.LanguageChanged += (_, _) => eventRaised = true;

        await localizer.SetLanguageAsync("de");

        localizer.CurrentLanguageId.ShouldBe("de");
        raisedProperties.ShouldContain("Item[]");
        eventRaised.ShouldBeTrue();
    }

    [Fact]
    public async Task Data_phrases_translate_in_english_and_pass_through_in_german()
    {
        var localizer = await CreateAsync("en");
        localizer.TranslateData("Fehlerspeicher lesen").ShouldBe("Reads the fault memory");
        localizer.HasDataTranslation("Fehlerspeicher lesen").ShouldBeTrue();

        await localizer.SetLanguageAsync("de");
        localizer.TranslateData("Fehlerspeicher lesen").ShouldBe("Fehlerspeicher lesen");
        localizer.HasDataTranslation("Fehlerspeicher lesen").ShouldBeFalse();
    }

    [Fact]
    public async Task Data_phrase_lookup_ignores_whitespace_variance()
    {
        var localizer = await CreateAsync("en");

        // SGBDs carry the same comment with varying interior spacing.
        localizer.TranslateData("Modus  : Default").ShouldBe("Mode: default");
        localizer.TranslateData("  Modus   :   Default  ").ShouldBe("Mode: default");
    }

    [Fact]
    public async Task Untranslated_data_text_is_returned_verbatim_not_hidden()
    {
        var localizer = await CreateAsync("en");

        localizer.TranslateData("Irgendein unbekannter Kommentar")
            .ShouldBe("Irgendein unbekannter Kommentar");
    }

    [Fact]
    public async Task Format_uses_the_active_packs_format_string()
    {
        var localizer = await CreateAsync("en");

        localizer.Format("Status_JobsIn", 42, "CAS").ShouldBe("42 jobs in CAS.");
    }

    [Fact]
    public async Task Unknown_preferred_language_falls_back_to_english()
    {
        var localizer = await CreateAsync("fr");

        localizer.CurrentLanguageId.ShouldBe("en");
    }

    [Fact]
    public void Whitespace_normalisation_trims_and_collapses()
    {
        TextNormaliser.NormaliseWhitespace("  a   b\t c  ").ShouldBe("a b c");
        TextNormaliser.NormaliseWhitespace(null).ShouldBe(string.Empty);
        TextNormaliser.NormaliseWhitespace("   ").ShouldBe(string.Empty);
    }
}
