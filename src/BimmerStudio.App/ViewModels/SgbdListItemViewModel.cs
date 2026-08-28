using BimmerStudio.Application.Localization;
using BimmerStudio.Application.Modules;
using BimmerStudio.Domain.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BimmerStudio.App.ViewModels;

/// <summary>
/// One row in the ECU picker: a variant, a group file, or a non-selectable category header.
/// </summary>
/// <remarks>
/// Raw SGBD names are codes (<c>00swtkwp</c>, <c>d_kombi</c>), so recognised ones carry a
/// localised module name from the catalog and the list is sectioned by vehicle area. The raw
/// name stays visible beside the friendly one — it is what forums, traces and other tools use,
/// and it is the truth the friendly name merely annotates.
/// </remarks>
public sealed class SgbdListItemViewModel : ObservableObject
{
    private readonly ILocalizer _localizer;
    private readonly bool _canReachVehicle;

    private SgbdListItemViewModel(ILocalizer localizer, string categoryKey)
    {
        _localizer = localizer;
        IsHeader = true;
        FileName = string.Empty;
        CategoryKey = categoryKey;
        ModuleKey = null;
        Identifier = null;
    }

    public SgbdListItemViewModel(
        string fileName,
        ILocalizer localizer,
        bool canReachVehicle,
        ModuleResolution resolution)
    {
        _localizer = localizer;
        _canReachVehicle = canReachVehicle;
        FileName = fileName;
        Identifier = SgbdIdentifier.Parse(fileName);
        ModuleKey = resolution.ModuleKey;
        CategoryKey = resolution.CategoryKey;
    }

    /// <summary>A section divider in the dropdown. Never selectable, carries no file.</summary>
    public static SgbdListItemViewModel Header(string categoryKey, ILocalizer localizer) =>
        new(localizer, categoryKey);

    public bool IsHeader { get; }

    public string FileName { get; }

    public SgbdIdentifier? Identifier { get; }

    public string CategoryKey { get; }

    public string? ModuleKey { get; }

    public string DisplayName =>
        IsHeader ? CategoryName : Path.GetFileNameWithoutExtension(FileName);

    public string CategoryName => _localizer[$"Category_{CategoryKey}"];

    /// <summary>Localised module name, null when the raw name was not recognised.</summary>
    public string? ModuleName => ModuleKey is null ? null : _localizer[$"Module_{ModuleKey}"];

    public bool HasModuleName => ModuleKey is not null;

    public bool IsGroup => Identifier?.Kind == SgbdKind.Group;

    public string KindLabel => _localizer[IsGroup ? "Sgbd_Group" : "Sgbd_Variant"];

    /// <summary>Muted blue for groups so they read as a different kind of thing, not a warning.</summary>
    public string KindBrush => IsGroup ? "#58A6FF" : "#8B949E";

    /// <summary>
    /// Group files are offered only when a vehicle can answer: identifying the fitted ECU means
    /// asking it, which a simulation cannot do. Headers are structure, never selectable.
    /// </summary>
    public bool IsSelectable => !IsHeader && (_canReachVehicle || !IsGroup);

    /// <summary>Kept visible in the row itself, because tooltips on disabled items are unreliable.</summary>
    public string? UnavailableNote =>
        IsHeader || IsSelectable ? null : _localizer["Sgbd_RequiresConnection_Short"];

    public string? Tooltip
    {
        get
        {
            if (IsHeader)
            {
                return null;
            }

            if (!IsSelectable)
            {
                return _localizer["Sgbd_RequiresConnection_Desc"];
            }

            var kind = _localizer[IsGroup ? "Sgbd_Group_Desc" : "Sgbd_Variant_Desc"];
            return ModuleName is null ? kind : $"{ModuleName} — {CategoryName}\n{kind}";
        }
    }

    /// <summary>Re-evaluates every localised property after a language switch.</summary>
    public void RefreshTranslations()
    {
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(CategoryName));
        OnPropertyChanged(nameof(ModuleName));
        OnPropertyChanged(nameof(KindLabel));
        OnPropertyChanged(nameof(UnavailableNote));
        OnPropertyChanged(nameof(Tooltip));
    }

    public override string ToString() => DisplayName;
}
