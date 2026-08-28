namespace BimmerStudio.Domain.Safety;

/// <summary>
/// What a job does to the vehicle, ordered by how much damage getting it wrong can cause.
/// </summary>
public enum JobSafety
{
    /// <summary>Reads data. Cannot change the vehicle.</summary>
    Read,

    /// <summary>Opens communication with an ECU. Changes nothing persistent.</summary>
    CommInit,

    /// <summary>
    /// Erases stored data such as fault memory. Recoverable, but destroys diagnostic evidence
    /// that may be the reason the car is being looked at.
    /// </summary>
    MemoryClear,

    /// <summary>
    /// Drives an output: a pump, a fan, a lock, a lamp. Moves real hardware, so it is unsafe
    /// while anyone is near the car.
    /// </summary>
    Actuator,

    /// <summary>
    /// Writes coding data. Wrong values leave an ECU misconfigured, and recovery needs the
    /// original data, which is why coding must always be preceded by a backup.
    /// </summary>
    Coding,

    /// <summary>
    /// Reprograms ECU firmware. An interruption can leave the ECU unusable.
    /// The highest-risk category.
    /// </summary>
    Flash,

    /// <summary>
    /// Could not be classified. Treated as dangerous: an unrecognised job on an unknown SGBD is
    /// exactly the case where a wrong guess is most costly.
    /// </summary>
    Unknown,
}
