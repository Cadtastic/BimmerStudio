using BimmerStudio.App.ViewModels;
using BimmerStudio.Application.Localization;
using BimmerStudio.Domain.Safety;
using BimmerStudio.Infrastructure.Localization;
using BimmerStudio.Infrastructure.Modules;

namespace BimmerStudio.Application.Tests;

/// <summary>
/// The picker holds several hundred entries, so its order is the difference between a list you
/// can scan and one you can only search.
/// </summary>
public sealed class EcuPickerOrderingTests
{
    private static async Task<SgbdBrowserViewModel> BrowserAsync()
    {
        var localizer = await HelpSystemTests.ShippedLocalizerAsync("en");
        return new SgbdBrowserViewModel(new JobSafetyClassifier(), localizer, new ModuleCatalog());
    }

    [Fact]
    public async Task Groups_come_before_variants_inside_a_section()
    {
        var browser = await BrowserAsync();

        browser.SetAvailableSgbds(["CVM_E64.prg", "d_kombi.grp", "KBM_E65.prg", "d_cvm.grp"]);

        var body = browser.AvailableSgbds
            .SkipWhile(item => !item.IsHeader)
            .Skip(1)
            .TakeWhile(item => !item.IsHeader)
            .ToList();

        body.ShouldNotBeEmpty();

        // Every group precedes every variant within the section.
        var lastGroup = body.FindLastIndex(item => item.IsGroup);
        var firstVariant = body.FindIndex(item => !item.IsGroup);
        if (lastGroup >= 0 && firstVariant >= 0)
        {
            lastGroup.ShouldBeLessThan(firstVariant);
        }
    }

    [Fact]
    public async Task Sections_are_introduced_by_a_non_selectable_header()
    {
        var browser = await BrowserAsync();

        browser.SetAvailableSgbds(["MSV70.prg", "d_kombi.grp"]);

        var headers = browser.AvailableSgbds.Where(item => item.IsHeader).ToList();

        headers.ShouldNotBeEmpty();
        headers.ShouldAllBe(header => !header.IsSelectable);
        headers.ShouldAllBe(header => header.Identifier == null);

        // A header names the area, not a file.
        headers.Select(header => header.CategoryName).ShouldContain("Engine");
    }

    [Fact]
    public async Task Recognised_entries_carry_a_module_name_and_keep_the_raw_code()
    {
        var browser = await BrowserAsync();

        browser.SetAvailableSgbds(["d_kombi.grp"]);

        var item = browser.AvailableSgbds.Single(entry => !entry.IsHeader);

        item.ModuleName.ShouldBe("Instrument cluster (KOMBI)");
        item.DisplayName.ShouldBe("d_kombi");
        item.HasModuleName.ShouldBeTrue();
    }

    [Fact]
    public async Task Unrecognised_entries_fall_into_other_and_show_no_module_name()
    {
        var browser = await BrowserAsync();

        browser.SetAvailableSgbds(["00swtkwp.prg"]);

        var item = browser.AvailableSgbds.Single(entry => !entry.IsHeader);

        item.HasModuleName.ShouldBeFalse();
        item.ModuleName.ShouldBeNull();
        item.DisplayName.ShouldBe("00swtkwp");
        item.CategoryName.ShouldBe("Other / unrecognised");
    }

    [Fact]
    public async Task Module_names_follow_a_language_switch()
    {
        var localizer = await HelpSystemTests.ShippedLocalizerAsync("en");
        var browser = new SgbdBrowserViewModel(
            new JobSafetyClassifier(), localizer, new ModuleCatalog());

        browser.SetAvailableSgbds(["d_kombi.grp"]);
        var item = browser.AvailableSgbds.Single(entry => !entry.IsHeader);

        item.ModuleName.ShouldBe("Instrument cluster (KOMBI)");

        await localizer.SetLanguageAsync("de");

        item.ModuleName.ShouldBe("Instrumentenkombination (KOMBI)");
        item.CategoryName.ShouldBe("Karosserie & Komfort");
    }

    [Fact]
    public async Task The_job_filter_matches_name_and_translated_summary()
    {
        var localizer = await HelpSystemTests.ShippedLocalizerAsync("en");
        var browser = new SgbdBrowserViewModel(
            new JobSafetyClassifier(), localizer, new ModuleCatalog());

        browser.Jobs.Add(NewJob("FS_LESEN", "Fehlerspeicher lesen"));
        browser.Jobs.Add(NewJob("AIF_SCHREIBEN", "Schreiben des Anwender Informations Feldes"));

        // Matches the protocol name...
        browser.JobFilter = "FS_";
        browser.VisibleJobs.Select(job => job.Name).ShouldBe(["FS_LESEN"]);

        // ...and the translated description, which is what an English reader actually sees.
        browser.JobFilter = "user information";
        browser.VisibleJobs.Select(job => job.Name).ShouldBe(["AIF_SCHREIBEN"]);

        browser.JobFilter = string.Empty;
        browser.VisibleJobs.Count.ShouldBe(2);

        JobListItemViewModel NewJob(string name, string comment) =>
            new(
                new Domain.Diagnostics.JobDescriptor(name, [comment], [], []),
                JobSafety.Read,
                localizer);
    }
}
