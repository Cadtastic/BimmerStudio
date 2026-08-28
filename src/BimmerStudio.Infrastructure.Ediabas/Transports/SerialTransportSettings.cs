namespace BimmerStudio.Infrastructure.Ediabas.Transports;

/// <summary>
/// Setting keys understood by the serial-based transports.
/// </summary>
public static class SerialTransportSettings
{
    /// <summary>
    /// Serial device: <c>COM4</c> on Windows, <c>/dev/ttyUSB0</c> on Linux,
    /// <c>/dev/tty.usbserial-XXXX</c> on macOS.
    /// </summary>
    public const string Port = "port";

    /// <summary>Host and port of an ELM327 WiFi adapter, for example <c>192.168.0.10:35000</c>.</summary>
    public const string Endpoint = "endpoint";
}
