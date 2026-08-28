namespace BimmerStudio.Application.Abstractions;

/// <summary>
/// A diagnostics failure expressed in the app's own terms, so callers never catch
/// interpreter-specific exception types.
/// </summary>
public class DiagnosticConnectionException : Exception
{
    public DiagnosticConnectionException(string message)
        : base(message)
    {
    }

    public DiagnosticConnectionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
