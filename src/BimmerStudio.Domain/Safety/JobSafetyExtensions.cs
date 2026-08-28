namespace BimmerStudio.Domain.Safety;

public static class JobSafetyExtensions
{
    /// <summary>
    /// True when running the job cannot alter the vehicle. Only these run while the app is in
    /// read-only mode.
    /// </summary>
    public static bool IsReadOnly(this JobSafety safety) =>
        safety is JobSafety.Read or JobSafety.CommInit;

    /// <summary>
    /// Short explanation of the category, for badges and for the reason shown when a job is
    /// blocked.
    /// </summary>
    public static string Describe(this JobSafety safety) => safety switch
    {
        JobSafety.Read => "Reads data from the ECU. Safe.",
        JobSafety.CommInit => "Opens communication with the ECU. Changes nothing.",
        JobSafety.MemoryClear => "Erases stored data such as fault memory.",
        JobSafety.Actuator => "Drives a physical output on the vehicle.",
        JobSafety.Coding => "Writes coding data to the ECU.",
        JobSafety.Flash => "Reprograms ECU firmware. Can render the ECU unusable.",
        JobSafety.Unknown => "Cannot be classified, so it is treated as a write.",
        _ => string.Empty,
    };
}
