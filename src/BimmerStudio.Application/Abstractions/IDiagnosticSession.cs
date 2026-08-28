using BimmerStudio.Domain.Diagnostics;

namespace BimmerStudio.Application.Abstractions;

/// <summary>
/// One loaded SGBD on a connection, and the jobs that can be run against it.
/// </summary>
/// <remarks>
/// A session is not thread-safe for concurrent job execution: the vehicle bus carries one
/// request/response exchange at a time. Implementations serialise calls rather than reject them,
/// except that starting a job while a continuous one is streaming throws.
/// </remarks>
public interface IDiagnosticSession : IAsyncDisposable
{
    /// <summary>The SGBD that was asked for, which may be a group file.</summary>
    SgbdIdentifier RequestedSgbd { get; }

    /// <summary>
    /// The concrete ECU variant in use. Equal to <see cref="RequestedSgbd"/> for a variant;
    /// for a group file it is what EDIABAS resolved by interrogating the vehicle.
    /// </summary>
    string ResolvedVariant { get; }

    /// <summary>
    /// Names of every job the SGBD declares, in one interpreter call. Reads the SGBD file only,
    /// so it works with no vehicle attached.
    /// </summary>
    /// <remarks>
    /// Deliberately shallow: documentation for a job costs one call per job, which is wasted on
    /// a list the user is only scanning. Call <see cref="DescribeJobAsync"/> for the selected job.
    /// </remarks>
    Task<IReadOnlyList<JobDescriptor>> GetJobsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Full detail for one job: its comments, arguments and results as the SGBD documents them.
    /// </summary>
    /// <remarks>
    /// Metadata is best-effort. Many SGBDs carry partial or no description data, so missing
    /// documentation yields empty collections rather than an error.
    /// </remarks>
    Task<JobDescriptor> DescribeJobAsync(string jobName, CancellationToken cancellationToken = default);

    Task<JobResult> ExecuteJobAsync(JobRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Re-runs a job on an interval until cancelled, for watching live values. The session is
    /// reserved for the duration; other execution attempts fail while the stream is open.
    /// </summary>
    IAsyncEnumerable<JobResult> ExecuteJobContinuousAsync(
        JobRequest request,
        TimeSpan interval,
        CancellationToken cancellationToken = default);
}
