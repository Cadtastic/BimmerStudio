namespace BimmerStudio.Infrastructure.Settings;

/// <summary>
/// User preferences persisted between runs.
/// </summary>
/// <remarks>
/// The workspace fields are restored into the setup pane but never auto-connect: reopening the
/// app should put the previous settings back, not silently start talking to a vehicle.
/// </remarks>
public sealed record AppSettings(
    string? LanguageId = null,
    string? LastEcuDataPath = null,
    string? LastSimulationPath = null,
    string? LastTransportId = null,
    string? LastSerialPort = null,
    string? LastEnetHost = null);
