using BimmerStudio.Application.Abstractions;
using BimmerStudio.Domain.Connections;
using EdiabasLib;

namespace BimmerStudio.Infrastructure.Ediabas.Transports;

/// <summary>
/// ENET / DoIP over Ethernet, how F- and G-series cars are reached.
/// </summary>
/// <remarks>
/// Present from the start so the extension point to newer platforms is real rather than
/// aspirational: the transport works today, and what remains for F/G support is the coding-data
/// layer (PSdZData rather than SP-Daten), not the link.
/// </remarks>
public sealed class EnetInterfaceFactory : IEdiabasInterfaceFactory
{
    /// <summary>Discovers the gateway by UDP broadcast rather than requiring a fixed address.</summary>
    public const string AutoDetect = "auto";

    public string TransportId => TransportIds.Enet;

    public EdInterfaceBase CreateInterface(ConnectionProfile profile, Workspace workspace)
    {
        var host = profile.Setting(EnetTransportSettings.RemoteHost, AutoDetect);

        return new EdInterfaceEnet
        {
            RemoteHost = host.Equals(AutoDetect, StringComparison.OrdinalIgnoreCase)
                ? "auto:all"
                : host,
        };
    }
}
