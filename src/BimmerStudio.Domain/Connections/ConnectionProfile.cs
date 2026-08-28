namespace BimmerStudio.Domain.Connections;

/// <summary>
/// A saved, named way of reaching a vehicle: which transport, and its settings.
/// </summary>
/// <param name="TransportId">
/// Selects the transport provider. See <see cref="TransportIds"/> for the built-in set.
/// </param>
/// <param name="Settings">
/// Transport-specific values whose keys are declared by that transport's profile schema, for
/// example a serial port name or a gateway address. Kept as strings so a new transport can add
/// settings without changing this type.
/// </param>
public sealed record ConnectionProfile(
    Guid Id,
    string Name,
    string TransportId,
    IReadOnlyDictionary<string, string> Settings)
{
    public static ConnectionProfile Create(
        string name,
        string transportId,
        IReadOnlyDictionary<string, string>? settings = null) =>
        new(Guid.NewGuid(), name, transportId,
            settings ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

    public string? Setting(string key) =>
        Settings.TryGetValue(key, out var value) ? value : null;

    public string Setting(string key, string fallback) => Setting(key) ?? fallback;

    /// <summary>
    /// True when the transport talks to a real vehicle. Simulation profiles are the only ones
    /// where write-class jobs may run during read-only milestones.
    /// </summary>
    public bool IsHardware =>
        !string.Equals(TransportId, TransportIds.Simulation, StringComparison.OrdinalIgnoreCase);
}
