using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using BimmerStudio.App.Help;
using BimmerStudio.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace BimmerStudio.App.Views;

public partial class MainWindow : Window
{
    private readonly IServiceProvider? _services;
    private HelpWindow? _helpWindow;

    public MainWindow()
    {
        InitializeComponent();
        AddHandler(KeyDownEvent, OnKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
    }

    public MainWindow(IServiceProvider services)
        : this() => _services = services;

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    /// <summary>
    /// F1 opens help for whatever has focus; Shift+F1 opens the browser at the table of contents.
    /// Handled as a tunnel so a focused text box cannot swallow it.
    /// </summary>
    private async void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.F1)
        {
            return;
        }

        e.Handled = true;

        var focusedTopicId = e.KeyModifiers.HasFlag(KeyModifiers.Shift)
            ? null
            : Help.Help.FindTopicId(FocusManager?.GetFocusedElement() as Visual);

        await ShowHelpAsync(focusedTopicId);
    }

    private async Task ShowHelpAsync(string? focusedTopicId)
    {
        if (_services is null || DataContext is not MainWindowViewModel viewModel)
        {
            return;
        }

        var helpViewModel = _services.GetRequiredService<HelpViewerViewModel>();
        await helpViewModel.InitialiseAsync();

        var topic = await viewModel.ResolveHelpAsync(focusedTopicId);
        if (topic is not null)
        {
            helpViewModel.Show(topic);
        }

        // A single non-modal window, so pressing F1 repeatedly navigates rather than stacking up.
        if (_helpWindow is null)
        {
            _helpWindow = new HelpWindow { DataContext = helpViewModel };
            _helpWindow.Closed += (_, _) => _helpWindow = null;
            _helpWindow.Show(this);
        }
        else
        {
            _helpWindow.Activate();
        }
    }
}
