namespace BimmerStudio.Domain.Diagnostics;

/// <summary>
/// A request to run one job on the currently loaded SGBD.
/// </summary>
/// <param name="JobName">
/// Job name as declared by the SGBD, for example <c>FS_LESEN</c>. Job names are German protocol
/// identifiers and are never translated.
/// </param>
/// <param name="Arguments">
/// Semicolon-separated argument string, exactly as EDIABAS expects it. Null when the job takes none.
/// </param>
/// <param name="ResultFilter">
/// Optional semicolon-separated list restricting which results are returned. Null returns all.
/// </param>
public sealed record JobRequest(string JobName, string? Arguments = null, string? ResultFilter = null)
{
    public static JobRequest For(string jobName) => new(jobName);

    public override string ToString() =>
        string.IsNullOrEmpty(Arguments) ? JobName : $"{JobName}({Arguments})";
}
