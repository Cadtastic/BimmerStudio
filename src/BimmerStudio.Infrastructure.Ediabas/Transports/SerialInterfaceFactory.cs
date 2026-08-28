using BimmerStudio.Application.Abstractions;
using BimmerStudio.Domain.Connections;
using EdiabasLib;

namespace BimmerStudio.Infrastructure.Ediabas.Transports;

/// <summary>
/// FTDI K+DCAN cables and other serial adapters — the classic E-series interface.
/// </summary>
/// <remarks>
/// Uses the virtual COM port rather than FTDI's D2XX driver, because VCP is what exists on all
/// three platforms: <c>ftdi_sio</c> is in the Linux kernel and macOS has shipped an FTDI driver
/// since 10.9. The trade-off is that D2XX-only tricks are unavailable off Windows, notably the
/// 5-baud bit-banged slow init that the oldest K-line ECUs need. D-CAN cars are unaffected.
/// </remarks>
public sealed class SerialInterfaceFactory : IEdiabasInterfaceFactory
{
    public string TransportId => TransportIds.KDCanSerial;

    public EdInterfaceBase CreateInterface(ConnectionProfile profile, Workspace workspace)
    {
        var port = profile.Setting(SerialTransportSettings.Port);
        if (string.IsNullOrWhiteSpace(port))
        {
            throw new DiagnosticConnectionException(
                $"Connection '{profile.Name}' has no serial port configured.");
        }

        return new EdInterfaceObd { ComPort = port };
    }
}
