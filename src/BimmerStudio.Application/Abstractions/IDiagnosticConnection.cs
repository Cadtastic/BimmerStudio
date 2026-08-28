using BimmerStudio.Domain.Connections;
using BimmerStudio.Domain.Diagnostics;

namespace BimmerStudio.Application.Abstractions;

/// <summary>
/// A live link to a vehicle, or to a simulation standing in for one.
/// </summary>
/// <remarks>
/// Mirrors how EDIABAS actually works: the connection owns the interface handle, and loading an
/// SGBD switches what that handle is pointed at. Only one session is active at a time.
/// </remarks>
public interface IDiagnosticConnection : IAsyncDisposable
{
    ConnectionProfile Profile { get; }

    ConnectionState State { get; }

    /// <summary>
    /// Loads an SGBD and returns a session for it, superseding any previous session on this
    /// connection. Loading a group file interrogates the vehicle to resolve the fitted variant,
    /// so it fails without a responding ECU.
    /// </summary>
    /// <exception cref="SessionBusyException">A continuous job is streaming.</exception>
    Task<IDiagnosticSession> OpenSessionAsync(
        SgbdIdentifier sgbd,
        CancellationToken cancellationToken = default);
}
