using BimmerStudio.Application.Localization;
using BimmerStudio.Domain.Diagnostics;

namespace BimmerStudio.App.ViewModels;

/// <summary>
/// One name/value pair from a result set.
/// </summary>
/// <remarks>
/// The name is always shown verbatim: result names are protocol identifiers, and a user
/// comparing against a forum post or another tool needs to see the same string. What gets
/// localised is the explanation beside it.
/// </remarks>
public sealed class ResultRowViewModel(ResultValue value, ILocalizer localizer, bool isSystemResult)
{
    public string Name => value.Name;

    public string Value => value.ToDisplayString();

    public string Kind => value.Kind.ToString();

    /// <summary>
    /// What the result means, in the selected language. Only the EDIABAS system results have
    /// this: they are a fixed, finite set the interpreter produces for every job, so they can be
    /// documented once. Job-declared results vary per ECU and are described in the job's own
    /// "Results this job returns" panel instead.
    /// </summary>
    public string? Description
    {
        get
        {
            if (!isSystemResult)
            {
                return null;
            }

            var key = $"SysResult_{Name.ToUpperInvariant()}";
            var text = localizer[key];

            // The localizer returns the key when nothing defines it: an undocumented system
            // result should show no description rather than a raw key.
            return text == key ? null : text;
        }
    }

    public bool HasDescription => Description is not null;

    /// <summary>
    /// Binary results are shown as hex, so they get a monospaced face to keep byte pairs aligned.
    /// </summary>
    public bool IsMonospaced => value.Kind is ResultValueKind.Binary;
}
