using System.Collections.ObjectModel;
using BimmerStudio.Domain.Diagnostics;

namespace BimmerStudio.App.ViewModels;

/// <summary>
/// One result set rendered as name/value rows.
/// </summary>
public sealed class ResultSetViewModel(string title, ResultSet resultSet)
{
    public string Title { get; } = title;

    public ObservableCollection<ResultRowViewModel> Rows { get; } =
    [
        .. resultSet
            .OrderBy(value => value.Name, StringComparer.OrdinalIgnoreCase)
            .Select(value => new ResultRowViewModel(value)),
    ];

    public int Count => Rows.Count;
}
