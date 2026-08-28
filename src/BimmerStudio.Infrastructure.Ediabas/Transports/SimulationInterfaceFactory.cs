using BimmerStudio.Application.Abstractions;
using BimmerStudio.Domain.Connections;
using EdiabasLib;

namespace BimmerStudio.Infrastructure.Ediabas.Transports;

/// <summary>
/// Replays recorded traffic from EDIABAS <c>.sim</c> files instead of talking to a car.
/// </summary>
/// <remarks>
/// The development and test seam: the whole stack above it — interpreter, session engine, use
/// cases, UI — runs unchanged with no hardware present. A simulation only answers the requests
/// its file contains, so it proves plumbing and job handling rather than protocol coverage.
/// </remarks>
public sealed class SimulationInterfaceFactory : IEdiabasInterfaceFactory
{
    public string TransportId => TransportIds.Simulation;

    public EdInterfaceBase CreateInterface(ConnectionProfile profile, Workspace workspace) =>
        new EdInterfaceObd { ComPort = "COM1" };

    public void ConfigureRuntime(EdiabasNet ediabas, ConnectionProfile profile, Workspace workspace)
    {
        var simulationPath = profile.Setting(SimulationTransportSettings.SimulationPath)
            ?? workspace.SimulationPath;

        if (string.IsNullOrWhiteSpace(simulationPath))
        {
            throw new DiagnosticConnectionException(
                $"Connection '{profile.Name}' is a simulation but no simulation folder is set.");
        }

        if (!Directory.Exists(simulationPath))
        {
            throw new DiagnosticConnectionException(
                $"Simulation folder '{simulationPath}' does not exist.");
        }

        ediabas.SetConfigProperty("SimulationPath", simulationPath);
        ediabas.SetConfigProperty("Simulation", "1");
    }
}
