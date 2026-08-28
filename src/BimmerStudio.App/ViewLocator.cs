using Avalonia.Controls;
using Avalonia.Controls.Templates;
using BimmerStudio.App.ViewModels;

namespace BimmerStudio.App;

/// <summary>
/// Maps a view model to its view by name: <c>SgbdBrowserViewModel</c> to <c>SgbdBrowserView</c>.
/// </summary>
public sealed class ViewLocator : IDataTemplate
{
    public Control Build(object? param)
    {
        if (param is null)
        {
            return new TextBlock { Text = "No view model." };
        }

        var viewName = param.GetType().FullName!
            .Replace("ViewModels", "Views", StringComparison.Ordinal)
            .Replace("ViewModel", "View", StringComparison.Ordinal);

        var viewType = Type.GetType(viewName);

        return viewType is null
            ? new TextBlock { Text = $"No view found for {viewName}." }
            : (Control)Activator.CreateInstance(viewType)!;
    }

    public bool Match(object? data) => data is ViewModelBase;
}
