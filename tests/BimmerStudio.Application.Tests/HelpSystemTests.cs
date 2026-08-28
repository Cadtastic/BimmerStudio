using System.Reflection;
using BimmerStudio.Application.Help;
using BimmerStudio.Application.Localization;
using BimmerStudio.Domain.Diagnostics;
using BimmerStudio.Domain.Safety;
using BimmerStudio.Infrastructure.Help;
using BimmerStudio.Infrastructure.Localization;

namespace BimmerStudio.Application.Tests;

/// <summary>
/// Covers F1 resolution, the shipped topic set, and the job-help composer.
/// </summary>
public sealed class HelpSystemTests
{
    private const string AppResourcePrefix = "BimmerStudio.App.Help.Topics";

    /// <summary>The real shipped packs, so composed help is exercised against real strings.</summary>
    internal static async Task<Localizer> ShippedLocalizerAsync(string languageId = "en")
    {
        var localizer = new Localizer(new JsonLanguagePackProvider(
            typeof(App.ViewLocator).Assembly,
            "BimmerStudio.App.Assets.Languages"));

        await localizer.InitialiseAsync(languageId);
        return localizer;
    }

    private static async Task<HelpService> CreateServiceAsync(string languageId = "en")
    {
        var localizer = await ShippedLocalizerAsync(languageId);

        return new HelpService(
            new EmbeddedHelpContentStore(typeof(App.ViewLocator).Assembly, AppResourcePrefix),
            new JobHelpComposer(new JobSafetyClassifier(), localizer),
            localizer);
    }

    [Fact]
    public async Task Ships_the_core_topics()
    {
        var topics = await (await CreateServiceAsync()).GetTableOfContentsAsync();

        topics.ShouldNotBeEmpty();
        topics.Select(topic => topic.Id.Value)
            .ShouldBe(["connection", "glossary", "overview", "safety", "sgbd-browser", "workspace"], ignoreOrder: true);
    }

    [Fact]
    public async Task Every_shipped_topic_has_a_title_and_a_body()
    {
        var topics = await (await CreateServiceAsync()).GetTableOfContentsAsync();

        topics.ShouldAllBe(topic => !string.IsNullOrWhiteSpace(topic.Title));
        topics.ShouldAllBe(topic => topic.Markdown.Length > 100);
    }

    [Fact]
    public async Task F1_falls_back_from_a_missing_element_topic_to_the_view()
    {
        var service = await CreateServiceAsync();

        // Nothing authored for this control; the view's topic should still open.
        var topic = await service.ResolveAsync(new HelpContext(
            HelpTopicId.Parse("sgbd-browser/job-list/nonexistent-control"),
            HelpTopicId.Parse("overview")));

        topic.ShouldNotBeNull();
        topic.Id.Value.ShouldBe("sgbd-browser");
    }

    [Fact]
    public async Task F1_prefers_the_focused_element_over_the_view()
    {
        var topic = await (await CreateServiceAsync()).ResolveAsync(new HelpContext(
            HelpTopicId.Parse("connection"),
            HelpTopicId.Parse("overview")));

        topic!.Id.Value.ShouldBe("connection");
    }

    [Fact]
    public async Task A_selected_job_wins_over_any_authored_topic()
    {
        var job = new JobDescriptor("FS_LESEN", ["Fehlerspeicher lesen"], [], []);

        var topic = await (await CreateServiceAsync()).ResolveAsync(new HelpContext(
            HelpTopicId.Parse("connection"),
            HelpTopicId.Parse("overview"),
            job,
            SgbdIdentifier.Variant("CAS")));

        topic!.Title.ShouldBe("FS_LESEN");
        topic.Markdown.ShouldContain("CAS");

        // The SGBD's German comment reaches the reader through the same phrase dictionary the
        // browser uses, so the two views never disagree about what a job does.
        topic.Markdown.ShouldContain("Reads the fault memory");
    }

