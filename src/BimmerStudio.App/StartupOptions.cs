namespace BimmerStudio.App;

/// <summary>
/// Command-line startup automation, for demos and smoke tests:
/// <c>BimmerStudio --ecu-path C:\EDIABAS\Ecu --connect --load CAS --select-job AIF_LESEN --lang de</c>.
/// </summary>
/// <remarks>
/// Deliberately limited to actions the user could click themselves, against the simulation
/// transport only — automation never gets a faster path to hardware than a person has.
/// </remarks>
public sealed record StartupOptions(
    string? EcuDataPath = null,
    bool AutoConnect = false,
    string? LoadSgbd = null,
    string? SelectJob = null,
    string? Language = null)
{
    public static StartupOptions Parse(string[] args)
    {
        var options = new StartupOptions();

        for (var i = 0; i < args.Length; i++)
        {
            options = args[i] switch
            {
                "--ecu-path" when i + 1 < args.Length => options with { EcuDataPath = args[++i] },
                "--connect" => options with { AutoConnect = true },
                "--load" when i + 1 < args.Length => options with { LoadSgbd = args[++i] },
                "--select-job" when i + 1 < args.Length => options with { SelectJob = args[++i] },
                "--lang" when i + 1 < args.Length => options with { Language = args[++i] },
                _ => options,
            };
        }

        return options;
    }
}
