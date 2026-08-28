using System.Collections.ObjectModel;
using BimmerStudio.Application.Help;
using BimmerStudio.Application.Localization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BimmerStudio.App.ViewModels;

/// <summary>
/// The help window: table of contents, search, and the current topic.
/// </summary>
public sealed partial class HelpViewerViewModel : ViewModelBase
{
    private readonly IHelpService _helpService;
    private readonly ILocalizer _localizer;
    private readonly Stack<HelpTopic> _back = new();

    public HelpViewerViewModel(IHelpService helpService, ILocalizer localizer)
    {
        _helpService = helpService;
        _localizer = localizer;

        // Help follows the language like everything else: the topic set is rebuilt and the open
        // page re-resolved, so a switch does not leave the window showing the previous language.
        localizer.LanguageChanged += (_, _) => _ = ReloadForLanguageChangeAsync();
    }

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
        await RefreshTopicsAsync(SearchQuery, cancellationToken);
        Current ??= Topics.FirstOrDefault(topic => topic.Id.Value == "overview")
            ?? Topics.FirstOrDefault();
    }

    /// <summary>
    /// Rebuilds the topic list in the new language and re-resolves the open page by id, so the
    /// reader stays where they were instead of being sent back to the overview.
    /// </summary>
    private async Task ReloadForLanguageChangeAsync()
    {
        var openTopicId = Current?.Id;

        await RefreshTopicsAsync(SearchQuery);

        // A composed job topic is not in the authored set; recompose it by leaving it in place.
        if (openTopicId is not null
            && await _helpService.GetTopicAsync(openTopicId) is { } translated)
        {
            Current = translated;
        }

        OnPropertyChanged(nameof(CurrentTitle));
        OnPropertyChanged(nameof(CurrentMarkdown));
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
            ? await _helpService.GetTableOfContentsAsync(cancellationToken)
            : await _helpService.SearchAsync(query, cancellationToken);

        Topics.Clear();
        foreach (var topic in results)
        {
            Topics.Add(topic);
        }

        StatusMessage = _localizer.Format("Help_TopicCount", Topics.Count);
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
