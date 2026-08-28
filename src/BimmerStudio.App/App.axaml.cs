using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using BimmerStudio.App.ViewModels;
using BimmerStudio.App.Views;
using Microsoft.Extensions.DependencyInjection;

namespace BimmerStudio.App;

public partial class App : Avalonia.Application
{
    private IServiceProvider? _services;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        _services = Program.Services
            ?? throw new InvalidOperationException("Services were not configured before startup.");

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var viewModel = _services.GetRequiredService<MainWindowViewModel>();

            desktop.MainWindow = new MainWindow(_services) { DataContext = viewModel };

            // The interpreter holds a serial port or socket, so it must be released on the way out.
            desktop.ShutdownRequested += async (_, _) => await viewModel.DisposeConnectionAsync();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
