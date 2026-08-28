using System.Collections.ObjectModel;
using BimmerStudio.Application.Help;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BimmerStudio.App.ViewModels;

/// <summary>
/// The help window: table of contents, search, and the current topic.
/// </summary>
public sealed partial class HelpViewerViewModel(IHelpService helpService) : ViewModelBase
{
    private readonly Stack<HelpTopic> _back = new();

    public override string HelpTopicId => "overview";

    public ObservableCollection<HelpTopic> Topics { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CurrentTitle))]
    [NotifyPropertyChangedFor(nameof(CurrentMarkdown))]
    [NotifyCanExecuteChangedFor(nameof(GoBackCommand))]
    private HelpTopic? _current;

    [ObservableProperty]
    private string? _searchQuery;

    public string CurrentTitle => Current?.Title ?? "Help";

    public string CurrentMarkdown =>
        Current?.Markdown ?? "Select a topic, or press F1 in the main window.";

    public bool CanGoBack => _back.Count > 0;

    public async Task InitialiseAsync(CancellationToken cancellationToken = default)
    {
        await RefreshTopicsAsync(null, cancellationToken);
        Current ??= Topics.FirstOrDefault(topic => topic.Id.Value == "overview")
            ?? Topics.FirstOrDefault();
    }

    /// <summary>Shows a topic, remembering the previous one so Back works.</summary>
    public void Show(HelpTopic topic)
    {
        ArgumentNullException.ThrowIfNull(topic);

        if (Current is not null && !Current.Id.Equals(topic.Id))
        {
            _back.Push(Current);
        }

        Current = topic;
        OnPropertyChanged(nameof(CanGoBack));
    }

    partial void OnSearchQueryChanged(string? value) => _ = RefreshTopicsAsync(value);

    private async Task RefreshTopicsAsync(string? query, CancellationToken cancellationToken = default)
    {
        var results = string.IsNullOrWhiteSpace(query)
            ? await helpService.GetTableOfContentsAsync(cancellationToken)
            : await helpService.SearchAsync(query, cancellationToken);

        Topics.Clear();
        foreach (var topic in results)
        {
            Topics.Add(topic);
        }

        StatusMessage = string.IsNullOrWhiteSpace(query)
            ? $"{Topics.Count} topics."
            : $"{Topics.Count} topics matching “{query}”.";
    }

    [RelayCommand(CanExecute = nameof(CanGoBack))]
    private void GoBack()
    {
        if (_back.Count == 0)
        {
            return;
        }

        Current = _back.Pop();
        OnPropertyChanged(nameof(CanGoBack));
    }
}
