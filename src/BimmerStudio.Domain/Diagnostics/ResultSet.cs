using System.Collections;

namespace BimmerStudio.Domain.Diagnostics;

/// <summary>
/// One result set returned by a job. A job returns a system set describing the call itself
/// (job status, resolved variant, battery voltage) followed by zero or more data sets — for
/// example one per stored fault code.
/// </summary>
public sealed class ResultSet(IReadOnlyDictionary<string, ResultValue> values)
    : IReadOnlyCollection<ResultValue>
{
    public static ResultSet Empty { get; } =
        new(new Dictionary<string, ResultValue>(StringComparer.OrdinalIgnoreCase));

    public int Count => values.Count;

    public IReadOnlyCollection<string> Names => (IReadOnlyCollection<string>)values.Keys;

    /// <summary>Looks a value up by name, case-insensitively. Returns null when absent.</summary>
    public ResultValue? this[string name] =>
        values.TryGetValue(name, out var value) ? value : null;

    public bool Contains(string name) => values.ContainsKey(name);

    public string? TextOrNull(string name) => this[name]?.AsText();

    public long? IntegerOrNull(string name) => this[name]?.AsInteger();

    public IEnumerator<ResultValue> GetEnumerator() => values.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
