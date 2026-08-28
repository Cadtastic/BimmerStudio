using BimmerStudio.Domain.Vehicles;

namespace BimmerStudio.Application.Abstractions;

/// <summary>
/// An environment: which vehicle platform is being worked on and where its data lives.
/// </summary>
/// <param name="EcuDataPath">
/// Directory holding the compiled SGBDs (<c>.prg</c>) and group files (<c>.grp</c>) — the
/// <c>Ecu</c> folder of an EDIABAS or SP-Daten installation. Never bundled with the app.
/// </param>
/// <param name="SimulationPath">Directory of EDIABAS <c>.sim</c> files, when simulating.</param>
public sealed record Workspace(
    Guid Id,
    string Name,
    VehiclePlatform Platform,
    string EcuDataPath,
    string? SimulationPath = null,
    string? TracePath = null,
    Guid? DefaultConnectionProfileId = null)
{
    public static Workspace Create(string name, VehiclePlatform platform, string ecuDataPath) =>
        new(Guid.NewGuid(), name, platform, ecuDataPath);
}
