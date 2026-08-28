using BimmerStudio.Domain.Connections;

namespace BimmerStudio.Application.Abstractions;

/// <summary>
/// Opens connections for a profile. The seam at which the whole diagnostics stack could be
/// replaced — for example by a PSdZ-based implementation for F/G cars.
/// </summary>
public interface IDiagnosticConnectionFactory
{
    /// <exception cref="DiagnosticConnectionException">The link could not be established.</exception>
    Task<IDiagnosticConnection> ConnectAsync(
        ConnectionProfile profile,
        Workspace workspace,
        CancellationToken cancellationToken = default);
}
