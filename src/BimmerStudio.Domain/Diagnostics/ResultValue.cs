using System.Globalization;

namespace BimmerStudio.Domain.Diagnostics;

/// <summary>
/// One named value from a job result set.
/// </summary>
public sealed record ResultValue(string Name, ResultValueKind Kind, object Value)
{
    public static ResultValue Text(string name, string value) => new(name, ResultValueKind.Text, value);

    public static ResultValue Integer(string name, long value) => new(name, ResultValueKind.Integer, value);

    public static ResultValue Real(string name, double value) => new(name, ResultValueKind.Real, value);

    public static ResultValue Binary(string name, byte[] value) => new(name, ResultValueKind.Binary, value);

    public string? AsText() => Value as string;

    public long? AsInteger() => Value as long?;

    public double? AsReal() => Value switch
    {
        double d => d,
        long l => l,
        _ => null,
    };

    public byte[]? AsBinary() => Value as byte[];

    /// <summary>
    /// Culture-invariant rendering, with binary values as uppercase hex byte pairs.
    /// Job results are protocol data, so they are never formatted for the current locale.
    /// </summary>
    public string ToDisplayString() => Value switch
    {
        null => string.Empty,
        string s => s,
        long l => l.ToString(CultureInfo.InvariantCulture),
        double d => d.ToString("G", CultureInfo.InvariantCulture),
        byte[] bytes => Convert.ToHexString(bytes),
        _ => Value.ToString() ?? string.Empty,
    };
}
