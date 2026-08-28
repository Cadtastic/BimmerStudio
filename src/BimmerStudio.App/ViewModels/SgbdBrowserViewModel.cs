using System.Collections.ObjectModel;
using System.Diagnostics;
using BimmerStudio.Application.Abstractions;
using BimmerStudio.Application.Localization;
using BimmerStudio.Domain.Diagnostics;
using BimmerStudio.Domain.Safety;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BimmerStudio.App.ViewModels;

/// <summary>
/// The Tool32 equivalent: pick an ECU description file, browse its jobs, run one, read results.
/// </summary>
public sealed partial class SgbdBrowserViewModel : ViewModelBase
{
    private readonly JobSafetyClassifier _classifier;
    private readonly ILocalizer _localizer;

    private IDiagnosticConnection? _connection;
    private IDiagnosticSession? _session;
    private CancellationTokenSource? _continuousCancellation;

    public SgbdBrowserViewModel(JobSafetyClassifier classifier, ILocalizer localizer)
    {
        _classifier = classifier;
        _localizer = localizer;

        // Translated text lives in every row and in the computed labels; one language switch
        // refreshes them all in place.
        _localizer.LanguageChanged += (_, _) =>
        {
            foreach (var job in Jobs)
            {
                job.RefreshTranslations();
            }

            OnPropertyChanged(nameof(BlockedReason));
            OnPropertyChanged(nameof(VariantLabel));
            OnPropertyChanged(nameof(RunCountLabel));
        };
    }

    public override string HelpTopicId => "sgbd-browser";

    public ObservableCollection<string> AvailableSgbds { get; } = [];

    public ObservableCollection<JobListItemViewModel> Jobs { get; } = [];

    public ObservableCollection<ResultSetViewModel> Results { get; } = [];

    [ObservableProperty]
    private string? _sgbdFilter;

