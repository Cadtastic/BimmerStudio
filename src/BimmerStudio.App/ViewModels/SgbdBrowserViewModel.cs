using System.Collections.ObjectModel;
using System.Diagnostics;
using BimmerStudio.Application.Abstractions;
using BimmerStudio.Application.Localization;
using BimmerStudio.Application.Modules;
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
    private readonly IModuleCatalog _moduleCatalog;

    private IDiagnosticConnection? _connection;
    private IDiagnosticSession? _session;
    private CancellationTokenSource? _continuousCancellation;
    private CancellationTokenSource? _prefetchCancellation;
    private Task _loadTask = Task.CompletedTask;
    private bool _canReachVehicle;

    public SgbdBrowserViewModel(
        JobSafetyClassifier classifier,
        ILocalizer localizer,
        IModuleCatalog moduleCatalog)
    {
        _classifier = classifier;
        _localizer = localizer;
        _moduleCatalog = moduleCatalog;

        // Translated text lives in every row and in the computed labels; one language switch
        // refreshes them all in place.
        _localizer.LanguageChanged += (_, _) =>
        {
            foreach (var job in Jobs)
            {
                job.RefreshTranslations();
            }

            foreach (var sgbd in AvailableSgbds)
            {
                sgbd.RefreshTranslations();
            }

            OnPropertyChanged(nameof(BlockedReason));
            OnPropertyChanged(nameof(VariantLabel));
        };
    }

    public override string HelpTopicId => "sgbd-browser";

    public ObservableCollection<SgbdListItemViewModel> AvailableSgbds { get; } = [];

    public ObservableCollection<JobListItemViewModel> Jobs { get; } = [];

    /// <summary>What the list shows: <see cref="Jobs"/> narrowed by the filter text.</summary>
    public ObservableCollection<JobListItemViewModel> VisibleJobs { get; } = [];

    [ObservableProperty]
    private string? _jobFilter;

    /// <summary>
    /// Matches the protocol name and the translated summary, so "fault" finds FS_LESEN once the
    /// dictionary has translated its description.
    /// </summary>
    partial void OnJobFilterChanged(string? value) => ApplyJobFilter();

    private void ApplyJobFilter()
    {
        var filter = JobFilter?.Trim();

        VisibleJobs.Clear();
        foreach (var job in Jobs)
        {
            var matches = string.IsNullOrEmpty(filter)
                || job.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)
                || job.Summary?.Contains(filter, StringComparison.CurrentCultureIgnoreCase) == true;

            if (matches)
            {
                VisibleJobs.Add(job);
            }
        }
    }

    [ObservableProperty]
    private SgbdListItemViewModel? _selectedSgbd;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunOnceCommand))]
    [NotifyCanExecuteChangedFor(nameof(RunContinuousCommand))]
    [NotifyCanExecuteChangedFor(nameof(InsertArgumentTemplateCommand))]
    [NotifyCanExecuteChangedFor(nameof(ClearResultsCommand))]
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

    public bool CanRunSelectedJob =>
        SelectedJob is not null
        && !IsStreaming
        && (AllowWrites || SelectedJob.IsReadOnly);

    public string? VariantLabel =>
        LoadedVariant is null ? null : _localizer.Format("Browser_VariantFormat", LoadedVariant);

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

        // A simulation replays recorded traffic; there is no ECU to answer an identification
        // request, which is precisely what a group file needs.
        _canReachVehicle = connection.Profile.IsHardware;
        Reset();
    }

    /// <summary>
    /// Builds the picker: rows resolved against the module catalog, sectioned by vehicle area
    /// with non-selectable headers, sorted by friendly name within each section. Unrecognised
    /// names gather under "Other" and show their raw code — honest beats guessed.
    /// </summary>
    public void SetAvailableSgbds(IEnumerable<string> names)
    {
        AvailableSgbds.Clear();

        var items = names
            .Select(name => new SgbdListItemViewModel(
                name,
                _localizer,
                _canReachVehicle,
                _moduleCatalog.Resolve(name)))
            .ToList();

        var byCategory = items
            .GroupBy(item => item.CategoryKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);

        foreach (var category in _moduleCatalog.CategoryOrder)
        {
            if (!byCategory.TryGetValue(category, out var section))
            {
                continue;
            }

            AvailableSgbds.Add(SgbdListItemViewModel.Header(category, _localizer));

            // Groups first within each section: with a car connected they are the entry point
            // you usually want, because they identify the fitted variant for you.
            foreach (var item in section
                         .OrderByDescending(item => item.IsGroup)
                         .ThenBy(item => item.ModuleName ?? item.DisplayName, StringComparer.CurrentCultureIgnoreCase)
                         .ThenBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase))
            {
                AvailableSgbds.Add(item);
            }
        }
    }

    /// <summary>
    /// Loading is what selecting an ECU means, so it happens on selection rather than behind a
    /// second button. Reading an ECU description file cannot change the vehicle, so there is
    /// nothing here worth making the user confirm.
    /// </summary>
    partial void OnSelectedSgbdChanged(SgbdListItemViewModel? value)
    {
        // Disabled rows are unreachable by pointer, but keyboard navigation can still land on
        // one; refusing here keeps the guarantee in the view model rather than only in the view.
        if (value is { IsSelectable: true })
        {
            _loadTask = LoadSgbdAsync(value, CancellationToken.None);
        }
        else if (value is not null)
        {
            StatusMessage = value.Tooltip;
        }
    }

    /// <summary>
    /// Awaits the load started by the last selection change. Selection is a property set, so it
    /// cannot be awaited directly; startup automation and tests need somewhere to wait.
    /// </summary>
    public Task WaitForLoadAsync() => _loadTask;

    private async Task LoadSgbdAsync(SgbdListItemViewModel sgbd, CancellationToken cancellationToken)
    {
        // Headers carry no file. They are never selectable, so this is belt and braces.
        if (_connection is null || sgbd.Identifier is not { } identifier)
        {
            return;
        }

        IsBusy = true;
        Reset();

        try
        {
            _session = await _connection.OpenSessionAsync(identifier, cancellationToken);
            LoadedVariant = _session.ResolvedVariant;

            var jobs = await _session.GetJobsAsync(cancellationToken);
            foreach (var job in jobs)
            {
                Jobs.Add(new JobListItemViewModel(job, _classifier.Classify(job.Name), _localizer));
            }

            ApplyJobFilter();
            StatusMessage = _localizer.Format("Status_JobsIn", Jobs.Count, LoadedVariant);

            StartDescriptionPrefetch();
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
    /// Fills in every job's documentation in the background after the list appears.
    /// </summary>
    /// <remarks>
    /// Documentation costs three interpreter calls per job, but they are answered from the file
    /// rather than the vehicle: describing a 150-job ECU takes roughly 400 ms in total. Doing it
    /// eagerly means the whole list carries descriptions instead of only the jobs the user has
    /// happened to click, which is what makes the list scannable.
    /// </remarks>
    private void StartDescriptionPrefetch()
    {
        _prefetchCancellation?.Cancel();
        _prefetchCancellation?.Dispose();
        _prefetchCancellation = new CancellationTokenSource();

        var session = _session;
        var token = _prefetchCancellation.Token;
        var jobs = Jobs.ToList();

        _ = Task.Run(async () =>
        {
            foreach (var job in jobs)
            {
                if (token.IsCancellationRequested || session is null)
                {
                    return;
                }

                try
                {
                    var described = await session.DescribeJobAsync(job.Name, token);
                    job.UpdateDescriptor(described);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (DiagnosticConnectionException)
                {
                    // Documentation is optional; a job that cannot describe keeps its name only.
                }
            }
        }, token);
    }

    [RelayCommand(CanExecute = nameof(CanInsertArgumentTemplate))]
    private void InsertArgumentTemplate()
    {
        if (SelectedJob is null)
        {
            return;
        }

        // EDIABAS arguments are positional, so what matters is having the right number of slots.
        Arguments = string.Join(';', SelectedJob.Arguments.Select(argument => argument.Placeholder));
    }

    private bool CanInsertArgumentTemplate() => SelectedJob?.HasArguments == true;

    [RelayCommand(CanExecute = nameof(CanClearResults))]
    private void ClearResults() => SelectedJob?.ClearResults();

    private bool CanClearResults() => SelectedJob is not null;

    [RelayCommand(CanExecute = nameof(CanRunSelectedJob))]
    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        if (_session is null || SelectedJob is null)
        {
            return;
        }

        var job = SelectedJob;
        IsBusy = true;

        try
        {
            var request = new JobRequest(job.Name, NullIfBlank(Arguments));
            var timestamp = Stopwatch.GetTimestamp();
            var result = await _session.ExecuteJobAsync(request, cancellationToken);

            ShowResult(job, result);
            job.LastDuration = $"{Stopwatch.GetElapsedTime(timestamp).TotalMilliseconds:F0} ms";
            job.ExecutionCount++;
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

        var job = SelectedJob;
        _continuousCancellation = new CancellationTokenSource();
        IsStreaming = true;

        try
        {
            var request = new JobRequest(job.Name, NullIfBlank(Arguments));

            await foreach (var result in _session.ExecuteJobContinuousAsync(
                               request,
                               TimeSpan.FromMilliseconds(500),
                               _continuousCancellation.Token))
            {
                ShowResult(job, result);
                job.ExecutionCount++;
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
            StatusMessage = _localizer.Format("Status_StoppedAfter", job.ExecutionCount);
        }
    }

    [RelayCommand(CanExecute = nameof(IsStreaming))]
    private void StopContinuous() => _continuousCancellation?.Cancel();

    /// <summary>
    /// Attaches results to the job that produced them, not to the browser, so they travel with
    /// the selection.
    /// </summary>
    private void ShowResult(JobListItemViewModel job, JobResult result)
    {
        var sets = new List<ResultSetViewModel>
        {
            ResultSetViewModel.System(result.SystemResults, _localizer),
        };

        for (var i = 0; i < result.DataSets.Count; i++)
        {
            sets.Add(ResultSetViewModel.Data(i + 1, result.DataSets[i], _localizer));
        }

        job.ShowResults(sets);

        StatusMessage = result.IsSuccess
            ? _localizer.Format("Status_DataSets", result.JobName, result.DataSets.Count)
            : _localizer.Format("Status_JobStatus", result.JobName, result.JobStatus);
    }

    private void Reset()
    {
        _prefetchCancellation?.Cancel();
        Jobs.Clear();
        VisibleJobs.Clear();
        SelectedJob = null;
        LoadedVariant = null;
        Arguments = null;
    }

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
