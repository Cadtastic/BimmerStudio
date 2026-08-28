using BimmerStudio.Application.Abstractions;
using BimmerStudio.Domain.Vehicles;

namespace BimmerStudio.Infrastructure.Ediabas.Tests;

/// <summary>
/// Locates a real EDIABAS <c>Ecu</c> folder for integration tests.
/// </summary>
/// <remarks>
/// SGBDs are BMW's property and are never committed, so these tests are opt-in: set
/// <c>BIMMERSTUDIO_ECU_PATH</c> to an <c>Ecu</c> directory to run them. They skip themselves
/// everywhere else, which is why CI stays green without any vehicle data.
/// </remarks>
public sealed class EcuDataFixture
{
    public const string PathVariable = "BIMMERSTUDIO_ECU_PATH";

    public EcuDataFixture()
    {
        var configured = Environment.GetEnvironmentVariable(PathVariable);

        if (!string.IsNullOrWhiteSpace(configured) && Directory.Exists(configured))
        {
            EcuPath = configured;
            SgbdFiles = Directory.GetFiles(EcuPath, "*.prg", SearchOption.TopDirectoryOnly);
            GroupFiles = Directory.GetFiles(EcuPath, "*.grp", SearchOption.TopDirectoryOnly);
        }
        else
        {
            SgbdFiles = [];
            GroupFiles = [];
        }
    }

    public string? EcuPath { get; }

    public IReadOnlyList<string> SgbdFiles { get; }

    public IReadOnlyList<string> GroupFiles { get; }

    public bool IsAvailable => EcuPath is not null && SgbdFiles.Count > 0;

    public string SkipReason =>
        $"Set {PathVariable} to an EDIABAS Ecu folder to run this test.";

    public Workspace CreateWorkspace(string? simulationPath = null) =>
        new(
            Guid.NewGuid(),
            "Integration",
            VehiclePlatform.ESeries,
            EcuPath ?? throw new InvalidOperationException(SkipReason),
            simulationPath);
}
