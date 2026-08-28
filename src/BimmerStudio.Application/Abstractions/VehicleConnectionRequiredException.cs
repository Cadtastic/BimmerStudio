namespace BimmerStudio.Application.Abstractions;

/// <summary>
/// The SGBD cannot be used without a responding vehicle.
/// </summary>
/// <remarks>
/// Not every ECU description file can be inspected offline. Many engine and transmission SGBDs
/// (MSV70, MSD80, GS19, the DDE family) declare an <c>INITIALISIERUNG</c> job that EDIABAS runs
/// automatically before anything else, and that job talks to the ECU. Loading such an SGBD with
/// no car attached therefore fails at load time, before any job of ours runs. Group files behave
/// the same way, since resolving a variant means asking the vehicle which one is fitted.
/// <para>
/// This is a normal condition rather than a defect, so it gets its own type: the UI should
/// prompt the user to connect instead of reporting a fault.
/// </para>
/// </remarks>
public sealed class VehicleConnectionRequiredException(string sgbdName, string message)
    : DiagnosticConnectionException(message)
{
    public string SgbdName { get; } = sgbdName;
}
