using BimmerStudio.Application.Abstractions;
using BimmerStudio.Application.Help;
using BimmerStudio.Domain.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BimmerStudio.App.ViewModels;

/// <summary>
/// Hosts the setup and browser panes and owns the state the whole window shares.
/// </summary>
public sealed partial class MainWindowViewModel : ViewModelBase
{
    private readonly IHelpService _helpService;
    private IDiagnosticConnection? _connection;

    public MainWindowViewModel(
        SetupViewModel setup,
        SgbdBrowserViewModel browser,
        IHelpService helpService)
    {
        Setup = setup.WithDefaults();
        Browser = browser;
        _helpService = helpService;

        Setup.Connected += OnConnected;
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
    public string ReadOnlyBannerText => AllowWrites
        ? "Simulation — write-class jobs are permitted because there is no vehicle to affect."
        : "Read-only — jobs that could change the vehicle are blocked.";

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

    private SgbdIdentifier? ParseSelectedSgbd() =>
        string.IsNullOrWhiteSpace(Browser.SelectedSgbd)
            ? null
            : SgbdIdentifier.Parse(Browser.SelectedSgbd);

    public async ValueTask DisposeConnectionAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }
}
