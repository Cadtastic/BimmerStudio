using System.Text.RegularExpressions;
using BimmerStudio.Domain.Diagnostics;

namespace BimmerStudio.Domain.Safety;

/// <summary>
/// Classifies a job by what its name says it does.
/// </summary>
/// <remarks>
/// <para>
/// SGBDs carry no machine-readable notion of risk, so the name is the only signal available
/// before running a job — and running it to find out is precisely what must not happen. BMW's
/// naming is consistent enough for this to work: <c>_LESEN</c> reads, <c>_LOESCHEN</c> erases,
/// <c>STEUERN_</c> actuates, <c>_SCHREIBEN</c> and <c>CODIER</c> write.
/// </para>
/// <para>
/// Rules are ordered and the first match wins, so dangerous patterns are tested before
/// permissive ones: <c>FS_LOESCHEN</c> must not be read as a read merely because some other rule
/// mentions fault memory. Anything unmatched is <see cref="JobSafety.Unknown"/>, which is treated
/// as a write.
/// </para>
/// </remarks>
public sealed partial class JobSafetyClassifier
{
    private static readonly (Regex Pattern, JobSafety Safety)[] Rules =
    [
        // Highest risk first.
        (FlashPattern(), JobSafety.Flash),
        (CodingPattern(), JobSafety.Coding),
        (ActuatorPattern(), JobSafety.Actuator),
        (MemoryClearPattern(), JobSafety.MemoryClear),

        (CommInitPattern(), JobSafety.CommInit),
        (ReadPattern(), JobSafety.Read),
    ];

    public JobSafety Classify(string jobName)
    {
        if (string.IsNullOrWhiteSpace(jobName))
        {
            return JobSafety.Unknown;
        }

        var name = jobName.Trim();

        // Exact names only. A leading underscore means nothing: real SGBDs ship jobs like
        // _COD_SCHREIBEN and _FLASH_COMICRO that write and reprogram.
        if (ReservedJobNames.IsReserved(name))
        {
            return JobSafety.Read;
        }

        foreach (var (pattern, safety) in Rules)
        {
            if (pattern.IsMatch(name))
            {
                return safety;
            }
        }

        return JobSafety.Unknown;
    }

    /// <summary>Firmware programming: <c>FLASH</c>, <c>PROG</c>, <c>UPDATE_PROGRAMM</c>.</summary>
    [GeneratedRegex(
        @"FLASH|PROGRAMMIER|(^|_)PROG(_|$)|DOWNLOAD|UPLOAD|ERASE",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex FlashPattern();

    /// <summary>
    /// Anything that writes persistent data: coding, VIN and ZCS writes, adaptation resets,
    /// and the general <c>_SCHREIBEN</c> (write) suffix.
    /// </summary>
    [GeneratedRegex(
        @"CODIER|SCHREIB|WRITE|_SET(_|$)|EEPROM_W|(^|_)RESET(_|$)|ANLERN|ADAPTION|LERNEN",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CodingPattern();

    /// <summary>
    /// <c>STEUERN</c> drives outputs. The UDS family (<c>STEUERN_IO</c>,
    /// <c>STEUERN_ROUTINE</c>) matches the same prefix.
    /// </summary>
    [GeneratedRegex(
        @"^STEUERN|_STEUERN|STELLGLIED|AKTIV|(^|_)TEST(_|$)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ActuatorPattern();

    /// <summary>Fault, info and shadow memory erasure.</summary>
    [GeneratedRegex(
        @"LOESCH|LOSCH|CLEAR|(^|_)DEL(_|$)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex MemoryClearPattern();

    [GeneratedRegex(
        @"^INITIALISIERUNG$|^INIT$|^ENDE$|^STOP$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CommInitPattern();

    /// <summary>
    /// Reads: <c>_LESEN</c>, the <c>STATUS_</c> family, identification and info jobs.
    /// Reached only after every write pattern has been ruled out.
    /// </summary>
    [GeneratedRegex(
        @"LESEN|READ|^STATUS|^IDENT|^INFO|^AIF|^SERIENNUMMER|ABFRAGE|^MESSWERT",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ReadPattern();
}