    [ObservableProperty]
    private string? _jobFilter;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LoadSgbdCommand))]
    private string? _selectedSgbd;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunOnceCommand))]
    [NotifyCanExecuteChangedFor(nameof(RunContinuousCommand))]
    [NotifyCanExecuteChangedFor(nameof(InsertArgumentTemplateCommand))]
    [NotifyPropertyChangedFor(nameof(CanRunSelectedJob))]
    [NotifyPropertyChangedFor(nameof(BlockedReason))]
    private JobListItemViewModel? _selectedJob;

    [ObservableProperty]
    private string? _arguments;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VariantLabel))]
    private string? _loadedVariant;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanRunSelectedJob))]
    [NotifyCanExecuteChangedFor(nameof(RunOnceCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopContinuousCommand))]
    private bool _isStreaming;

    /// <summary>
    /// False for hardware connections, which is what enforces read-only mode in the UI.
    /// A simulation has no car to damage, so writes are permitted there.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanRunSelectedJob))]
    private bool _allowWrites;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RunCountLabel))]
    private int _executionCount;

    [ObservableProperty]
    private string? _lastDuration;

    public bool CanRunSelectedJob =>
        SelectedJob is not null
        && !IsStreaming
        && (AllowWrites || SelectedJob.IsReadOnly);

    public string? VariantLabel =>
        LoadedVariant is null ? null : _localizer.Format("Browser_VariantFormat", LoadedVariant);

    public string RunCountLabel => _localizer.Format("Browser_RunCountFormat", ExecutionCount);

    /// <summary>Why Run is disabled, phrased for the user rather than as an error.</summary>
    public string? BlockedReason
    {
        get
        {
            if (SelectedJob is null || SelectedJob.IsReadOnly || AllowWrites)
            {
                return null;
            }

            return _localizer.Format(
                "Blocked_ReadOnly",
                SelectedJob.Name,
                SelectedJob.SafetyLabel,
                SelectedJob.SafetyDescription);
        }
    }

    public void AttachConnection(IDiagnosticConnection connection, bool allowWrites)
    {
        _connection = connection;
        AllowWrites = allowWrites;
        Reset();
    }

    public void SetAvailableSgbds(IEnumerable<string> names)
    {
        AvailableSgbds.Clear();
        foreach (var name in names)
        {
            AvailableSgbds.Add(name);
        }
    }

    private bool CanLoadSgbd() => !string.IsNullOrWhiteSpace(SelectedSgbd) && _connection is not null;

    [RelayCommand(CanExecute = nameof(CanLoadSgbd))]
    private async Task LoadSgbdAsync(CancellationToken cancellationToken)
    {
        if (_connection is null || string.IsNullOrWhiteSpace(SelectedSgbd))
        {
            return;
        }

        IsBusy = true;
        Reset();

        try
        {
            var identifier = SgbdIdentifier.Parse(SelectedSgbd);
            _session = await _connection.OpenSessionAsync(identifier, cancellationToken);
            LoadedVariant = _session.ResolvedVariant;

            var jobs = await _session.GetJobsAsync(cancellationToken);
            foreach (var job in jobs)
            {
                Jobs.Add(new JobListItemViewModel(job, _classifier.Classify(job.Name), _localizer));
            }

            StatusMessage = _localizer.Format("Status_JobsIn", Jobs.Count, LoadedVariant);
        }
        catch (VehicleConnectionRequiredException ex)
        {
            // Expected for group files and for ECUs that initialise on load. Not an error.
            StatusMessage = ex.Message;
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

    /// <summary>
    /// Fetches the selected job's documentation on demand. Doing this for every job up front
    /// would cost three interpreter calls each, which is wasted on a list being scanned.
    /// </summary>
    partial void OnSelectedJobChanged(JobListItemViewModel? value)
    {
        if (value is null || _session is null || value.Descriptor.Results.Count > 0)
        {
            return;
        }

        _ = DescribeSelectedJobAsync(value);
    }

    private async Task DescribeSelectedJobAsync(JobListItemViewModel item)
    {
        if (_session is null)
        {
            return;
        }

        try
        {
            var described = await _session.DescribeJobAsync(item.Name);
            item.UpdateDescriptor(described);
        }
        catch (DiagnosticConnectionException)
        {
            // Documentation is optional; leaving the name-only descriptor is correct.
        }
    }

    /// <summary>
    /// Pre-fills the argument line with one placeholder per declared argument, in order:
    /// numbers get zeros, everything else a question mark to replace. EDIABAS arguments are
    /// positional, so what matters is having the right number of slots.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanInsertArgumentTemplate))]
    private void InsertArgumentTemplate()
    {
        if (SelectedJob is null)
        {
            return;
        }

        Arguments = string.Join(';', SelectedJob.Arguments.Select(argument => argument.Placeholder));
    }

    private bool CanInsertArgumentTemplate() => SelectedJob?.HasArguments == true;

    [RelayCommand(CanExecute = nameof(CanRunSelectedJob))]
    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        if (_session is null || SelectedJob is null)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var request = new JobRequest(SelectedJob.Name, NullIfBlank(Arguments));
            var timestamp = Stopwatch.GetTimestamp();
            var result = await _session.ExecuteJobAsync(request, cancellationToken);

            ShowResult(result);
            LastDuration = $"{Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds:F0} ms";
            ExecutionCount++;
        }
        catch (OperationCanceledException)
        {
            StatusMessage = _localizer["Status_Cancelled"];
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

    [RelayCommand(CanExecute = nameof(CanRunSelectedJob))]
    private async Task RunContinuousAsync()
    {
        if (_session is null || SelectedJob is null)
        {
            return;
        }

        _continuousCancellation = new CancellationTokenSource();
        IsStreaming = true;

        try
        {
            var request = new JobRequest(SelectedJob.Name, NullIfBlank(Arguments));

            await foreach (var result in _session.ExecuteJobContinuousAsync(
                               request,
                               TimeSpan.FromMilliseconds(500),
                               _continuousCancellation.Token))
            {
                ShowResult(result);
                ExecutionCount++;
            }
        }
        catch (OperationCanceledException)
        {
            // Stopping is the normal way out of a continuous run.
        }
        catch (DiagnosticConnectionException ex)
        {
            StatusMessage = ex.Message;
        }
        finally
        {
            IsStreaming = false;
            _continuousCancellation?.Dispose();
            _continuousCancellation = null;
            StatusMessage = _localizer.Format("Status_StoppedAfter", ExecutionCount);
        }
    }

    [RelayCommand(CanExecute = nameof(IsStreaming))]
    private void StopContinuous() => _continuousCancellation?.Cancel();

    private void ShowResult(JobResult result)
    {
        Results.Clear();
        Results.Add(new ResultSetViewModel("System", result.SystemResults));

        for (var i = 0; i < result.DataSets.Count; i++)
        {
            Results.Add(new ResultSetViewModel($"Set {i + 1}", result.DataSets[i]));
        }

        StatusMessage = result.IsSuccess
            ? _localizer.Format("Status_DataSets", result.JobName, result.DataSets.Count)
            : _localizer.Format("Status_JobStatus", result.JobName, result.JobStatus);
    }

    private void Reset()
    {
        Jobs.Clear();
        Results.Clear();
        SelectedJob = null;
        LoadedVariant = null;
        ExecutionCount = 0;
        LastDuration = null;
    }

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
