namespace BimmerStudio.Domain.Diagnostics;

/// <summary>
/// One argument or result declared by a job, as reported by the SGBD's own metadata jobs.
/// </summary>
public sealed record JobParameterInfo(string Name, string? Type = null, string? Comment = null);
