using System.Diagnostics;
using System.Runtime.CompilerServices;
using BimmerStudio.Application.Abstractions;
using BimmerStudio.Domain.Diagnostics;
using EdiabasLib;

namespace BimmerStudio.Infrastructure.Ediabas;

/// <summary>
/// One loaded SGBD on an <see cref="EdiabasConnection"/>. All interpreter work is funnelled back
/// through the connection's worker thread.
/// </summary>
internal sealed class EdiabasSession(
    EdiabasConnection connection,
    SgbdIdentifier requestedSgbd,
    string resolvedVariant) : IDiagnosticSession
{
    private int _invalidated;

    public SgbdIdentifier RequestedSgbd => requestedSgbd;

    public string ResolvedVariant => resolvedVariant;

    public async Task<IReadOnlyList<JobDescriptor>> GetJobsAsync(
        CancellationToken cancellationToken = default)
    {
        ThrowIfInvalidated();

        var names = await RunJobAsync(new JobRequest(ReservedJobs.Jobs), cancellationToken)
            .ConfigureAwait(false);

        return names.DataSets
            .Select(set => set.TextOrNull(ReservedJobs.JobNameResult))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .Select(JobDescriptor.NameOnly)
            .ToList();
    }

    public async Task<JobResult> ExecuteJobAsync(
        JobRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ThrowIfInvalidated();
        connection.ThrowIfStreaming();

        return await RunJobAsync(request, cancellationToken).ConfigureAwait(false);
    }

    public async IAsyncEnumerable<JobResult> ExecuteJobContinuousAsync(
        JobRequest request,
        TimeSpan interval,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentOutOfRangeException.ThrowIfLessThan(interval, TimeSpan.Zero);
        ThrowIfInvalidated();

        // Held for the lifetime of the stream so nothing else touches the bus meanwhile.
        using var reservation = connection.ReserveForStreaming();

        while (!cancellationToken.IsCancellationRequested)
        {
            JobResult result;
            try
            {
                result = await RunJobAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                yield break;
            }

            yield return result;

            if (interval > TimeSpan.Zero)
            {
                try
                {
                    await Task.Delay(interval, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    yield break;
                }
            }
        }
    }

    private async Task<JobResult> RunJobAsync(JobRequest request, CancellationToken cancellationToken)
    {
        var timestamp = Stopwatch.GetTimestamp();

        try
        {
            var resultSets = await connection.RunAsync(
                ediabas =>
                {
                    ExecuteOnInterpreter(ediabas, request);
                    return ediabas.ResultSets;
                },
                cancellationToken).ConfigureAwait(false);

            return ResultMapper.ToJobResult(
                request.JobName,
                resultSets,
                Stopwatch.GetElapsedTime(timestamp));
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not DiagnosticConnectionException)
        {
            // The interpreter defers an SGBD's automatic INITIALISIERUNG job until the first
            // real execution, so "no vehicle" surfaces here rather than at load time.
            if (EdiabasErrorClassifier.IndicatesMissingVehicle(ex))
            {
                throw new VehicleConnectionRequiredException(
                    requestedSgbd.BaseName,
                    $"'{requestedSgbd.BaseName}' needs a responding vehicle before "
                    + $"'{request.JobName}' can run: it initialises communication on first use.");
            }

            throw new JobExecutionException(
                request,
                $"Job '{request.JobName}' failed on {resolvedVariant}: {ex.Message}",
                ex);
        }
    }

    private static void ExecuteOnInterpreter(EdiabasNet ediabas, JobRequest request)
    {
        ediabas.ArgString = request.Arguments ?? string.Empty;
        ediabas.ArgBinaryStd = null;
        ediabas.ResultsRequests = request.ResultFilter ?? string.Empty;
        ediabas.ExecuteJob(request.JobName);
    }

    public async Task<JobDescriptor> DescribeJobAsync(
        string jobName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(jobName);
        ThrowIfInvalidated();
        connection.ThrowIfStreaming();

        var comments = await ReadCommentsAsync(jobName, cancellationToken).ConfigureAwait(false);

        var arguments = await ReadParametersAsync(
            ReservedJobs.Arguments,
            jobName,
            ReservedJobs.ArgumentResult,
            ReservedJobs.ArgumentTypeResult,
            ReservedJobs.ArgumentCommentPrefix,
            cancellationToken).ConfigureAwait(false);

        var results = await ReadParametersAsync(
            ReservedJobs.Results,
            jobName,
            ReservedJobs.ResultResult,
            ReservedJobs.ResultTypeResult,
            ReservedJobs.ResultCommentPrefix,
            cancellationToken).ConfigureAwait(false);

        return new JobDescriptor(jobName, comments, arguments, results);
    }

    /// <summary>
    /// Runs a metadata job, treating failure as "this SGBD documents nothing here".
    /// </summary>
    /// <remarks>
    /// Description data is optional and unevenly present across SGBDs, and a missing section
    /// surfaces as an interpreter error rather than an empty result. Failing the whole job list
    /// because one SGBD lacks argument documentation would be wrong.
    /// </remarks>
    private async Task<JobResult?> TryRunMetadataJobAsync(
        JobRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            return await RunJobAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (JobExecutionException)
        {
            return null;
        }
    }

    private async Task<IReadOnlyList<string>> ReadCommentsAsync(
        string jobName,
        CancellationToken cancellationToken)
    {
        var result = await TryRunMetadataJobAsync(
            new JobRequest(ReservedJobs.JobComments, jobName),
            cancellationToken).ConfigureAwait(false);

        if (result is null)
        {
            return [];
        }

        return result.DataSets
            .SelectMany(set => OrderedByIndex(set, ReservedJobs.JobCommentPrefix))
            .ToList();
    }

    private async Task<IReadOnlyList<JobParameterInfo>> ReadParametersAsync(
        string metadataJob,
        string jobName,
        string nameKey,
        string typeKey,
        string commentPrefix,
        CancellationToken cancellationToken)
    {
        var result = await TryRunMetadataJobAsync(new JobRequest(metadataJob, jobName), cancellationToken)
            .ConfigureAwait(false);

        if (result is null)
        {
            return [];
        }

        var parameters = new List<JobParameterInfo>(result.DataSets.Count);
        foreach (var set in result.DataSets)
        {
            var name = set.TextOrNull(nameKey);
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var comment = string.Join(' ', OrderedByIndex(set, commentPrefix));
            parameters.Add(new JobParameterInfo(
                name,
                set.TextOrNull(typeKey),
                string.IsNullOrWhiteSpace(comment) ? null : comment));
        }

        return parameters;
    }

    /// <summary>
    /// Collects numbered keys such as <c>JOBCOMMENT0</c>, <c>JOBCOMMENT1</c> in index order.
    /// Dictionary order is not guaranteed, and lexical order would put 10 before 2.
    /// </summary>
    private static IEnumerable<string> OrderedByIndex(ResultSet set, string prefix) =>
        set
            .Where(value => value.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Select(value => (
                Index: int.TryParse(value.Name[prefix.Length..], out var index) ? index : int.MaxValue,
                Text: value.AsText()))
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Text))
            .OrderBy(entry => entry.Index)
            .Select(entry => entry.Text!);

    /// <summary>Marks the session unusable because another SGBD was loaded on the connection.</summary>
    internal void Invalidate() => Interlocked.Exchange(ref _invalidated, 1);

    private void ThrowIfInvalidated()
    {
        if (Volatile.Read(ref _invalidated) == 1)
        {
            throw new DiagnosticConnectionException(
                $"The session for '{requestedSgbd.BaseName}' is no longer active because another "
                + "SGBD was loaded on this connection.");
        }
    }

    public ValueTask DisposeAsync()
    {
        Invalidate();
        return ValueTask.CompletedTask;
    }
}
