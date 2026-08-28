using System.Text.Json;

namespace BimmerStudio.Infrastructure.Settings;

/// <summary>
/// Persists <see cref="AppSettings"/> as JSON in the per-user application-data folder.
/// </summary>
public sealed class AppSettingsStore(string? overridePath = null)
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    private string SettingsPath => overridePath ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "BimmerStudio",
        "settings.json");

    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(SettingsPath))
            {
                return new AppSettings();
            }

            await using var stream = File.OpenRead(SettingsPath);
            return await JsonSerializer
                .DeserializeAsync<AppSettings>(stream, Options, cancellationToken)
                .ConfigureAwait(false) ?? new AppSettings();
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            // Damaged or unreadable settings mean defaults, not a startup failure.
            return new AppSettings();
        }
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);

            await using var stream = File.Create(SettingsPath);
            await JsonSerializer
                .SerializeAsync(stream, settings, Options, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Losing a preference write is preferable to surfacing an error for it.
        }
    }
}
