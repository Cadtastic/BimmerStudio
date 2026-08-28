using System.Text;
using System.Text.RegularExpressions;
using BimmerStudio.Application.Help;
using BimmerStudio.Application.Localization;
using BimmerStudio.Domain.Diagnostics;
using BimmerStudio.Domain.Safety;

namespace BimmerStudio.Infrastructure.Help;

/// <summary>
/// Builds help for a job at request time from the SGBD's own documentation.
/// </summary>
/// <remarks>
/// Job help cannot be authored in advance: there are thousands of job names, they differ per ECU,
/// and the only description that exists is the German comment the SGBD carries. So the topic is
/// composed from three sources — what the SGBD says, what the safety classifier concluded, and a
/// short note for the well-known standard jobs.
/// <para>
/// Everything it emits goes through the localizer: its own prose by key, and the SGBD's text
/// through the same phrase dictionary the main window uses, so a description reads identically
/// in both places.
/// </para>
/// </remarks>
public sealed partial class JobHelpComposer(JobSafetyClassifier classifier, ILocalizer localizer)
{
    /// <summary>A run of two or more spaces, or any tab: SGBDs use these as column gaps.</summary>
    [GeneratedRegex(@"[ ]{2,}|\t+")]
    private static partial Regex ColumnGap();

    public HelpTopic Compose(JobDescriptor job, SgbdIdentifier? sgbd)
    {
        ArgumentNullException.ThrowIfNull(job);

        var safety = classifier.Classify(job.Name);
        var markdown = new StringBuilder();

        markdown.Append("# ").Append(job.Name).AppendLine().AppendLine();

        if (sgbd is not null)
        {
            markdown.AppendLine(localizer.Format("JobHelp_OfSgbd", sgbd.BaseName)).AppendLine();
        }

        // Notes for the handful of job names that mean the same thing across every SGBD.
        // Absent for everything else, which is described from the file's own comments.
        var noteKey = $"JobNote_{job.Name.ToUpperInvariant()}";
        var note = localizer[noteKey];
        if (note != noteKey)
        {
            markdown.AppendLine(note).AppendLine();
        }

        AppendSafety(markdown, safety);

        markdown.AppendLine("## " + localizer["JobHelp_WhatFileSays"]).AppendLine();

        if (job.Comments.Count > 0)
        {
            foreach (var line in job.Comments.SelectMany(Readable))
            {
                markdown.Append("> ").AppendLine(line);
            }
        }
        else
        {
            markdown.AppendLine(localizer["JobHelp_NoDescription"]);
        }

        markdown.AppendLine();

        AppendParameters(markdown, localizer["JobHelp_Arguments"], job.Arguments,
            localizer["JobHelp_NoArguments"]);
        AppendParameters(markdown, localizer["JobHelp_Results"], job.Results,
            localizer["JobHelp_NoResults"]);

        markdown
            .AppendLine("---")
            .AppendLine()
            .AppendLine(localizer["JobHelp_NamesNote"]);

        return new HelpTopic(
            HelpTopicId.Parse($"job/{job.Name}"),
            job.Name,
            markdown.ToString(),
            [job.Name, safety.ToString()]);
    }

    private void AppendSafety(StringBuilder markdown, JobSafety safety)
    {
        markdown
            .AppendLine(localizer.Format(
                "JobHelp_Classification",
                localizer[$"Safety_{safety}"],
                localizer[$"Safety_{safety}_Desc"]))
            .AppendLine();

        if (!safety.IsReadOnly())
        {
            markdown.AppendLine(localizer["JobHelp_ReadOnlyNote"]).AppendLine();
        }

        if (safety == JobSafety.Unknown)
        {
            markdown.AppendLine(localizer["JobHelp_UnknownNote"]).AppendLine();
        }
    }

    private void AppendParameters(
        StringBuilder markdown,
        string heading,
        IReadOnlyList<JobParameterInfo> parameters,
        string emptyNote)
    {
        markdown.Append("## ").AppendLine(heading).AppendLine();

        if (parameters.Count == 0)
        {
            markdown.AppendLine(emptyNote).AppendLine();
            return;
        }

        markdown
            .Append("| ").Append(localizer["JobHelp_ColumnName"])
            .Append(" | ").Append(localizer["JobHelp_ColumnType"])
            .Append(" | ").Append(localizer["JobHelp_ColumnDescription"]).AppendLine(" |")
            .AppendLine("|---|---|---|");

        foreach (var parameter in parameters)
        {
            // A newline inside a Markdown table cell breaks the row, so the readable lines are
            // rejoined with a separator that survives the table.
            var comment = string.Join(" · ", Readable(parameter.Comment));

            markdown
                .Append("| `").Append(parameter.Name).Append("` | ")
                .Append(parameter.Type ?? "—").Append(" | ")
                .Append(comment.Length == 0 ? "—" : comment)
                .AppendLine(" |");
        }

        markdown.AppendLine();
    }

    /// <summary>
    /// Turns one SGBD comment into readable lines: split on the file's own line breaks and on
    /// the tab or space runs it uses as column gaps, each piece translated on its own. This is
    /// the same treatment the browser applies, so the two never disagree.
    /// </summary>
    private IEnumerable<string> Readable(string? comment) =>
        (comment ?? string.Empty)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(localizer.TranslateData)
            .SelectMany(line => ColumnGap().Split(line))
            .Select(part => part.Trim())
            .Where(part => part.Length > 0);
}
