using System.Reflection;
using BimmerStudio.Application.Help;
using BimmerStudio.Domain.Diagnostics;
using BimmerStudio.Domain.Safety;
using BimmerStudio.Infrastructure.Help;

namespace BimmerStudio.Application.Tests;

/// <summary>
/// Covers F1 resolution, the shipped topic set, and the job-help composer.
/// </summary>
public sealed class HelpSystemTests
{
    private const string AppResourcePrefix = "BimmerStudio.App.Help.Topics";

    private static HelpService CreateService() =>
        new(
            new EmbeddedHelpContentStore(
                typeof(App.ViewLocator).Assembly,
                AppResourcePrefix),
            new JobHelpComposer(new JobSafetyClassifier()));

    [Fact]
    public async Task Ships_the_core_topics()
    {
        var topics = await CreateService().GetTableOfContentsAsync();

        topics.ShouldNotBeEmpty();
        topics.Select(topic => topic.Id.Value)
            .ShouldBe(["connection", "glossary", "overview", "safety", "sgbd-browser", "workspace"], ignoreOrder: true);
    }

    [Fact]
    public async Task Every_shipped_topic_has_a_title_and_a_body()
    {
        var topics = await CreateService().GetTableOfContentsAsync();

        topics.ShouldAllBe(topic => !string.IsNullOrWhiteSpace(topic.Title));
        topics.ShouldAllBe(topic => topic.Markdown.Length > 100);
    }

    [Fact]
    public async Task F1_falls_back_from_a_missing_element_topic_to_the_view()
    {
        var service = CreateService();

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
        var topic = await CreateService().ResolveAsync(new HelpContext(
            HelpTopicId.Parse("connection"),
            HelpTopicId.Parse("overview")));

        topic!.Id.Value.ShouldBe("connection");
    }

    [Fact]
    public async Task A_selected_job_wins_over_any_authored_topic()
    {
        var job = new JobDescriptor("FS_LESEN", ["Fehlerspeicher lesen"], [], []);

        var topic = await CreateService().ResolveAsync(new HelpContext(
            HelpTopicId.Parse("connection"),
            HelpTopicId.Parse("overview"),
            job,
            SgbdIdentifier.Variant("CAS")));

        topic!.Title.ShouldBe("FS_LESEN");
        topic.Markdown.ShouldContain("Fehlerspeicher lesen");
        topic.Markdown.ShouldContain("CAS");
    }

    [Fact]
    public async Task Search_ranks_the_glossary_above_passing_mentions()
    {
        var results = await CreateService().SearchAsync("ZCS");

        results.ShouldNotBeEmpty();
        results[0].Id.Value.ShouldBe("glossary");
    }

    [Fact]
    public async Task Search_with_no_query_returns_everything()
    {
        var service = CreateService();

        var all = await service.GetTableOfContentsAsync();
        var searched = await service.SearchAsync("   ");

        searched.Count.ShouldBe(all.Count);
    }

    [Fact]
    public void Job_help_explains_why_a_write_job_is_blocked()
    {
        var composer = new JobHelpComposer(new JobSafetyClassifier());

        var topic = composer.Compose(JobDescriptor.NameOnly("FS_LOESCHEN"), SgbdIdentifier.Variant("CAS"));

        topic.Markdown.ShouldContain("MemoryClear");
        topic.Markdown.ShouldContain("read-only");
    }

    [Fact]
    public void Job_help_says_so_when_the_description_file_documents_nothing()
    {
        var composer = new JobHelpComposer(new JobSafetyClassifier());

        var topic = composer.Compose(JobDescriptor.NameOnly("STATUS_IRGENDWAS"), null);

        topic.Markdown.ShouldContain("carries no description");
    }

    [Fact]
    public void Job_help_flags_an_unclassified_job_rather_than_assuming_it_is_safe()
    {
        var composer = new JobHelpComposer(new JobSafetyClassifier());

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
}
