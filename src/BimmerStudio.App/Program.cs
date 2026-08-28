using System.Reflection;
using Avalonia;
using BimmerStudio.App.Localization;
using BimmerStudio.App.ViewModels;
using BimmerStudio.Application.Help;
using BimmerStudio.Application.Localization;
using BimmerStudio.Application.Modules;
using BimmerStudio.Domain.Safety;
using BimmerStudio.Infrastructure.Ediabas;
using BimmerStudio.Infrastructure.Help;
using BimmerStudio.Infrastructure.Modules;
using BimmerStudio.Infrastructure.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using InfraLocalization = BimmerStudio.Infrastructure.Localization;

namespace BimmerStudio.App;

internal static class Program
{
    /// <summary>
    /// Composition root. Avalonia constructs the Application itself, so the container is built
    /// first and handed over rather than the other way round.
    /// </summary>
    public static IServiceProvider? Services { get; private set; }

    [STAThread]
    public static void Main(string[] args)
    {
        var options = StartupOptions.Parse(args);
        Services = BuildServices(options);

        // The language must be live before any window binds: views bind to the localizer's
        // indexer at construction, and the saved preference decides what they see first.
        var localizer = (InfraLocalization.Localizer)Services.GetRequiredService<ILocalizer>();
        var settings = Services.GetRequiredService<AppSettingsStore>().LoadAsync().GetAwaiter().GetResult();
        localizer.InitialiseAsync(options.Language ?? settings.LanguageId).GetAwaiter().GetResult();
        LocalizerHost.Attach(localizer);

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    private static ServiceProvider BuildServices(StartupOptions options)
    {
        var services = new ServiceCollection();

        services.AddSingleton(options);
        services.AddLogging(builder => builder.AddSimpleConsole().SetMinimumLevel(LogLevel.Information));

        // Diagnostics: interpreter plus the four built-in transports.
        services.AddEdiabasDiagnostics();

        services.AddSingleton<JobSafetyClassifier>();
        services.AddSingleton<JobHelpComposer>();
        services.AddSingleton<AppSettingsStore>();
        services.AddSingleton<IModuleCatalog, ModuleCatalog>();

        services.AddSingleton<ILanguagePackProvider>(_ => new InfraLocalization.JsonLanguagePackProvider(
            Assembly.GetExecutingAssembly(),
            "BimmerStudio.App.Assets.Languages",
            // The drop-in folder: a new language is one JSON file here, no rebuild.
            Path.Combine(AppContext.BaseDirectory, "languages")));

        services.AddSingleton<ILocalizer, InfraLocalization.Localizer>();

        services.AddSingleton<IHelpContentStore>(_ => new EmbeddedHelpContentStore(
            Assembly.GetExecutingAssembly(),
            "BimmerStudio.App.Help.Topics",
            // Optional local pack: lets a user add the legacy BMW reference material without
            // it ever being redistributed with the application.
            Path.Combine(AppContext.BaseDirectory, "help", "legacy-reference")));

        services.AddSingleton<IHelpService, HelpService>();

        services.AddSingleton<SetupViewModel>();
        services.AddSingleton<SgbdBrowserViewModel>();
        services.AddSingleton<MainWindowViewModel>();

        // One help view model, not one per F1 press: the window is a single instance, so a
        // transient would navigate an object the open window is no longer bound to.
        services.AddSingleton<HelpViewerViewModel>();

        return services.BuildServiceProvider();
    }
}
