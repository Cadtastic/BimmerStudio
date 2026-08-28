using Avalonia.Data;
using Avalonia.Markup.Xaml;

namespace BimmerStudio.App.Localization;

/// <summary>
/// Localised text in XAML: <c>Text="{loc:T Setup_Connect}"</c>.
/// </summary>
/// <remarks>
/// Produces a binding to the localizer's indexer rather than a one-shot lookup, which is what
/// makes a language switch update every visible string in place with no window reload.
/// </remarks>
public sealed class TExtension(string key) : MarkupExtension
{
    public string Key { get; } = key;

    public override object ProvideValue(IServiceProvider serviceProvider) =>
        new Binding($"[{Key}]")
        {
            Source = LocalizerHost.Current,
            Mode = BindingMode.OneWay,
        };
}
