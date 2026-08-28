using System.Text.RegularExpressions;
using BimmerStudio.Application.Localization;
using BimmerStudio.Domain.Diagnostics;

namespace BimmerStudio.App.ViewModels;

/// <summary>
/// One declared argument or result of the selected job, with its description run through the
/// data-phrase translation. The original German always survives as a tooltip.
/// </summary>
public sealed partial class JobParameterViewModel(JobParameterInfo info, ILocalizer localizer)
{
    /// <summary>
    /// A run of two or more spaces, or any tab. SGBDs use these to lay out labelled fields
    /// ("Beschreibung:&#9;&#9;…&#9;&#9;Datenlänge:&#9;&#9;…") inside a single comment line.
    /// </summary>
    [GeneratedRegex(@"[ ]{2,}|\t+")]
    private static partial Regex ColumnGap();

    public string Name => info.Name;

    public string Type => info.Type ?? "—";

    /// <summary>
    /// The description as readable lines.
    /// </summary>
    /// <remarks>
    /// Two different things in the source produce a line break. The SGBD's own comment lines are
    /// separate statements — the binary-buffer layout arrives as one line per byte offset — and
    /// within a single line, tabs and space runs separate labelled fields. Both were previously
    /// flattened into one paragraph, which turned a byte-layout table into an unreadable
    /// run-on sentence.
    /// </remarks>
    public string? Comment
    {
        get
        {
            if (string.IsNullOrWhiteSpace(info.Comment))
            {
                return null;
            }

            var lines = SourceLines()
                .Select(localizer.TranslateData)
                .SelectMany(SplitColumns)
                .Where(line => line.Length > 0);

            return string.Join('\n', lines);
        }
    }

    public bool HasComment => !string.IsNullOrWhiteSpace(info.Comment);

    /// <summary>Shown as the tooltip whenever any line of the display text is a translation.</summary>
    public string? OriginalComment =>
        HasComment && SourceLines().Any(localizer.HasDataTranslation)
            ? string.Join('\n', SourceLines())
            : null;

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

    private IEnumerable<string> SourceLines() =>
        (info.Comment ?? string.Empty).Split('\n', StringSplitOptions.RemoveEmptyEntries);

    private static IEnumerable<string> SplitColumns(string line) =>
        ColumnGap().Split(line).Select(part => part.Trim());
}
