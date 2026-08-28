using BimmerStudio.Application.Localization;
using BimmerStudio.Domain.Diagnostics;

namespace BimmerStudio.App.ViewModels;

/// <summary>
/// One declared argument or result of the selected job, with its description run through the
/// data-phrase translation. The original German always survives as a tooltip.
/// </summary>
public sealed class JobParameterViewModel(JobParameterInfo info, ILocalizer localizer)
{
    public string Name => info.Name;

    public string Type => info.Type ?? "—";

    /// <summary>
    /// The description with each line translated separately. Lines are the unit that recurs
    /// across SGBDs ("oder alternativ" appears a thousand times as a line of longer comments),
    /// so per-line lookup is what makes the dictionary bite on multi-line text.
    /// </summary>
    public string? Comment =>
        string.IsNullOrWhiteSpace(info.Comment)
            ? null
            : string.Join(' ', Lines().Select(localizer.TranslateData));

    public bool HasComment => !string.IsNullOrWhiteSpace(info.Comment);

    /// <summary>Shown as the tooltip whenever any line of the display text is a translation.</summary>
    public string? OriginalComment =>
        HasComment && Lines().Any(localizer.HasDataTranslation)
            ? info.Comment!.Replace('\n', ' ')
            : null;

    private IEnumerable<string> Lines() =>
        (info.Comment ?? string.Empty).Split('\n', StringSplitOptions.RemoveEmptyEntries);

    /// <summary>
    /// A value to pre-fill the argument line with, chosen by declared type. Honest placeholders:
    /// a number type gets a zero, everything else a question mark the user must replace.
    /// </summary>
    public string Placeholder
    {
        get
        {
            var type = info.Type?.ToLowerInvariant() ?? string.Empty;

            if (type.Contains("int") || type.Contains("long") || type.Contains("char"))
            {
                return "0";
            }

            return type switch
            {
                "real" => "0.0",
                "binary" => "00",
                _ => "?",
            };
        }
    }
}
