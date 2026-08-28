using BimmerStudio.App.ViewModels;
using BimmerStudio.Application.Localization;
using BimmerStudio.Domain.Diagnostics;
using BimmerStudio.Infrastructure.Localization;

namespace BimmerStudio.Application.Tests;

/// <summary>
/// Argument descriptions in SGBDs are laid out as tables — one line per byte offset, or labelled
/// fields separated by tabs. Flattening that into a paragraph is what made the binary-buffer
/// descriptions unreadable.
/// </summary>
public sealed class JobParameterFormattingTests
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
                ["Sgbd_Variant"] = "Variant",
                ["Sgbd_Group"] = "Group",
                ["Sgbd_Variant_Desc"] = "One specific ECU.",
                ["Sgbd_Group_Desc"] = "A family of ECUs.",
                ["Sgbd_NeedsVehicle_Short"] = "needs a vehicle",
                ["Sgbd_NeedsVehicle_Desc"] = "Group files identify the fitted ECU by asking the vehicle.",
            },
            new Dictionary<string, string>
            {
                ["Der Binaerbuffer hat folgenden Aufbau"] = "The binary buffer is laid out as follows",
            });

        var localizer = new Localizer(new FakePackProvider(pack));
        await localizer.InitialiseAsync("en");
        return localizer;
    }

    private static JobParameterViewModel Parameter(ILocalizer localizer, string comment) =>
        new(new JobParameterInfo("BINAER_BUFFER", "binary", comment), localizer);

    [Fact]
    public async Task Each_documented_line_becomes_its_own_display_line()
    {
        var localizer = await LocalizerAsync();

        var parameter = Parameter(
            localizer,
            "Der Binaerbuffer hat folgenden Aufbau\nByte 0 : Datentyp\nByte 1 : Wortbreite");

        parameter.Comment.ShouldBe(
            "The binary buffer is laid out as follows\nByte 0 : Datentyp\nByte 1 : Wortbreite");
    }

    [Fact]
    public async Task Tab_separated_labelled_fields_are_split_onto_separate_lines()
    {
        var localizer = await LocalizerAsync();

        // A single source line: SGBDs use tabs to lay out labelled fields inside one comment.
        var parameter = Parameter(
            localizer,
            "Beschreibung:\t\tDummy-Daten\tDatenlänge:\t\t1 Byte\tEinheit:\t\tkeine");

        parameter.Comment!.Split('\n').ShouldBe(
            ["Beschreibung:", "Dummy-Daten", "Datenlänge:", "1 Byte", "Einheit:", "keine"]);
    }

    [Fact]
    public async Task Runs_of_spaces_are_treated_as_column_gaps_too()
    {
        var localizer = await LocalizerAsync();

        var parameter = Parameter(localizer, "Bereich:     0-255     Einheit:     keine");

        parameter.Comment!.Split('\n').ShouldBe(["Bereich:", "0-255", "Einheit:", "keine"]);
    }

    [Fact]
    public async Task A_single_spaced_sentence_is_left_alone()
    {
        var localizer = await LocalizerAsync();

        var parameter = Parameter(localizer, "Aenderungsindex max. 2-stellig ASCII inkl. Ziffern");

        parameter.Comment.ShouldBe("Aenderungsindex max. 2-stellig ASCII inkl. Ziffern");
    }

    [Fact]
    public async Task Untranslated_text_keeps_its_original_layout()
    {
        var localizer = await LocalizerAsync();

        // Nothing translates this, so its internal structure must survive rather than collapse.
        localizer.TranslateData("Byte 0 :  Datentyp").ShouldBe("Byte 0 :  Datentyp");
    }

    [Theory]
    [InlineData("CAS.PRG", false, "Variant")]
    [InlineData("d_motor.grp", true, "Group")]
    [InlineData("MSV70.prg", false, "Variant")]
    public async Task Ecu_picker_distinguishes_variants_from_group_files(
        string fileName,
        bool expectedGroup,
        string expectedLabel)
    {
        var localizer = await LocalizerAsync();
        var item = new SgbdListItemViewModel(fileName, localizer, canReachVehicle: true);

        item.IsGroup.ShouldBe(expectedGroup);
        item.KindLabel.ShouldBe(expectedLabel);
        item.DisplayName.ShouldBe(Path.GetFileNameWithoutExtension(fileName));
        item.Identifier.Kind.ShouldBe(expectedGroup ? SgbdKind.Group : SgbdKind.Variant);
    }

    [Fact]
    public async Task Group_files_are_unselectable_without_a_vehicle_and_say_why()
    {
        var localizer = await LocalizerAsync();
        var group = new SgbdListItemViewModel("d_motor.grp", localizer, canReachVehicle: false);

        group.IsSelectable.ShouldBeFalse();
        group.UnavailableNote.ShouldBe("needs a vehicle");
        group.Tooltip.ShouldBe("Group files identify the fitted ECU by asking the vehicle.");
    }

    [Fact]
    public async Task Variants_stay_selectable_without_a_vehicle()
    {
        var localizer = await LocalizerAsync();
        var variant = new SgbdListItemViewModel("CAS.PRG", localizer, canReachVehicle: false);

        // Most variants can be browsed offline; only group files need the car.
        variant.IsSelectable.ShouldBeTrue();
        variant.UnavailableNote.ShouldBeNull();
        variant.Tooltip.ShouldBe("One specific ECU.");
    }

    [Fact]
    public async Task Group_files_become_selectable_once_a_vehicle_can_answer()
    {
        var localizer = await LocalizerAsync();
        var group = new SgbdListItemViewModel("d_motor.grp", localizer, canReachVehicle: true);

        group.IsSelectable.ShouldBeTrue();
        group.UnavailableNote.ShouldBeNull();
        group.Tooltip.ShouldBe("A family of ECUs.");
    }
}
