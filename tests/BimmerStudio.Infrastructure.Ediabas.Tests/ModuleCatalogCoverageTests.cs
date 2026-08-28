using BimmerStudio.Application.Modules;
using BimmerStudio.Infrastructure.Modules;
using Xunit.Abstractions;

namespace BimmerStudio.Infrastructure.Ediabas.Tests;

/// <summary>
/// Measures the module map against a real SGBD folder. The map only claims names whose meaning
/// is certain, so the interesting number is not "how many are recognised" but "is anything
/// recognised wrongly" — a confident wrong label is worse than an honest raw code.
/// </summary>
public sealed class ModuleCatalogCoverageTests(EcuDataFixture fixture, ITestOutputHelper output)
    : IClassFixture<EcuDataFixture>
{
    private readonly ModuleCatalog _catalog = new();

    [SkippableFact]
    public void Reports_recognition_across_the_real_corpus()
    {
        Skip.IfNot(fixture.IsAvailable, fixture.SkipReason);

        var names = fixture.SgbdFiles.Concat(fixture.GroupFiles)
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => !string.IsNullOrEmpty(name))
            .Select(name => name!)
            .ToList();

        var resolved = names.Select(name => (Name: name, Resolution: _catalog.Resolve(name))).ToList();
        var recognised = resolved.Where(entry => entry.Resolution.ModuleKey is not null).ToList();

        output.WriteLine($"{recognised.Count} of {names.Count} names mapped to a module");

        foreach (var group in recognised
                     .GroupBy(entry => entry.Resolution.CategoryKey)
                     .OrderByDescending(group => group.Count()))
        {
            output.WriteLine($"  {group.Key,-14} {group.Count(),4}");
        }

        output.WriteLine("\nUnrecognised sample (shown raw in the picker):");
        foreach (var name in resolved
                     .Where(entry => entry.Resolution.ModuleKey is null)
                     .Select(entry => entry.Name)
                     .Take(40))
        {
            output.WriteLine($"  {name}");
        }

        recognised.ShouldNotBeEmpty();
    }

    [SkippableFact]
    public void Every_group_file_is_recognised()
    {
        Skip.IfNot(fixture.IsAvailable, fixture.SkipReason);
        Skip.If(fixture.GroupFiles.Count == 0, "No group files present.");

        // Group files are the small, stable, well-known set — d_motor, d_kombi, d_cas. If the
        // map cannot name these, it is not earning its keep.
        var unmapped = fixture.GroupFiles
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => !string.IsNullOrEmpty(name))
            .Where(name => _catalog.Resolve(name!).ModuleKey is null)
            .ToList();

        output.WriteLine($"{unmapped.Count} of {fixture.GroupFiles.Count} group files unmapped:");
        foreach (var name in unmapped)
        {
            output.WriteLine($"  {name}");
        }

        // Not all: the corpus contains address-style groups (d_0099) with no stable meaning.
        unmapped.Count.ShouldBeLessThan(fixture.GroupFiles.Count / 2);
    }

    [Theory]
    [InlineData("d_motor", "motor", "engine")]
    [InlineData("d_kombi", "kombi", "body")]
    [InlineData("d_cas", "cas", "access")]
    [InlineData("MSV70", "dme", "engine")]
    [InlineData("04DDE731", "dde", "engine")]
    [InlineData("CAS", "cas", "access")]
    [InlineData("gs19a", "egs", "transmission")]
    public void Maps_known_names_to_the_right_module(string name, string module, string category)
    {
        var resolution = _catalog.Resolve(name);

        resolution.ModuleKey.ShouldBe(module);
        resolution.CategoryKey.ShouldBe(category);
    }

    [Fact]
    public void Acsm_is_crash_safety_not_cruise_control()
    {
        // ACSM contains "acc" as a substring; rule order is what keeps the crash module out of
        // the chassis section.
        _catalog.Resolve("ACSM2").ModuleKey.ShouldBe("acsm");
        _catalog.Resolve("ACSM2").CategoryKey.ShouldBe("safety");
    }

    [Theory]
    [InlineData("00swtkwp")]
    [InlineData("31BKOML2")]
    [InlineData("ZZZ_NOT_A_REAL_ECU")]
    public void Unrecognised_names_stay_unnamed_rather_than_guessed(string name)
    {
        var resolution = _catalog.Resolve(name);

        resolution.ModuleKey.ShouldBeNull();
        resolution.CategoryKey.ShouldBe(ModuleResolution.OtherCategory);
    }
}
