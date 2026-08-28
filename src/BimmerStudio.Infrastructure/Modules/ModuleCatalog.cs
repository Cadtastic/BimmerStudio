using System.Reflection;
using System.Text.Json;
using BimmerStudio.Application.Modules;

namespace BimmerStudio.Infrastructure.Modules;

/// <summary>
/// Resolves raw SGBD names against the embedded module map.
/// </summary>
/// <remarks>
/// Rules run in file order, first match wins, matched case-insensitively against the base name.
/// The map deliberately covers only names whose meaning is certain; everything else resolves to
/// the "other" category and is displayed raw. A wrong friendly name would be worse than none.
/// </remarks>
public sealed class ModuleCatalog : IModuleCatalog
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly IReadOnlyList<MapRule> _rules;
    private readonly IReadOnlyDictionary<string, string> _moduleCategories;

    public ModuleCatalog()
    {
        using var stream = typeof(ModuleCatalog).Assembly.GetManifestResourceStream(
            "BimmerStudio.Infrastructure.Modules.module-map.json")
            ?? throw new InvalidOperationException("module-map.json is not embedded.");

        var map = JsonSerializer.Deserialize<MapFile>(stream, Options)
            ?? throw new InvalidOperationException("module-map.json is empty.");

        CategoryOrder = map.CategoryOrder ?? [ModuleResolution.OtherCategory];
        _rules = map.Rules ?? [];
        _moduleCategories = new Dictionary<string, string>(
            map.Modules ?? [], StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<string> CategoryOrder { get; }

    public ModuleResolution Resolve(string sgbdBaseName)
    {
        if (string.IsNullOrWhiteSpace(sgbdBaseName))
        {
            return ModuleResolution.Unknown;
        }

        var name = Path.GetFileNameWithoutExtension(sgbdBaseName.Trim());

        foreach (var rule in _rules)
        {
            var matches = rule.Match switch
            {
                "exact" => name.Equals(rule.Token, StringComparison.OrdinalIgnoreCase),
                "prefix" => name.StartsWith(rule.Token, StringComparison.OrdinalIgnoreCase),
                "contains" => name.Contains(rule.Token, StringComparison.OrdinalIgnoreCase),
                _ => false,
            };

            if (matches && _moduleCategories.TryGetValue(rule.Module, out var category))
            {
                return new ModuleResolution(rule.Module.ToLowerInvariant(), category);
            }
        }

        return ModuleResolution.Unknown;
    }

    private sealed record MapFile(
        List<string>? CategoryOrder,
        Dictionary<string, string>? Modules,
        List<MapRule>? Rules);

    private sealed record MapRule(string Match, string Token, string Module);
}
