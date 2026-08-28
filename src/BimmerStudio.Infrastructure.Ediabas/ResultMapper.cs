using BimmerStudio.Domain.Diagnostics;
using EdiabasLib;

namespace BimmerStudio.Infrastructure.Ediabas;

/// <summary>
/// Translates EdiabasNet result sets into domain types. The boundary at which interpreter types
/// stop: nothing from <c>EdiabasLib</c> travels past this class.
/// </summary>
internal static class ResultMapper
{
    /// <summary>
    /// Splits raw result sets into the system set and the data sets.
    /// </summary>
    /// <remarks>
    /// EDIABAS reserves set 0 for information about the call itself — <c>JOBSTATUS</c>,
    /// <c>VARIANTE</c>, <c>UBATT</c> — and uses sets 1..n for the payload, one per record.
    /// A job returning nothing still yields set 0, so an empty list means the job did not run.
    /// </remarks>
    public static JobResult ToJobResult(
        string jobName,
        List<Dictionary<string, EdiabasNet.ResultData>>? resultSets,
        TimeSpan duration)
    {
        if (resultSets is not { Count: > 0 })
        {
            return new JobResult(jobName, ResultSet.Empty, [], duration);
        }

        var systemResults = ToResultSet(resultSets[0]);

        var dataSets = new List<ResultSet>(resultSets.Count - 1);
        for (var i = 1; i < resultSets.Count; i++)
        {
            dataSets.Add(ToResultSet(resultSets[i]));
        }

        return new JobResult(jobName, systemResults, dataSets, duration);
    }

    public static ResultSet ToResultSet(Dictionary<string, EdiabasNet.ResultData>? raw)
    {
        if (raw is not { Count: > 0 })
        {
            return ResultSet.Empty;
        }

        var values = new Dictionary<string, ResultValue>(raw.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var (name, data) in raw)
        {
            values[name] = ToResultValue(name, data);
        }

        return new ResultSet(values);
    }

    private static ResultValue ToResultValue(string name, EdiabasNet.ResultData data) =>
        data.OpData switch
        {
            string text => ResultValue.Text(name, text),
            long integer => ResultValue.Integer(name, integer),
            double real => ResultValue.Real(name, real),
            byte[] binary => ResultValue.Binary(name, binary),

            // The interpreter normalises to the four types above; anything else is a new
            // EdiabasLib result type. Keep the value rather than dropping it.
            null => ResultValue.Text(name, string.Empty),
            var other => ResultValue.Text(name, other.ToString() ?? string.Empty),
        };
}
