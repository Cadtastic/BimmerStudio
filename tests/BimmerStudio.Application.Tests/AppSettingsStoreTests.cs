using BimmerStudio.Infrastructure.Settings;

namespace BimmerStudio.Application.Tests;

/// <summary>
/// Settings are a convenience, so the store must never be a source of startup failures: a
/// missing or damaged file yields defaults rather than an exception.
/// </summary>
public sealed class AppSettingsStoreTests : IDisposable
{
    private readonly string _path = Path.Combine(
        Path.GetTempPath(),
        $"bimmerstudio-settings-{Guid.NewGuid():N}.json");

    public void Dispose()
    {
        if (File.Exists(_path))
        {
            File.Delete(_path);
        }
    }

    [Fact]
    public async Task Round_trips_the_whole_workspace()
    {
        var store = new AppSettingsStore(_path);

        await store.SaveAsync(new AppSettings(
            LanguageId: "de",
            LastEcuDataPath: @"C:\EDIABAS\Ecu",
            LastSimulationPath: @"C:\EDIABAS\Sim",
            LastTransportId: "kdcan-serial",
            LastSerialPort: "COM4",
            LastEnetHost: "auto"));

        var loaded = await store.LoadAsync();

        loaded.LanguageId.ShouldBe("de");
        loaded.LastEcuDataPath.ShouldBe(@"C:\EDIABAS\Ecu");
        loaded.LastSimulationPath.ShouldBe(@"C:\EDIABAS\Sim");
        loaded.LastTransportId.ShouldBe("kdcan-serial");
        loaded.LastSerialPort.ShouldBe("COM4");
        loaded.LastEnetHost.ShouldBe("auto");
    }

    [Fact]
    public async Task A_missing_file_yields_defaults()
    {
        var loaded = await new AppSettingsStore(_path).LoadAsync();

        loaded.LanguageId.ShouldBeNull();
        loaded.LastEcuDataPath.ShouldBeNull();
    }

    [Fact]
    public async Task A_damaged_file_yields_defaults_rather_than_throwing()
    {
        await File.WriteAllTextAsync(_path, "{ this is not json");

        var loaded = await new AppSettingsStore(_path).LoadAsync();

        loaded.LastEcuDataPath.ShouldBeNull();
    }

    [Fact]
    public async Task Saving_creates_the_directory_when_absent()
    {
        var nested = Path.Combine(
            Path.GetTempPath(),
            $"bimmerstudio-{Guid.NewGuid():N}",
            "settings.json");

        try
        {
            await new AppSettingsStore(nested).SaveAsync(new AppSettings(LanguageId: "en"));

            File.Exists(nested).ShouldBeTrue();
        }
        finally
        {
            var directory = Path.GetDirectoryName(nested)!;
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
