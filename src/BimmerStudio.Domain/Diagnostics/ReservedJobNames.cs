namespace BimmerStudio.Domain.Diagnostics;

/// <summary>
/// The jobs EDIABAS answers itself from the SGBD file, without ever reaching the vehicle.
/// </summary>
/// <remarks>
/// <para>
/// This is a closed set, and treating it as one matters for safety. A leading underscore does
/// <em>not</em> mean "reserved": real SGBDs ship manufacturer jobs such as <c>_COD_SCHREIBEN</c>,
/// <c>_HISTORY_LOESCHEN</c> and <c>_FLASH_COMICRO</c>, which write coding data, erase memory and
/// reprogram flash. Classifying by the <c>_</c> prefix would mark those as harmless reads.
/// </para>
/// <para>
/// Membership is therefore by exact name only.
/// </para>
/// </remarks>
public static class ReservedJobNames
{
    public const string Jobs = "_JOBS";
    public const string JobComments = "_JOBCOMMENTS";
    public const string Arguments = "_ARGUMENTS";
    public const string Results = "_RESULTS";
    public const string VersionInfo = "_VERSIONINFO";
    public const string Tables = "_TABLES";
    public const string Table = "_TABLE";

    private static readonly HashSet<string> Names = new(StringComparer.OrdinalIgnoreCase)
    {
        Jobs,
        JobComments,
        Arguments,
        Results,
        VersionInfo,
        Tables,
        Table,
    };

    public static IReadOnlyCollection<string> All => Names;

    /// <summary>
    /// True only for the interpreter's own metadata jobs. Never infers from the name's shape.
    /// </summary>
    public static bool IsReserved(string? jobName) =>
        !string.IsNullOrWhiteSpace(jobName) && Names.Contains(jobName.Trim());
}
