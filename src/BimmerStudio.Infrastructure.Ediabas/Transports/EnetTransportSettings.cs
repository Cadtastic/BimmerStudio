namespace BimmerStudio.Infrastructure.Ediabas.Transports;

/// <summary>
/// Setting keys understood by the ENET transport.
/// </summary>
internal static class EnetTransportSettings
{
    /// <summary>
    /// Gateway address, or <c>auto</c> to find it by UDP broadcast.
    /// </summary>
    public const string RemoteHost = "remoteHost";
}
