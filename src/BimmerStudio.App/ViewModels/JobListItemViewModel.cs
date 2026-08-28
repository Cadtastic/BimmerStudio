using System.Collections.ObjectModel;
using BimmerStudio.Application.Localization;
using BimmerStudio.Domain.Diagnostics;
using BimmerStudio.Domain.Safety;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BimmerStudio.App.ViewModels;

/// <summary>
/// One row in the job list, carrying the safety verdict the UI needs to decide whether Run is
/// offered at all. The job name itself is a protocol identifier and is never translated; the
/// descriptive text around it is.
/// </summary>
/// <remarks>
/// Results live here rather than on the browser, so each job keeps its own. Selecting another
/// job shows that job's results instead of leaving the previous job's output on screen next to
/// a name it does not belong to, and coming back shows what the job last returned.
/// </remarks>
public sealed partial class JobListItemViewModel(
    JobDescriptor descriptor,
    JobSafety safety,
    ILocalizer localizer) : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Summary))]
    [NotifyPropertyChangedFor(nameof(HasSummary))]
    [NotifyPropertyChangedFor(nameof(OriginalSummary))]
    [NotifyPropertyChangedFor(nameof(Arguments))]
    [NotifyPropertyChangedFor(nameof(HasArguments))]
    [NotifyPropertyChangedFor(nameof(DocumentedResults))]
    [NotifyPropertyChangedFor(nameof(HasDocumentedResults))]
    private JobDescriptor _descriptor = descriptor;

    /// <summary>What this job last returned. Empty until it has been run.</summary>
    public ObservableCollection<ResultSetViewModel> Results { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RunCountLabel))]
    [NotifyPropertyChangedFor(nameof(HasRun))]
    private int _executionCount;

    [ObservableProperty]
    private string? _lastDuration;

    public string Name => Descriptor.Name;

    public JobSafety Safety { get; } = safety;

    public bool IsReadOnly => Safety.IsReadOnly();

    public string SafetyLabel => localizer[$"Safety_{Safety}"];

    public string SafetyDescription => localizer[$"Safety_{Safety}_Desc"];

    public bool HasRun => ExecutionCount > 0;

    public string RunCountLabel => localizer.Format("Browser_RunCountFormat", ExecutionCount);

    /// <summary>Short comment from the SGBD, run through the phrase translation.</summary>
    public string? Summary =>
        Descriptor.FirstComment is { } comment ? localizer.TranslateData(comment) : null;

    public bool HasSummary => !string.IsNullOrWhiteSpace(Descriptor.FirstComment);

    /// <summary>The untranslated German, shown as a tooltip when a translation applied.</summary>
    public string? OriginalSummary =>
        Descriptor.FirstComment is { } comment && localizer.HasDataTranslation(comment)
            ? comment
            : null;

    /// <summary>
    /// Every documented comment line for the detail pane; the list shows only the first.
    /// </summary>
    /// <remarks>
    /// Kept as separate lines rather than run together. The SGBD's comment lines are distinct
    /// statements — a summary, then the protocol services the job uses — and joining them into
    /// one paragraph reads as a run-on sentence.
    /// </remarks>
    public string? FullDescription =>
        Descriptor.Comments.Count == 0
            ? null
            : string.Join('\n', Descriptor.Comments.Select(localizer.TranslateData));

    public IReadOnlyList<JobParameterViewModel> Arguments =>
        [.. Descriptor.Arguments.Select(argument => new JobParameterViewModel(argument, localizer))];

    public bool HasArguments => Descriptor.Arguments.Count > 0;

    public IReadOnlyList<JobParameterViewModel> DocumentedResults =>
        [.. Descriptor.Results.Select(result => new JobParameterViewModel(result, localizer))];

    public bool HasDocumentedResults => Descriptor.Results.Count > 0;

    /// <summary>Red for anything that writes, so the list reads at a glance.</summary>
    public string SafetyBrush => Safety switch
    {
        JobSafety.Read or JobSafety.CommInit => "#3FB950",
        JobSafety.MemoryClear => "#D29922",
        JobSafety.Actuator => "#DB6D28",
        JobSafety.Coding => "#F85149",
        JobSafety.Flash => "#FF7B72",
        _ => "#8B949E",
    };

    public void UpdateDescriptor(JobDescriptor described)
    {
        Descriptor = described;
        OnPropertyChanged(nameof(FullDescription));
    }

    public void ShowResults(IEnumerable<ResultSetViewModel> sets)
    {
        Results.Clear();
        foreach (var set in sets)
        {
            Results.Add(set);
        }
    }

    public void ClearResults()
    {
        Results.Clear();
        ExecutionCount = 0;
        LastDuration = null;
    }

    /// <summary>Re-evaluates every translated property after a language switch.</summary>
    public void RefreshTranslations()
    {
        OnPropertyChanged(nameof(SafetyLabel));
        OnPropertyChanged(nameof(SafetyDescription));
        OnPropertyChanged(nameof(Summary));
        OnPropertyChanged(nameof(OriginalSummary));
        OnPropertyChanged(nameof(FullDescription));
        OnPropertyChanged(nameof(RunCountLabel));
        OnPropertyChanged(nameof(Arguments));
        OnPropertyChanged(nameof(DocumentedResults));
    }
}
