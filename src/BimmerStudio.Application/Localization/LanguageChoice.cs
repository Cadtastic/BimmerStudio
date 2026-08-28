namespace BimmerStudio.Application.Localization;

/// <summary>
/// One selectable language, named in itself so a user can recognise their own.
/// </summary>
public sealed record LanguageChoice(string Id, string DisplayName)
{
    public override string ToString() => DisplayName;
}
