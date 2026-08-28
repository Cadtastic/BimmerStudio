namespace BimmerStudio.Application.Modules;

/// <summary>
/// What a raw SGBD file name resolved to.
/// </summary>
/// <param name="ModuleKey">
/// Key of the recognised module, for example <c>dme</c> or <c>kombi</c>. The localised display
/// name is the language pack's <c>Module_&lt;key&gt;</c> entry. Null when the name was not
/// recognised — unknown names are shown raw rather than guessed at.
/// </param>
/// <param name="CategoryKey">
/// Grouping key, localised via <c>Category_&lt;key&gt;</c>. Always present; unrecognised names
/// fall into <c>other</c>.
/// </param>
public sealed record ModuleResolution(string? ModuleKey, string CategoryKey)
{
    public const string OtherCategory = "other";

    public static ModuleResolution Unknown { get; } = new(null, OtherCategory);
}
