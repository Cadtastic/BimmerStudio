using System.Collections.ObjectModel;
using BimmerStudio.Application.Abstractions;
using BimmerStudio.Application.Localization;
using BimmerStudio.Domain.Connections;
using BimmerStudio.Domain.Vehicles;
using BimmerStudio.Infrastructure.Ediabas.Transports;
using BimmerStudio.Infrastructure.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BimmerStudio.App.ViewModels;

/// <summary>
/// Chooses where the vehicle data lives, how the car is reached, and the display language.
/// </summary>
public sealed partial class SetupViewModel(
    IDiagnosticConnectionFactory connectionFactory,
    ILocalizer localizer,
    AppSettingsStore settingsStore) : ViewModelBase
{
    public override string HelpTopicId => "workspace";

    public ObservableCollection<string> SerialPorts { get; } = [];

    public IReadOnlyList<string> TransportOptions { get; } =
    [
        TransportIds.Simulation,
        TransportIds.KDCanSerial,
        TransportIds.Enet,
        TransportIds.Elm327,
    ];

    public IReadOnlyList<LanguageChoice> Languages => localizer.AvailableLanguages;

    [ObservableProperty]
    private LanguageChoice? _selectedLanguage;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    private string? _ecuDataPath;

    [ObservableProperty]
    private string? _simulationPath;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnectCommand))]
    [NotifyPropertyChangedFor(nameof(IsSerialTransport))]
    [NotifyPropertyChangedFor(nameof(IsEnetTransport))]
    [NotifyPropertyChangedFor(nameof(IsSimulationTransport))]
    private string _selectedTransport = TransportIds.Simulation;

    [ObservableProperty]
    private string? _serialPort;

    [ObservableProperty]
    private string _enetHost = EnetInterfaceFactory.AutoDetect;

    [ObservableProperty]
    private int _sgbdCount;

    [ObservableProperty]
    private int _groupCount;

    [ObservableProperty]
    private bool _isConnected;

    public bool IsSerialTransport =>
        SelectedTransport is TransportIds.KDCanSerial or TransportIds.Elm327;

    public bool IsEnetTransport => SelectedTransport == TransportIds.Enet;

    public bool IsSimulationTransport => SelectedTransport == TransportIds.Simulation;

    /// <summary>Raised once a connection is open, carrying it and whether writes are permitted.</summary>
    public event Action<IDiagnosticConnection, bool, IReadOnlyList<string>>? Connected;

    public SetupViewModel WithDefaults()
    {
        RefreshSerialPorts();
        SelectedLanguage = Languages.FirstOrDefault(language =>
            language.Id.Equals(localizer.CurrentLanguageId, StringComparison.OrdinalIgnoreCase))
            ?? Languages.FirstOrDefault();

        return this;
    }

    partial void OnSelectedLanguageChanged(LanguageChoice? value)
    {
        if (value is null || value.Id.Equals(localizer.CurrentLanguageId, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _ = ApplyLanguageAsync(value.Id);
    }

    private async Task ApplyLanguageAsync(string languageId)
    {
        await localizer.SetLanguageAsync(languageId);

        // Persisted so the next start opens in the chosen language, before any UI exists.
        var settings = await settingsStore.LoadAsync();
        await settingsStore.SaveAsync(settings with { LanguageId = languageId });

        // Re-issue the current status in the new language where it was a canned message.
        CountDescriptionFiles(EcuDataPath);
    }

    [RelayCommand]
    private void RefreshSerialPorts()
    {
        SerialPorts.Clear();

        try
        {
            // Qualified: SerialPort is also the name of this view model's selected-port property.
            var ports = System.IO.Ports.SerialPort.GetPortNames();

            foreach (var port in ports.OrderBy(name => name, StringComparer.Ordinal))
            {
                SerialPorts.Add(port);
            }
        }
        catch (Exception ex)
        {
            // Enumeration needs platform support that may be absent (missing libudev, say).
            StatusMessage = localizer.Format("Status_SerialError", ex.Message);
        }

        SerialPort ??= SerialPorts.FirstOrDefault();
    }

    partial void OnEcuDataPathChanged(string? value) => CountDescriptionFiles(value);

    private void CountDescriptionFiles(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            SgbdCount = 0;
            GroupCount = 0;
            StatusMessage = string.IsNullOrWhiteSpace(path)
                ? null
                : localizer["Status_FolderMissing"];
            return;
        }

        SgbdCount = Directory.GetFiles(path, "*.prg", SearchOption.TopDirectoryOnly).Length;
        GroupCount = Directory.GetFiles(path, "*.grp", SearchOption.TopDirectoryOnly).Length;

        StatusMessage = SgbdCount == 0
            ? localizer["Status_NoFiles"]
            : localizer.Format("Status_FilesFound", SgbdCount, GroupCount);
    }

    private bool CanConnect() =>
        !string.IsNullOrWhiteSpace(EcuDataPath) && Directory.Exists(EcuDataPath);

    [RelayCommand(CanExecute = nameof(CanConnect))]
    private async Task ConnectAsync(CancellationToken cancellationToken)
    {
        IsBusy = true;

        try
        {
            var workspace = new Workspace(
                Guid.NewGuid(),
                "Workspace",
                VehiclePlatform.ESeries,
                EcuDataPath!,
                // Simulation files usually sit beside the ECU data; fall back to it so a
                // simulation connection works without a second path being configured.
                string.IsNullOrWhiteSpace(SimulationPath) ? EcuDataPath : SimulationPath);

            var profile = ConnectionProfile.Create(
                SelectedTransport,
                SelectedTransport,
                BuildSettings());

            var connection = await connectionFactory.ConnectAsync(profile, workspace, cancellationToken);

            var names = Directory
                .EnumerateFiles(EcuDataPath!, "*.prg")
                .Concat(Directory.EnumerateFiles(EcuDataPath!, "*.grp"))
                .Select(Path.GetFileName)
                .Where(name => name is not null)
                .Select(name => name!)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            IsConnected = true;
            StatusMessage = localizer.Format("Status_ConnectedVia", SelectedTransport);

            var settings = await settingsStore.LoadAsync(cancellationToken);
            await settingsStore.SaveAsync(settings with { LastEcuDataPath = EcuDataPath }, cancellationToken);

            Connected?.Invoke(connection, !profile.IsHardware, names);
        }
        catch (DiagnosticConnectionException ex)
        {
            StatusMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private Dictionary<string, string> BuildSettings()
    {
        var settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (IsSerialTransport && !string.IsNullOrWhiteSpace(SerialPort))
        {
            settings[SerialTransportSettings.Port] = SerialPort;
        }

        if (IsEnetTransport)
        {
            settings[EnetTransportSettings.RemoteHost] = EnetHost;
        }

        if (IsSimulationTransport)
        {
            settings[SimulationTransportSettings.SimulationPath] =
                string.IsNullOrWhiteSpace(SimulationPath) ? EcuDataPath! : SimulationPath;
        }

        return settings;
    }
}
