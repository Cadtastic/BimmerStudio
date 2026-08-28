using System.Collections.ObjectModel;
using BimmerStudio.Application.Localization;
using BimmerStudio.Domain.Diagnostics;

namespace BimmerStudio.App.ViewModels;

/// <summary>
/// One result set rendered as name/value rows.
/// </summary>
public sealed class ResultSetViewModel
{
    private ResultSetViewModel(string title, ResultSet resultSet, ILocalizer localizer, bool isSystemSet)
    {
        Title = title;
        IsSystemSet = isSystemSet;
        Rows =
        [
            .. resultSet
                .OrderBy(value => value.Name, StringComparer.OrdinalIgnoreCase)
                .Select(value => new ResultRowViewModel(value, localizer, isSystemSet)),
        ];
    }

    /// <summary>
    /// EDIABAS result set 0: what the interpreter reports about the call itself rather than
    /// the payload.
    /// </summary>
    public static ResultSetViewModel System(ResultSet resultSet, ILocalizer localizer) =>
        new(localizer["Results_SystemSet"], resultSet, localizer, isSystemSet: true);

    /// <summary>One payload set. A fault-memory read returns one per stored fault.</summary>
    public static ResultSetViewModel Data(int index, ResultSet resultSet, ILocalizer localizer) =>
        new(localizer.Format("Results_DataSetFormat", index), resultSet, localizer, isSystemSet: false);

    public string Title { get; }

    public bool IsSystemSet { get; }

    public ObservableCollection<ResultRowViewModel> Rows { get; }

    public int Count => Rows.Count;
}
