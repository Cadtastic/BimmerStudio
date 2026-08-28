using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using BimmerStudio.App.Help;
using BimmerStudio.App.ViewModels;

namespace BimmerStudio.App.Views;

public partial class HelpWindow : Window
{
    private ContentControl? _topicContent;

    public HelpWindow()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => Subscribe();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        _topicContent = this.FindControl<ContentControl>("TopicContent");
    }

    private void Subscribe()
    {
        if (DataContext is not HelpViewerViewModel viewModel)
        {
            return;
        }

        RenderCurrent(viewModel);

        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(HelpViewerViewModel.CurrentMarkdown)
                or nameof(HelpViewerViewModel.Current))
            {
                RenderCurrent(viewModel);
            }
        };
    }

    private void RenderCurrent(HelpViewerViewModel viewModel)
    {
        if (_topicContent is not null)
        {
            _topicContent.Content = MarkdownRenderer.Render(viewModel.CurrentMarkdown);
        }
    }
}
