using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace BimmerStudio.App.Help;

/// <summary>
/// Attaches a help topic to any control, so F1 knows what the user was looking at.
/// </summary>
/// <remarks>
/// Set <c>help:Help.TopicId</c> in XAML on a view, a panel or an individual field. F1 walks up
/// from the focused element to the nearest control that declares one, so a topic on a container
/// covers everything inside it and only controls needing their own help have to set it.
/// </remarks>
public static class Help
{
    public static readonly AttachedProperty<string?> TopicIdProperty =
        AvaloniaProperty.RegisterAttached<Control, string?>("TopicId", typeof(Help));

    public static void SetTopicId(Control control, string? value) =>
        control.SetValue(TopicIdProperty, value);

    public static string? GetTopicId(Control control) =>
        control.GetValue(TopicIdProperty);

    /// <summary>
    /// The nearest declared topic at or above <paramref name="start"/>, or null if none.
    /// </summary>
    public static string? FindTopicId(Visual? start)
    {
        for (var current = start; current is not null; current = current.GetVisualParent())
        {
            if (current is Control control && GetTopicId(control) is { Length: > 0 } topicId)
            {
                return topicId;
            }
        }

        return null;
    }
}
