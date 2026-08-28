namespace BimmerStudio.Infrastructure.Settings;

/// <summary>
/// User preferences persisted between runs.
/// </summary>
public sealed record AppSettings(string? LanguageId = null, string? LastEcuDataPath = null);
