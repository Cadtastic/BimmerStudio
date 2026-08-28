using BimmerStudio.Domain.Diagnostics;
using BimmerStudio.Domain.Safety;

namespace BimmerStudio.Domain.Tests;

public sealed class JobSafetyClassifierTests
{
    private readonly JobSafetyClassifier _classifier = new();

    [Theory]
    [InlineData("FS_LESEN")]           // read fault memory
    [InlineData("IDENT")]
    [InlineData("IDENTIFIKATION")]
    [InlineData("STATUS_UBATT")]       // battery voltage
    [InlineData("STATUS_LESEN")]       // UDS read by DID
    [InlineData("AIF_LESEN")]          // workshop info field
    [InlineData("C_FG_LESEN")]         // read VIN
    [InlineData("_JOBS")]              // reserved metadata, never reaches the car
    [InlineData("_ARGUMENTS")]
    public void Classifies_read_jobs_as_read(string jobName) =>
        _classifier.Classify(jobName).ShouldBe(JobSafety.Read);

    [Theory]
    [InlineData("FS_LOESCHEN")]        // clear fault memory
    [InlineData("IS_LOESCHEN")]        // clear info memory
    [InlineData("FEHLERSPEICHER_LOESCHEN")]
    public void Classifies_erase_jobs_as_memory_clear(string jobName) =>
        _classifier.Classify(jobName).ShouldBe(JobSafety.MemoryClear);

    [Theory]
    [InlineData("STEUERN")]
    [InlineData("STEUERN_IO")]         // UDS I/O control
    [InlineData("STEUERN_ROUTINE")]
    [InlineData("STELLGLIED_TEST")]
    public void Classifies_actuation_jobs_as_actuator(string jobName) =>
        _classifier.Classify(jobName).ShouldBe(JobSafety.Actuator);

    [Theory]
    [InlineData("SG_CODIEREN")]        // the standard NCS coding job
    [InlineData("CODIERDATEN_SCHREIBEN")]
    [InlineData("AIF_SCHREIBEN")]
    [InlineData("C_FG_SCHREIBEN")]     // write VIN
    public void Classifies_write_jobs_as_coding(string jobName) =>
        _classifier.Classify(jobName).ShouldBe(JobSafety.Coding);

    [Theory]
    [InlineData("FLASH_PROGRAMMIEREN")]
    [InlineData("PROGRAMMIERUNG_START")]
    [InlineData("SPEICHER_ERASE")]
    public void Classifies_programming_jobs_as_flash(string jobName) =>
        _classifier.Classify(jobName).ShouldBe(JobSafety.Flash);

    [Fact]
    public void Classifies_initialisation_as_comm_init() =>
        _classifier.Classify("INITIALISIERUNG").ShouldBe(JobSafety.CommInit);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("WHAT_IS_THIS")]
    [InlineData("XYZZY")]
    public void Treats_unrecognised_names_as_unknown(string jobName) =>
        _classifier.Classify(jobName).ShouldBe(JobSafety.Unknown);

    [Fact]
    public void Unknown_is_not_read_only_so_it_cannot_run_in_read_only_mode() =>
        JobSafety.Unknown.IsReadOnly().ShouldBeFalse();

    [Theory]
    [InlineData(JobSafety.Read, true)]
    [InlineData(JobSafety.CommInit, true)]
    [InlineData(JobSafety.MemoryClear, false)]
    [InlineData(JobSafety.Actuator, false)]
    [InlineData(JobSafety.Coding, false)]
    [InlineData(JobSafety.Flash, false)]
    [InlineData(JobSafety.Unknown, false)]
    public void Only_read_and_comm_init_are_read_only(JobSafety safety, bool expected) =>
        safety.IsReadOnly().ShouldBe(expected);

    /// <summary>
    /// The ordering guarantee that matters most. A name containing a read-ish word must not be
    /// classified as a read when it also says it erases or writes.
    /// </summary>
    [Theory]
    [InlineData("STATUS_LOESCHEN", JobSafety.MemoryClear)]
    [InlineData("FS_LESEN_UND_LOESCHEN", JobSafety.MemoryClear)]
    [InlineData("IDENT_SCHREIBEN", JobSafety.Coding)]
    [InlineData("STATUS_FLASH", JobSafety.Flash)]
    public void Dangerous_patterns_win_over_read_patterns(string jobName, JobSafety expected) =>
        _classifier.Classify(jobName).ShouldBe(expected);

    [Fact]
    public void Classification_is_case_insensitive() =>
        _classifier.Classify("fs_loeschen").ShouldBe(JobSafety.MemoryClear);

    /// <summary>
    /// A leading underscore does not mean "EDIABAS reserved". These are real job names taken
    /// from shipped SGBDs, and an earlier version of the classifier waved all of them through as
    /// harmless reads because they start with an underscore.
    /// </summary>
    [Theory]
    [InlineData("_COD_SCHREIBEN", JobSafety.Coding)]
    [InlineData("_CODIERDATEN_KOMPLETT_SCHREIBEN_LEAR", JobSafety.Coding)]
    [InlineData("_HERSTELLERDATEN_SMC_LEAR_SCHREIBEN", JobSafety.Coding)]
    [InlineData("_HISTORY_LOESCHEN", JobSafety.MemoryClear)]
    [InlineData("_FS_BUS_LOESCHEN", JobSafety.MemoryClear)]
    [InlineData("_FLASH_COMICRO", JobSafety.Flash)]
    [InlineData("_STEUERN_MAC_SCHREIBEN", JobSafety.Coding)]
    [InlineData("_STEUERN_STEUERGERAET_CODIEREN", JobSafety.Coding)]
    public void Underscore_prefixed_manufacturer_jobs_are_not_treated_as_reserved(
        string jobName,
        JobSafety expected) =>
        _classifier.Classify(jobName).ShouldBe(expected);

    [Theory]
    [InlineData("_JOBS")]
    [InlineData("_JOBCOMMENTS")]
    [InlineData("_ARGUMENTS")]
    [InlineData("_RESULTS")]
    [InlineData("_VERSIONINFO")]
    [InlineData("_TABLES")]
    [InlineData("_TABLE")]
    public void Only_the_known_reserved_names_are_reserved(string jobName)
    {
        ReservedJobNames.IsReserved(jobName).ShouldBeTrue();
        _classifier.Classify(jobName).ShouldBe(JobSafety.Read);
    }

    [Theory]
    [InlineData("_COD_SCHREIBEN")]
    [InlineData("_FLASH_COMICRO")]
    [InlineData("_JOBSOMETHING")]
    public void Reserved_membership_is_exact_not_prefix_based(string jobName) =>
        ReservedJobNames.IsReserved(jobName).ShouldBeFalse();
}
