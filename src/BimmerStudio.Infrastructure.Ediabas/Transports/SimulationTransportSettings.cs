namespace BimmerStudio.Infrastructure.Ediabas.Transports;

/// <summary>
/// Setting keys understood by the simulation transport.
/// </summary>
public static class SimulationTransportSettings
{
    /// <summary>
    /// Folder of <c>.sim</c> files. Overrides the workspace's own simulation folder when set.
    /// </summary>
    public const string SimulationPath = "simulationPath";
}
