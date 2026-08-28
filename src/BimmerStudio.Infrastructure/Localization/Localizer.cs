using System.ComponentModel;
using System.Globalization;
using BimmerStudio.Application.Localization;

namespace BimmerStudio.Infrastructure.Localization;

/// <summary>
/// The live localizer. Views bind to the indexer; a language switch raises one indexer change
/// and every bound string in the application updates in place.
/// </summary>
/// <remarks>
/// English is the fallback chain's end: a key missing from the active pack falls back to the
/// English pack, then to the key itself — a visible key being a bug report that writes itself.
/// Data-phrase lookups fall back to the original German, never to English prose, because vehicle
/// data must stay recognisable against what other tools and forums show.
/// </remarks>
public sealed class Localizer(ILanguagePackProvider packProvider) : ILocalizer
{
    public const string FallbackLanguageId = "en";

    private readonly Lock _gate = new();
    private IReadOnlyList<LanguagePack> _packs = [];
    private LanguagePack _active = LanguagePack.Empty(FallbackLanguageId, "English");
    private LanguagePack? _fallback;

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler? LanguageChanged;

    public string CurrentLanguageId => _active.Id;

    public IReadOnlyList<LanguageChoice> AvailableLanguages =>
        [.. _packs.Select(pack => new LanguageChoice(pack.Id, pack.DisplayName))];

    public string this[string key]
    {
        get
        {
            if (_active.Ui.TryGetValue(key, out var text))
            {
                return text;
            }

            if (_fallback is not null && _fallback.Ui.TryGetValue(key, out var fallbackText))
            {
                return fallbackText;
            }

            return key;
        }
    }

    public string Format(string key, params object?[] args) =>
        string.Format(CultureInfo.InvariantCulture, this[key], args);

    public string TranslateData(string? source)
    {
        var normalised = TextNormaliser.NormaliseWhitespace(source);
        if (normalised.Length == 0)
        {
            return string.Empty;
        }

        return _active.DataPhrases.TryGetValue(normalised, out var translated)
            ? translated
            : normalised;
    }

    public bool HasDataTranslation(string? source)
    {
        var normalised = TextNormaliser.NormaliseWhitespace(source);
        return normalised.Length > 0 && _active.DataPhrases.ContainsKey(normalised);
    }

    /// <summary>Loads the installed packs. Called once at startup, before any view binds.</summary>
    public async Task InitialiseAsync(
        string? preferredLanguageId,
        CancellationToken cancellationToken = default)
    {
        var packs = await packProvider.LoadAllAsync(cancellationToken).ConfigureAwait(false);

        lock (_gate)
        {
            _packs = packs;
            _fallback = packs.FirstOrDefault(pack =>
                pack.Id.Equals(FallbackLanguageId, StringComparison.OrdinalIgnoreCase));

            _active = Find(preferredLanguageId)
                ?? Find(CultureInfo.CurrentUICulture.TwoLetterISOLanguageName)
                ?? _fallback
                ?? packs.FirstOrDefault()
                ?? _active;
        }

        RaiseChanged();
    }

    public Task SetLanguageAsync(string languageId, CancellationToken cancellationToken = default)
    {
        var pack = Find(languageId);
        if (pack is null || ReferenceEquals(pack, _active))
        {
            return Task.CompletedTask;
        }

        lock (_gate)
        {
            _active = pack;
        }

        RaiseChanged();
        return Task.CompletedTask;
    }

    private LanguagePack? Find(string? id) =>
        string.IsNullOrWhiteSpace(id)
            ? null
            : _packs.FirstOrDefault(pack => pack.Id.Equals(id.Trim(), StringComparison.OrdinalIgnoreCase));

    private void RaiseChanged()
    {
        // "Item[]" is the indexer's property name: one event refreshes every bound string.
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentLanguageId)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AvailableLanguages)));
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }
}
