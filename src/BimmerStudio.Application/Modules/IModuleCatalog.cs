namespace BimmerStudio.Application.Modules;

/// <summary>
/// Maps raw SGBD file names — <c>d_kombi</c>, <c>MSV70</c>, <c>04DDE731</c> — to the vehicle
/// module they belong to, so the ECU picker can show "Instrument cluster" instead of a code.
/// </summary>
public interface IModuleCatalog
{
    /// <summary>Ordered category keys, defining the display order of picker sections.</summary>
    IReadOnlyList<string> CategoryOrder { get; }

    ModuleResolution Resolve(string sgbdBaseName);
}
