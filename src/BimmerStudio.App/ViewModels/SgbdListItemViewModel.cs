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
public sealed class SgbdListItemViewModel(string fileName, ILocalizer localizer)
{
    public string FileName { get; } = fileName;

    public SgbdIdentifier Identifier { get; } = SgbdIdentifier.Parse(fileName);

    public string DisplayName => Path.GetFileNameWithoutExtension(FileName);

    public bool IsGroup => Identifier.Kind == SgbdKind.Group;

    public string KindLabel => localizer[IsGroup ? "Sgbd_Group" : "Sgbd_Variant"];

    public string KindDescription => localizer[IsGroup ? "Sgbd_Group_Desc" : "Sgbd_Variant_Desc"];

    /// <summary>Muted blue for groups so they read as a different kind of thing, not a warning.</summary>
    public string KindBrush => IsGroup ? "#58A6FF" : "#8B949E";

    public override string ToString() => DisplayName;
}
