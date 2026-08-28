namespace BimmerStudio.Application.Abstractions;

/// <summary>
/// Thrown when work is requested on a connection that is reserved by a continuous job.
/// </summary>
public sealed class SessionBusyException(string message) : DiagnosticConnectionException(message);
