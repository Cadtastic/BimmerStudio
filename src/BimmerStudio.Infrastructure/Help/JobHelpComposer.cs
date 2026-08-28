using System.Text;
using BimmerStudio.Application.Help;
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
/// </remarks>
public sealed class JobHelpComposer(JobSafetyClassifier classifier)
{
    /// <summary>
    /// Notes for the handful of job names that mean the same thing across every SGBD. Everything
    /// else is described from the SGBD's own comments.
    /// </summary>
    private static readonly Dictionary<string, string> StandardJobNotes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["IDENT"] = "Reads ECU identification: part number, hardware and software versions, and coding index.",
            ["IDENTIFIKATION"] = "Reads ECU identification. On a group file this is what resolves which variant is fitted.",
            ["FS_LESEN"] = "Reads the fault memory. Each stored fault code comes back as its own result set.",
            ["FS_LOESCHEN"] = "Clears the fault memory. This destroys diagnostic evidence, so read the faults first.",
            ["IS_LESEN"] = "Reads the info (shadow) memory on older ECUs.",
            ["STATUS_LESEN"] = "UDS: reads measurement values by data identifier (DID).",
            ["STEUERN"] = "Actuates a function on the ECU. Moves real hardware.",
            ["STEUERN_IO"] = "UDS: drives an output directly, for actuator testing.",
            ["STEUERN_ROUTINE"] = "UDS: starts an ECU-internal routine such as an adaptation or reset.",
            ["INITIALISIERUNG"] = "Opens communication with the ECU. Many SGBDs run this automatically.",
            ["AIF_LESEN"] = "Reads the workshop info field, which records previous service writes.",
            ["C_FG_LESEN"] = "Reads the chassis number (VIN) from a coding-capable ECU.",
            ["SG_CODIEREN"] = "The standard coding job. Writes coding data to the ECU.",
        };

    public HelpTopic Compose(JobDescriptor job, SgbdIdentifier? sgbd)
    {
        ArgumentNullException.ThrowIfNull(job);

        var safety = classifier.Classify(job.Name);
        var markdown = new StringBuilder();

        markdown.Append("# ").Append(job.Name).AppendLine().AppendLine();

        if (sgbd is not null)
        {
            markdown.Append("Job of the `").Append(sgbd.BaseName).AppendLine("` ECU description file.").AppendLine();
        }

        if (StandardJobNotes.TryGetValue(job.Name, out var note))
        {
            markdown.AppendLine(note).AppendLine();
        }

        AppendSafety(markdown, safety);

        if (job.Comments.Count > 0)
        {
            markdown.AppendLine("## What the ECU description file says").AppendLine();
            foreach (var comment in job.Comments)
            {
                markdown.Append("> ").AppendLine(comment);
            }

            markdown.AppendLine();
        }
        else
        {
            markdown
                .AppendLine("## What the ECU description file says")
                .AppendLine()
                .AppendLine("This SGBD carries no description for the job. That is common: the")
                .AppendLine("documentation blocks are optional and many files omit them.")
                .AppendLine();
        }

        AppendParameters(markdown, "Arguments", job.Arguments,
            "This job takes no arguments.");
        AppendParameters(markdown, "Results", job.Results,
            "The description file does not list the results this job returns.");

        markdown
            .AppendLine("---")
            .AppendLine()
            .AppendLine("Job names are German protocol identifiers defined by the ECU description")
            .AppendLine("file, not by BimmerStudio, so they are always shown exactly as the SGBD")
            .AppendLine("declares them.");

        return new HelpTopic(
            HelpTopicId.Parse($"job/{job.Name}"),
            job.Name,
            markdown.ToString(),
            [job.Name, safety.ToString()]);
    }

    private static void AppendSafety(StringBuilder markdown, JobSafety safety)
    {
        markdown.Append("**Classification: ").Append(safety).Append("** — ")
            .AppendLine(safety.Describe()).AppendLine();

        if (!safety.IsReadOnly())
        {
            markdown
                .AppendLine("BimmerStudio is currently read-only, so this job cannot be run against")
                .AppendLine("a vehicle. The Run button stays disabled for anything that could change")
                .AppendLine("the car. It can still be run against a simulation.")
                .AppendLine();
        }

        if (safety == JobSafety.Unknown)
        {
            markdown
                .AppendLine("The name did not match any known pattern. Unrecognised jobs are treated")
                .AppendLine("as writes rather than assumed safe, so this may be a perfectly ordinary")
                .AppendLine("read that simply uses unusual naming — check the description above.")
                .AppendLine();
        }
    }

    private static void AppendParameters(
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

        markdown.AppendLine("| Name | Type | Description |")
            .AppendLine("|---|---|---|");

        foreach (var parameter in parameters)
        {
            // Comments carry line breaks; a newline inside a Markdown table cell breaks the row.
            var comment = string.IsNullOrWhiteSpace(parameter.Comment)
                ? "—"
                : parameter.Comment.Replace('\n', ' ');

            markdown
                .Append("| `").Append(parameter.Name).Append("` | ")
                .Append(parameter.Type ?? "—").Append(" | ")
                .Append(comment)
                .AppendLine(" |");
        }

        markdown.AppendLine();
    }
}
