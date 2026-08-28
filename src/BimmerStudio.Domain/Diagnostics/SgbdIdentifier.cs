namespace BimmerStudio.Domain.Diagnostics;

/// <summary>
/// Names an ECU description file without its extension or directory, the form EDIABAS resolves.
/// </summary>
public sealed record SgbdIdentifier
{
    private SgbdIdentifier(string baseName, SgbdKind kind)
    {
        BaseName = baseName;
        Kind = kind;
    }

    /// <summary>File name with no extension and no path, for example <c>CAS</c> or <c>d_motor</c>.</summary>
    public string BaseName { get; }

    public SgbdKind Kind { get; }

    /// <summary>
    /// Creates an identifier, inferring <see cref="SgbdKind"/> from the extension when one is
    /// present and otherwise from the <c>d_</c> group-file naming convention.
    /// </summary>
    public static SgbdIdentifier Parse(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var trimmed = name.Trim();
        var extension = Path.GetExtension(trimmed);
        var baseName = Path.GetFileNameWithoutExtension(trimmed);

        if (string.IsNullOrEmpty(baseName))
        {
            throw new ArgumentException($"'{name}' does not contain an SGBD name.", nameof(name));
        }

        var kind = extension.Equals(".grp", StringComparison.OrdinalIgnoreCase)
            || (string.IsNullOrEmpty(extension) && baseName.StartsWith("d_", StringComparison.OrdinalIgnoreCase))
                ? SgbdKind.Group
                : SgbdKind.Variant;

        return new SgbdIdentifier(baseName, kind);
    }

    public static SgbdIdentifier Variant(string baseName) =>
        new(Path.GetFileNameWithoutExtension(baseName.Trim()), SgbdKind.Variant);

    public static SgbdIdentifier Group(string baseName) =>
        new(Path.GetFileNameWithoutExtension(baseName.Trim()), SgbdKind.Group);

    public bool Equals(SgbdIdentifier? other) =>
        other is not null
        && Kind == other.Kind
        && string.Equals(BaseName, other.BaseName, StringComparison.OrdinalIgnoreCase);

    public override int GetHashCode() =>
        HashCode.Combine(BaseName.ToUpperInvariant(), Kind);

    public override string ToString() => BaseName;
}
