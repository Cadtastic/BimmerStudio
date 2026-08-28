namespace BimmerStudio.Domain.Connections;

/// <summary>
/// Identifiers of the transports shipped in the box. The set is open: a new transport is a new
/// provider registered with its own id, so this class is a convenience, not a closed enumeration.
/// </summary>
public static class TransportIds
{
    /// <summary>FTDI K+DCAN cable over a virtual serial port. The classic E-series interface.</summary>
    public const string KDCanSerial = "kdcan-serial";

    /// <summary>ENET / DoIP over Ethernet, used by F- and G-series cars.</summary>
    public const string Enet = "enet";

    /// <summary>Generic ELM327 adapter over Bluetooth RFCOMM or WiFi.</summary>
    public const string Elm327 = "elm327";

    /// <summary>Replays recorded traffic from EDIABAS simulation files. Needs no hardware.</summary>
    public const string Simulation = "simulation";
}
