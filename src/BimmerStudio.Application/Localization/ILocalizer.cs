using System.ComponentModel;

namespace BimmerStudio.Application.Localization;

/// <summary>
/// Live translation for the whole application. Switching language takes effect immediately.
/// </summary>
/// <remarks>
/// Implements <see cref="INotifyPropertyChanged"/> so views can bind straight to the indexer;
/// a language switch raises a change for it and every bound string updates in place.
/// </remarks>
public interface ILocalizer : INotifyPropertyChanged
{
    string CurrentLanguageId { get; }

    IReadOnlyList<LanguageChoice> AvailableLanguages { get; }

    /// <summary>A UI string by key. Returns the key itself when no pack defines it.</summary>
    string this[string key] { get; }

    /// <summary>A UI format string by key, formatted invariantly.</summary>
    string Format(string key, params object?[] args);

    /// <summary>
    /// Translates text that came from vehicle data (job comments, argument comments) via the
    /// pack's phrase dictionary. Returns the original when no translation exists — data text is
    /// never hidden, only augmented.
    /// </summary>
    string TranslateData(string? source);

    /// <summary>True when <see cref="TranslateData"/> would change the text.</summary>
    bool HasDataTranslation(string? source);

    Task SetLanguageAsync(string languageId, CancellationToken cancellationToken = default);

    event EventHandler? LanguageChanged;
}
