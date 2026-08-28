using BimmerStudio.Domain.Diagnostics;

namespace BimmerStudio.Application.Help;

/// <summary>
/// What the user was looking at when they pressed F1.
/// </summary>
/// <param name="ElementTopicId">
/// Topic of the focused control, if it declares one. Tried first, with its ancestors.
/// </param>
/// <param name="ViewTopicId">Topic of the containing view. The fallback.</param>
/// <param name="SelectedJob">
/// The job selected in a job list, if any. Its help is composed from the SGBD's own
/// documentation rather than authored, since job names are per-ECU.
/// </param>
/// <param name="Sgbd">The loaded SGBD, used to attribute composed job help.</param>
public sealed record HelpContext(
    HelpTopicId? ElementTopicId = null,
    HelpTopicId? ViewTopicId = null,
    JobDescriptor? SelectedJob = null,
    SgbdIdentifier? Sgbd = null)
{
    public static HelpContext ForView(string viewTopicId) =>
        new(ViewTopicId: HelpTopicId.Parse(viewTopicId));
}
