using BimmerStudio.Domain.Diagnostics;
using BimmerStudio.Domain.Safety;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BimmerStudio.App.ViewModels;

/// <summary>
/// One row in the job list, carrying the safety verdict the UI needs to decide whether Run is
/// offered at all.
/// </summary>
public sealed partial class JobListItemViewModel(JobDescriptor descriptor, JobSafety safety)
    : ObservableObject
{
    /// <summary>Full documentation, fetched lazily when the job is selected.</summary>
    [ObservableProperty]
    private JobDescriptor _descriptor = descriptor;

    public string Name => Descriptor.Name;

    public JobSafety Safety { get; } = safety;

    public bool IsReadOnly => Safety.IsReadOnly();

    public string SafetyLabel => Safety.ToString();

    public string SafetyDescription => Safety.Describe();

    /// <summary>Short German comment from the SGBD, when it documents one.</summary>
    public string? Summary => Descriptor.FirstComment;

    public bool HasSummary => !string.IsNullOrWhiteSpace(Summary);

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
        OnPropertyChanged(nameof(Summary));
        OnPropertyChanged(nameof(HasSummary));
    }
}
