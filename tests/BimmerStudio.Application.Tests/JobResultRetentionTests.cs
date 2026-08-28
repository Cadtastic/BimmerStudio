using BimmerStudio.App.ViewModels;
using BimmerStudio.Application.Localization;
using BimmerStudio.Domain.Diagnostics;
using BimmerStudio.Domain.Safety;
using BimmerStudio.Infrastructure.Localization;

namespace BimmerStudio.Application.Tests;

/// <summary>
/// Results belong to the job that produced them. Anything else puts one job's output on screen
/// under another job's name, which is how a user ends up trusting a reading from the wrong ECU
/// function.
/// </summary>
public sealed class JobResultRetentionTests
{
    private sealed class FakePackProvider(LanguagePack pack) : ILanguagePackProvider
    {
        public Task<IReadOnlyList<LanguagePack>> LoadAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<LanguagePack>>([pack]);
    }

    private static async Task<Localizer> LocalizerAsync()
    {
        var pack = new LanguagePack(
            "en",
            "English",
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Results_SystemSet"] = "System",
                ["Results_DataSetFormat"] = "Set {0}",
                ["Browser_RunCountFormat"] = "{0} run(s)",
                ["Safety_Read"] = "Read",
                ["Safety_Read_Desc"] = "Reads data.",
                ["SysResult_JOBSTATUS"] = "Status of job processing",
            },
            new Dictionary<string, string>());

        var localizer = new Localizer(new FakePackProvider(pack));
        await localizer.InitialiseAsync("en");
        return localizer;
    }

    private static JobListItemViewModel Job(ILocalizer localizer, string name) =>
        new(JobDescriptor.NameOnly(name), JobSafety.Read, localizer);

    private static ResultSetViewModel SystemSet(ILocalizer localizer, params (string Name, string Value)[] values)
    {
        var dictionary = values.ToDictionary(
            entry => entry.Name,
            entry => ResultValue.Text(entry.Name, entry.Value),
            StringComparer.OrdinalIgnoreCase);

        return ResultSetViewModel.System(new ResultSet(dictionary), localizer);
    }

    [Fact]
    public async Task Each_job_keeps_its_own_results()
    {
        var localizer = await LocalizerAsync();
        var first = Job(localizer, "AIF_LESEN");
        var second = Job(localizer, "C_AEI_LESEN");

        first.ShowResults([SystemSet(localizer, ("JOBSTATUS", "OKAY"))]);

        // The second job has never run: it must show nothing, not the first job's output.
        second.Results.ShouldBeEmpty();
        second.HasRun.ShouldBeFalse();
        first.Results.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Results_survive_switching_away_and_back()
    {
        var localizer = await LocalizerAsync();
        var job = Job(localizer, "AIF_LESEN");

        job.ShowResults([SystemSet(localizer, ("JOBSTATUS", "OKAY"))]);
        job.ExecutionCount++;

        // Selection is what changes in the UI; the job object keeps its own state either way.
        job.Results.ShouldHaveSingleItem();
        job.HasRun.ShouldBeTrue();
        job.RunCountLabel.ShouldBe("1 run(s)");
    }

    [Fact]
    public async Task Running_again_replaces_rather_than_appends()
    {
        var localizer = await LocalizerAsync();
        var job = Job(localizer, "STATUS_UBATT");

        job.ShowResults([SystemSet(localizer, ("JOBSTATUS", "OKAY"))]);
        job.ShowResults([SystemSet(localizer, ("JOBSTATUS", "OKAY")), SystemSet(localizer, ("X", "1"))]);

        job.Results.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Clearing_removes_results_and_the_run_count()
    {
        var localizer = await LocalizerAsync();
        var job = Job(localizer, "AIF_LESEN");

        job.ShowResults([SystemSet(localizer, ("JOBSTATUS", "OKAY"))]);
        job.ExecutionCount++;
        job.LastDuration = "3 ms";

        job.ClearResults();

        job.Results.ShouldBeEmpty();
        job.ExecutionCount.ShouldBe(0);
        job.LastDuration.ShouldBeNull();
        job.HasRun.ShouldBeFalse();
    }

    [Fact]
    public async Task System_results_carry_a_localised_description()
    {
        var localizer = await LocalizerAsync();
        var set = SystemSet(localizer, ("JOBSTATUS", "OKAY"));

        var row = set.Rows.Single();

        // The identifier itself is never translated — it is what other tools and forums show.
        row.Name.ShouldBe("JOBSTATUS");
        row.Description.ShouldBe("Status of job processing");
        row.HasDescription.ShouldBeTrue();
    }

    [Fact]
    public async Task Undocumented_system_results_show_no_description_rather_than_a_key()
    {
        var localizer = await LocalizerAsync();
        var set = SystemSet(localizer, ("SOME_UNDOCUMENTED_RESULT", "1"));

        var row = set.Rows.Single();

        row.Description.ShouldBeNull();
        row.HasDescription.ShouldBeFalse();
    }

    [Fact]
    public async Task Data_set_rows_carry_no_system_description()
    {
        var localizer = await LocalizerAsync();

        var dictionary = new Dictionary<string, ResultValue>(StringComparer.OrdinalIgnoreCase)
        {
            ["JOBSTATUS"] = ResultValue.Text("JOBSTATUS", "OKAY"),
        };
        var set = ResultSetViewModel.Data(1, new ResultSet(dictionary), localizer);

        // Same name, but in a payload set it is the job's own result, described by the job's
        // documentation panel rather than by the fixed system-result glossary.
        set.Title.ShouldBe("Set 1");
        set.Rows.Single().Description.ShouldBeNull();
    }

    [Fact]
    public async Task Job_description_uses_every_documented_comment_line()
    {
        var localizer = await LocalizerAsync();
        var descriptor = new JobDescriptor(
            "C_AEI_AUFTRAG",
            ["Aenderungsindex schreiben", "und ruecklesen"],
            [],
            []);

        var job = new JobListItemViewModel(descriptor, JobSafety.Coding, localizer);

        // Separate documented lines stay separate lines.
        job.FullDescription.ShouldBe("Aenderungsindex schreiben\nund ruecklesen");
        job.Summary.ShouldBe("Aenderungsindex schreiben");
        job.HasSummary.ShouldBeTrue();
    }
}
