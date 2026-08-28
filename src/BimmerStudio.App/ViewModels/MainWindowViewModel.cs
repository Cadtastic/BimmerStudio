using BimmerStudio.Application.Abstractions;
using BimmerStudio.Application.Help;
using BimmerStudio.Application.Localization;
using BimmerStudio.Domain.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BimmerStudio.App.ViewModels;

/// <summary>
/// Hosts the setup and browser panes and owns the state the whole window shares.
/// </summary>
public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly IHelpService _helpService;
    private readonly ILocalizer _localizer;
    private IDiagnosticConnection? _connection;

    public MainWindowViewModel(
        SetupViewModel setup,
        SgbdBrowserViewModel browser,
        IHelpService helpService,
        ILocalizer localizer)
    {
        Setup = setup.WithDefaults();
        Browser = browser;
        _helpService = helpService;
        _localizer = localizer;

        Setup.Connected += OnConnected;
        _localizer.LanguageChanged += (_, _) => OnPropertyChanged(nameof(ReadOnlyBannerText));
    }

    public override string HelpTopicId => "overview";

    public SetupViewModel Setup { get; }

    public SgbdBrowserViewModel Browser { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ReadOnlyBannerText))]
    private bool _allowWrites;

    [ObservableProperty]
    private bool _isConnected;

    /// <summary>
    /// Always visible while connected to real hardware. The point is that the guarantee should
    /// never be something the user has to remember or go looking for.
    /// </summary>
    public string ReadOnlyBannerText =>
        _localizer[AllowWrites ? "Banner_Simulation" : "Banner_ReadOnly"];

    private void OnConnected(
        IDiagnosticConnection connection,
        bool allowWrites,
        IReadOnlyList<string> sgbdNames)
    {
        _connection = connection;
        AllowWrites = allowWrites;
        IsConnected = true;

        Browser.AttachConnection(connection, allowWrites);
        Browser.SetAvailableSgbds(sgbdNames);
        StatusMessage = $"{sgbdNames.Count} ECU description files available.";
    }

    /// <summary>
    /// Resolves what F1 should open, given where focus was and what is selected.
    /// </summary>
    public Task<HelpTopic?> ResolveHelpAsync(string? focusedTopicId, CancellationToken cancellationToken = default)
    {
        // Fully qualified: this type has a HelpTopicId string property of its own, which would
        // otherwise shadow the value-object type.
        var context = new HelpContext(
            Application.Help.HelpTopicId.TryParse(focusedTopicId),
            Application.Help.HelpTopicId.Parse(Browser.HelpTopicId),
            Browser.SelectedJob?.Descriptor,
            ParseSelectedSgbd());

        return _helpService.ResolveAsync(context, cancellationToken);
    }

    private SgbdIdentifier? ParseSelectedSgbd() => Browser.SelectedSgbd?.Identifier;

    /// <summary>
    /// Replays the startup automation: the same steps a user would click, in order, so demos
    /// and smoke tests exercise the real code paths rather than a shortcut.
    /// </summary>
    public async Task ApplyStartupOptionsAsync(StartupOptions options)
    {
        if (options.EcuDataPath is not null)
        {
            Setup.EcuDataPath = options.EcuDataPath;
        }

        if (!options.AutoConnect)
        {
            return;
        }

        await Setup.ConnectCommand.ExecuteAsync(null);

        if (options.LoadSgbd is { } sgbd && IsConnected)
        {
            // Selecting is what loads: the browser opens the session on selection change.
            Browser.SelectedSgbd = Browser.AvailableSgbds.FirstOrDefault(item =>
                item.DisplayName.Equals(sgbd, StringComparison.OrdinalIgnoreCase));

            await Browser.WaitForLoadAsync();
        }

        if (options.SelectJob is { } jobName)
        {
            Browser.SelectedJob = Browser.Jobs.FirstOrDefault(job =>
                job.Name.Equals(jobName, StringComparison.OrdinalIgnoreCase));
        }

        if (options.Run && Browser.RunOnceCommand.CanExecute(null))
        {
            await Browser.RunOnceCommand.ExecuteAsync(null);
        }

        if (options.ThenSelectJob is { } secondJob)
        {
            Browser.SelectedJob = Browser.Jobs.FirstOrDefault(job =>
                job.Name.Equals(secondJob, StringComparison.OrdinalIgnoreCase));
        }
    }

    public async ValueTask DisposeConnectionAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }
}
