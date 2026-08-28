using BimmerStudio.Domain.Diagnostics;

namespace BimmerStudio.Application.Abstractions;

/// <summary>
/// A job failed to execute. Distinct from a job that ran and reported a non-OK
/// <c>JOBSTATUS</c>, which is a normal result rather than an error.
/// </summary>
public sealed class JobExecutionException(
    JobRequest request,
    string message,
    Exception? innerException = null)
    : DiagnosticConnectionException(message, innerException ?? new InvalidOperationException(message))
{
    public JobRequest Request { get; } = request;
}