    [Fact]
    public async Task Search_ranks_the_glossary_above_passing_mentions()
    {
        var results = await (await CreateServiceAsync()).SearchAsync("ZCS");

        results.ShouldNotBeEmpty();
        results[0].Id.Value.ShouldBe("glossary");
    }

    [Fact]
    public async Task Search_with_no_query_returns_everything()
    {
        var service = await CreateServiceAsync();

        var all = await service.GetTableOfContentsAsync();
        var searched = await service.SearchAsync("   ");

        searched.Count.ShouldBe(all.Count);
    }

    [Fact]
    public async Task Job_help_explains_why_a_write_job_is_blocked()
    {
        var composer = new JobHelpComposer(new JobSafetyClassifier(), await ShippedLocalizerAsync());

        var topic = composer.Compose(JobDescriptor.NameOnly("FS_LOESCHEN"), SgbdIdentifier.Variant("CAS"));

        // The localised category label, not the enum name: help is written for a reader.
        topic.Markdown.ShouldContain("Erases memory");
        topic.Markdown.ShouldContain("read-only");
    }

    [Fact]
    public async Task Job_help_says_so_when_the_description_file_documents_nothing()
    {
        var composer = new JobHelpComposer(new JobSafetyClassifier(), await ShippedLocalizerAsync());

        var topic = composer.Compose(JobDescriptor.NameOnly("STATUS_IRGENDWAS"), null);

        topic.Markdown.ShouldContain("carries no description");
    }

    [Fact]
    public async Task Job_help_flags_an_unclassified_job_rather_than_assuming_it_is_safe()
    {
        var composer = new JobHelpComposer(new JobSafetyClassifier(), await ShippedLocalizerAsync());

        var topic = composer.Compose(JobDescriptor.NameOnly("XYZZY_FROBNICATE"), null);

        // Whitespace is collapsed because the source wraps prose across lines; the assertion is
        // about what the text says, not where it happens to break.
        var prose = CollapseWhitespace(topic.Markdown);

        prose.ShouldContain("Unknown");
        prose.ShouldContain("treated as writes rather than assumed safe");
    }

    private static string CollapseWhitespace(string text) =>
        string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    [Theory]
    [InlineData("sgbd-browser/job-list/args", new[] { "sgbd-browser/job-list/args", "sgbd-browser/job-list", "sgbd-browser" })]
    [InlineData("overview", new[] { "overview" })]
    public void Topic_ids_enumerate_their_ancestors_most_specific_first(string id, string[] expected) =>
        HelpTopicId.Parse(id).WithAncestors().Select(topic => topic.Value).ShouldBe(expected);

    [Fact]
    public void Front_matter_is_parsed_and_the_body_kept()
    {
        var topic = MarkdownTopicParser.Parse("fallback", """
            ---
            id: custom-id
            title: Custom Title
            keywords: alpha, beta
            ---

            # Heading

            Body text.
            """);

        topic.Id.Value.ShouldBe("custom-id");
        topic.Title.ShouldBe("Custom Title");
        topic.Keywords.ShouldBe(["alpha", "beta"]);
        topic.Markdown.ShouldStartWith("# Heading");
        topic.Markdown.ShouldNotContain("keywords:");
    }

    [Fact]
    public void A_topic_without_front_matter_still_loads()
    {
        var topic = MarkdownTopicParser.Parse("some/id", "# Just A Heading\n\nText.");

        topic.Id.Value.ShouldBe("some/id");
        topic.Title.ShouldBe("Just A Heading");
    }

    // ---- Help follows the selected language ----

    [Fact]
    public async Task Authored_topics_are_served_in_the_selected_language()
    {
        var english = await (await CreateServiceAsync("en")).GetTopicAsync(HelpTopicId.Parse("safety"));
        var german = await (await CreateServiceAsync("de")).GetTopicAsync(HelpTopicId.Parse("safety"));

        english!.Title.ShouldBe("Safety");
        german!.Title.ShouldBe("Sicherheit");
        german.Markdown.ShouldNotBe(english.Markdown);
    }

