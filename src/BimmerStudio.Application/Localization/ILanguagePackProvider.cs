namespace BimmerStudio.Application.Localization;

/// <summary>
/// Supplies the installed language packs: the ones shipped with the app plus any the user
/// dropped into the languages folder.
/// </summary>
public interface ILanguagePackProvider
{
    Task<IReadOnlyList<LanguagePack>> LoadAllAsync(CancellationToken cancellationToken = default);
}
