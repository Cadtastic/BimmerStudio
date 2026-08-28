using BimmerStudio.Application.Abstractions;
using BimmerStudio.Domain.Connections;
using EdiabasLib;

namespace BimmerStudio.Infrastructure.Ediabas.Transports;

/// <summary>
/// Builds the EdiabasLib interface for one transport. Adding a transport means adding one
/// implementation of this and registering it; nothing else in the app changes.
/// </summary>
public interface IEdiabasInterfaceFactory
{
    /// <summary>Matches <see cref="ConnectionProfile.TransportId"/>. See <see cref="TransportIds"/>.</summary>
    string TransportId { get; }

    /// <summary>
    /// Creates and configures the interface for a profile. Ownership passes to the caller, which
    /// assigns it to an <see cref="EdiabasNet"/> and disposes both together.
    /// </summary>
    EdInterfaceBase CreateInterface(ConnectionProfile profile, Workspace workspace);

    /// <summary>
    /// Applies transport-specific interpreter settings, such as enabling simulation.
    /// Called after the interface is attached and the ECU path is set.
    /// </summary>
    void ConfigureRuntime(EdiabasNet ediabas, ConnectionProfile profile, Workspace workspace)
    {
    }
}