    [Fact]
    public async Task A_language_missing_a_topic_falls_back_to_english_rather_than_losing_it()
    {
        // Every id present in English must resolve in any language, translated or not.
        var english = await (await CreateServiceAsync("en")).GetTableOfContentsAsync();
        var german = await (await CreateServiceAsync("de")).GetTableOfContentsAsync();

        german.Select(topic => topic.Id.Value)
            .ShouldBe(english.Select(topic => topic.Id.Value), ignoreOrder: true);
    }

    [Fact]
    public async Task Composed_job_help_is_written_in_the_selected_language()
    {
        var job = JobDescriptor.NameOnly("FS_LOESCHEN");
        var sgbd = SgbdIdentifier.Variant("CAS");

        var english = new JobHelpComposer(new JobSafetyClassifier(), await ShippedLocalizerAsync("en"))
            .Compose(job, sgbd);
        var german = new JobHelpComposer(new JobSafetyClassifier(), await ShippedLocalizerAsync("de"))
            .Compose(job, sgbd);

        // Chrome and classification follow the language...
        english.Markdown.ShouldContain("Classification:");
        english.Markdown.ShouldContain("What the ECU description file says");
        german.Markdown.ShouldContain("Einstufung:");
        german.Markdown.ShouldContain("Was die Steuergerätebeschreibung sagt");

        // ...while the job name, being protocol, does not.
        english.Title.ShouldBe("FS_LOESCHEN");
        german.Title.ShouldBe("FS_LOESCHEN");
    }

    [Fact]
    public async Task Composed_job_help_translates_the_description_files_own_text()
    {
        // The regression this was written for: the help window showed raw German that the main
        // window had already translated, because the composer bypassed the phrase dictionary.
        var job = new JobDescriptor(
            "AIF_SCHREIBEN",
            ["Schreiben des Anwender Informations Feldes", "Standard Flashjob", "Modus : Default"],
            [new JobParameterInfo("AIF_FG_NR", "string", "Fahrgestellnummer 7-stellig oder 17-stellig")],
            []);

        var topic = new JobHelpComposer(new JobSafetyClassifier(), await ShippedLocalizerAsync("en"))
            .Compose(job, SgbdIdentifier.Variant("CAS"));

        topic.Markdown.ShouldContain("Writes the user information field (AIF)");
        topic.Markdown.ShouldContain("Standard flash job");
        topic.Markdown.ShouldContain("Mode: default");
        topic.Markdown.ShouldContain("Chassis number (VIN), 7 or 17 characters");
    }

    [Fact]
    public async Task German_help_leaves_the_description_files_text_as_written()
    {
        var job = new JobDescriptor("AIF_SCHREIBEN", ["Standard Flashjob"], [], []);

        var topic = new JobHelpComposer(new JobSafetyClassifier(), await ShippedLocalizerAsync("de"))
            .Compose(job, SgbdIdentifier.Variant("CAS"));

        // German is the source language of this text: it passes through untouched.
        topic.Markdown.ShouldContain("Standard Flashjob");
        topic.Markdown.ShouldNotContain("Standard flash job");
    }

    [Fact]
    public async Task Multi_line_parameter_text_survives_the_markdown_table()
    {
        var job = new JobDescriptor(
            "C_C_LESEN",
            [],
            [new JobParameterInfo("BINAER_BUFFER", "binary", "Der Binaerbuffer hat folgenden Aufbau\nByte 0 : Datentyp")],
            []);

        var topic = new JobHelpComposer(new JobSafetyClassifier(), await ShippedLocalizerAsync("en"))
            .Compose(job, null);

        var row = topic.Markdown
            .Split('\n')
            .Single(line => line.Contains("BINAER_BUFFER", StringComparison.Ordinal));

        // A newline inside a cell would break the table, so the lines are joined visibly instead.
        row.ShouldContain("The binary buffer is laid out as follows");
        row.ShouldContain("Byte 0 : Datentyp");
        row.Count(character => character == '|').ShouldBe(4);
    }
}
