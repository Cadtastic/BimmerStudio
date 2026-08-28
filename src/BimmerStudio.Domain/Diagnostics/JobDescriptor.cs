namespace BimmerStudio.Domain.Diagnostics;

/// <summary>
/// A job as declared by an SGBD, together with the documentation the SGBD carries for it.
/// </summary>
/// <param name="Comments">
/// Free-text lines from the SGBD's <c>_JOBCOMMENTS</c>. Usually German, and the only
/// human-readable description of a job that exists.
/// </param>
public sealed record JobDescriptor(
    string Name,
    IReadOnlyList<string> Comments,
    IReadOnlyList<JobParameterInfo> Arguments,
    IReadOnlyList<JobParameterInfo> Results)
{
    public static JobDescriptor NameOnly(string name) => new(name, [], [], []);

    /// <summary>
    /// Names beginning with an underscore are EDIABAS's reserved metadata jobs
    /// (<c>_JOBS</c>, <c>_JOBCOMMENTS</c>, <c>_VERSIONINFO</c> and friends). They describe the
    /// SGBD itself, never touch the vehicle, and are normally hidden from job lists.
    /// </summary>
    public bool IsReserved => Name.StartsWith('_');

    public string? FirstComment => Comments.Count > 0 ? Comments[0] : null;
}
