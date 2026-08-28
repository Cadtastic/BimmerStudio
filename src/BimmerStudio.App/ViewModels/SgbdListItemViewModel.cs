using BimmerStudio.Application.Localization;
using BimmerStudio.Domain.Diagnostics;

namespace BimmerStudio.App.ViewModels;

/// <summary>
/// One entry in the ECU picker: either a concrete ECU variant or a group file.
/// </summary>
/// <remarks>
/// Both kinds are offered. A group file is not a lesser variant — it is the entry point you want
/// when the fitted ECU is unknown, because opening one makes the interpreter ask the car which
/// variant it has. Hiding them would remove the only way to reach an ECU whose part number you
/// do not already know.
/// </remarks>
/// <param name="canReachVehicle">
/// Whether the active connection can talk to a car. False for the simulation transport, which is
/// what makes group files unusable: identifying an ECU means asking it.
/// </param>
public sealed class SgbdListItemViewModel(
    string fileName,
    ILocalizer localizer,
    bool canReachVehicle)
{
    public string FileName { get; } = fileName;

    public SgbdIdentifier Identifier { get; } = SgbdIdentifier.Parse(fileName);

    public string DisplayName => Path.GetFileNameWithoutExtension(FileName);

    public bool IsGroup => Identifier.Kind == SgbdKind.Group;

    public string KindLabel => localizer[IsGroup ? "Sgbd_Group" : "Sgbd_Variant"];

    /// <summary>Muted blue for groups so they read as a different kind of thing, not a warning.</summary>
    public string KindBrush => IsGroup ? "#58A6FF" : "#8B949E";

    /// <summary>
    /// Group files are offered only when a vehicle can answer. Roughly nine in ten cannot be
    /// opened at all without one, and the few that can are stripped-down virtual-ECU stubs whose
    /// generic job set is reachable from any variant anyway — so offering them all would mostly
    /// be offering failures.
    /// </summary>
    public bool IsSelectable => canReachVehicle || !IsGroup;

    /// <summary>Kept visible in the row itself, because tooltips on disabled items are unreliable.</summary>
    public string? UnavailableNote =>
        IsSelectable ? null : localizer["Sgbd_NeedsVehicle_Short"];

    public string Tooltip =>
        IsSelectable
            ? localizer[IsGroup ? "Sgbd_Group_Desc" : "Sgbd_Variant_Desc"]
            : localizer["Sgbd_NeedsVehicle_Desc"];

    public override string ToString() => DisplayName;
}
