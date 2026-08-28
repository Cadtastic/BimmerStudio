using CommunityToolkit.Mvvm.ComponentModel;

namespace BimmerStudio.App.ViewModels;

public abstract partial class ViewModelBase : ObservableObject
{
    /// <summary>
    /// Help topic for this view, used when the focused control declares none of its own.
    /// </summary>
    public abstract string HelpTopicId { get; }

    [ObservableProperty]
    private string? _statusMessage;

    [ObservableProperty]
    private bool _isBusy;
}
