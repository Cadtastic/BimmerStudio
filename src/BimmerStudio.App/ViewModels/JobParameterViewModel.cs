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

    public string? Comment =>
        string.IsNullOrWhiteSpace(info.Comment) ? null : localizer.TranslateData(info.Comment);

    public bool HasComment => !string.IsNullOrWhiteSpace(info.Comment);

    /// <summary>Shown as the tooltip whenever the display text is a translation.</summary>
    public string? OriginalComment =>
        HasComment && localizer.HasDataTranslation(info.Comment) ? info.Comment : null;

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
