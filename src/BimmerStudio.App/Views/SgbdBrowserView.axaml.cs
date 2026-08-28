using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace BimmerStudio.App.Views;

public partial class SgbdBrowserView : UserControl
{
    public SgbdBrowserView() => InitializeComponent();

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
