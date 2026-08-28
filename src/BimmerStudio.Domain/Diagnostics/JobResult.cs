namespace BimmerStudio.Domain.Diagnostics;

/// <summary>
/// The outcome of one job execution.
/// </summary>
/// <param name="SystemResults">
/// EDIABAS result set 0, describing the call rather than the payload: <c>JOBSTATUS</c>,
/// <c>VARIANTE</c>, <c>UBATT</c> and similar.
/// </param>
/// <param name="DataSets">The remaining result sets, one per record the job returned.</param>
public sealed record JobResult(
    string JobName,
    ResultSet SystemResults,
    IReadOnlyList<ResultSet> DataSets,
    TimeSpan Duration)
{
    /// <summary>
    /// EDIABAS reports job-level failures in <c>JOBSTATUS</c> rather than by faulting, so an
    /// empty or <c>OKAY</c> status is the success case.
    /// </summary>
    public string JobStatus => SystemResults.TextOrNull("JOBSTATUS") ?? string.Empty;

    public bool IsSuccess =>
        JobStatus.Length == 0 || JobStatus.Equals("OKAY", StringComparison.OrdinalIgnoreCase);

    /// <summary>The ECU variant EDIABAS resolved, populated when a group file was loaded.</summary>
    public string? Variant => SystemResults.TextOrNull("VARIANTE");
}
