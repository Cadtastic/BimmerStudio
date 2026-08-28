namespace BimmerStudio.Infrastructure.Ediabas;

/// <summary>
/// EDIABAS's reserved metadata jobs, as registered by the interpreter. They are answered from the
/// SGBD file itself and never reach the vehicle, so they work with no car connected.
/// </summary>
/// <remarks>
/// The names are not what the <c>_JOB*</c> prefix of the result keys suggests: arguments and
/// results come from <c>_ARGUMENTS</c> and <c>_RESULTS</c>. Asking for <c>_JOBARGS</c> fails with
/// <c>EDIABAS_SYS_0008</c>.
/// </remarks>
internal static class ReservedJobs
{
    /// <summary>Lists the jobs an SGBD declares. One result set per job, key <c>JOBNAME</c>.</summary>
    public const string Jobs = "_JOBS";

    /// <summary>
    /// Documentation for the job named by the argument. One result set with keys
    /// <c>JOBCOMMENT0</c>, <c>JOBCOMMENT1</c>, and so on.
    /// </summary>
    public const string JobComments = "_JOBCOMMENTS";

    /// <summary>
    /// Arguments of the job named by the argument. One result set per argument, keys
    /// <c>ARG</c>, <c>ARGTYPE</c> and <c>ARGCOMMENT0</c>...
    /// </summary>
    public const string Arguments = "_ARGUMENTS";

    /// <summary>
    /// Results of the job named by the argument. One result set per result, keys
    /// <c>RESULT</c>, <c>RESULTTYPE</c> and <c>RESULTCOMMENT0</c>...
    /// </summary>
    public const string Results = "_RESULTS";

    /// <summary>Version and authorship of the SGBD itself.</summary>
    public const string VersionInfo = "_VERSIONINFO";

    /// <summary>Lists the lookup tables an SGBD carries. Used by the UDS argument wizard.</summary>
    public const string Tables = "_TABLES";

    /// <summary>Contents of one lookup table, named by the argument.</summary>
    public const string Table = "_TABLE";

    public const string JobNameResult = "JOBNAME";
    public const string ArgumentResult = "ARG";
    public const string ArgumentTypeResult = "ARGTYPE";
    public const string ResultResult = "RESULT";
    public const string ResultTypeResult = "RESULTTYPE";

    public const string JobCommentPrefix = "JOBCOMMENT";
    public const string ArgumentCommentPrefix = "ARGCOMMENT";
    public const string ResultCommentPrefix = "RESULTCOMMENT";
}
