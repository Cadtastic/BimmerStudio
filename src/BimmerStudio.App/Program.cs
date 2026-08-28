using System.Reflection;
using Avalonia;
using BimmerStudio.App.ViewModels;
using BimmerStudio.Application.Help;
using BimmerStudio.Domain.Safety;
using BimmerStudio.Infrastructure.Ediabas;
using BimmerStudio.Infrastructure.Help;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
        Services = BuildServices();

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    private static ServiceProvider BuildServices()
    {
        var services = new ServiceCollection();

        services.AddLogging(builder => builder.AddSimpleConsole().SetMinimumLevel(LogLevel.Information));

        // Diagnostics: interpreter plus the four built-in transports.
        services.AddEdiabasDiagnostics();

        services.AddSingleton<JobSafetyClassifier>();
        services.AddSingleton<JobHelpComposer>();

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
        services.AddTransient<HelpViewerViewModel>();

        return services.BuildServiceProvider();
    }
}
