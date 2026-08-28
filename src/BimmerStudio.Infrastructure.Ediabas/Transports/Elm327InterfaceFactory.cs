using BimmerStudio.Application.Abstractions;
using BimmerStudio.Domain.Connections;
using EdiabasLib;

namespace BimmerStudio.Infrastructure.Ediabas.Transports;

/// <summary>
/// Generic ELM327 adapters over WiFi or Bluetooth RFCOMM.
/// </summary>
/// <remarks>
/// Limited by the hardware, not by this code: stock ELM327 firmware cannot speak the K-line
/// protocols older E-series ECUs use, and is marginal even on D-CAN. Treat it as a convenience
/// for newer E-series cars and prefer a K+DCAN cable where timing matters.
/// <para>
/// Bluetooth adapters are reached as ordinary serial devices (<c>/dev/rfcomm0</c>, or the
/// outgoing COM port Windows creates when the device is paired), so no Bluetooth stack is needed.
/// </para>
/// </remarks>
internal sealed class Elm327InterfaceFactory : IEdiabasInterfaceFactory
{
    /// <summary>Prefix EdInterfaceObd uses to route a port string to its ELM327 WiFi handler.</summary>
    private const string WifiPortPrefix = "ELM327WIFI:";

    public string TransportId => TransportIds.Elm327;

    public EdInterfaceBase CreateInterface(ConnectionProfile profile, Workspace workspace)
    {
        var endpoint = profile.Setting(SerialTransportSettings.Endpoint);
        if (!string.IsNullOrWhiteSpace(endpoint))
        {
            return new EdInterfaceObd { ComPort = WifiPortPrefix + endpoint };
        }

        var port = profile.Setting(SerialTransportSettings.Port);
        if (!string.IsNullOrWhiteSpace(port))
        {
            return new EdInterfaceObd { ComPort = port };
        }

        throw new DiagnosticConnectionException(
            $"Connection '{profile.Name}' needs either a serial port or a WiFi endpoint.");
    }
}
