using EdiabasLib;

namespace BimmerStudio.Infrastructure.Ediabas;

/// <summary>
/// Decides what an interpreter error means in the app's terms.
/// </summary>
internal static class EdiabasErrorClassifier
{
    /// <summary>Prefix of every interface-handler error code.</summary>
    private const string InterfaceErrorPrefix = "EDIABAS_IFH_";

    /// <summary>
    /// True when the failure means "there is nothing to talk to" rather than a genuine fault.
    /// </summary>
    /// <remarks>
    /// Two families qualify. <c>SYS_0010</c> is raised when an SGBD's automatic
    /// <c>INITIALISIERUNG</c> job fails, which is what happens when such an SGBD is loaded with
    /// no vehicle present. The whole <c>IFH</c> family is interface-handler errors — timeouts,
    /// no response, transport faults — which mean the same thing in that situation.
    /// <para>
    /// Matched as a family rather than as a list of individual codes: there are around seventy
    /// IFH codes, they vary by transport, and enumerating them invites exactly the gap where an
    /// ordinary "no car attached" surfaces to the user as an unexplained failure.
    /// </para>
    /// </remarks>
    public static bool IndicatesMissingVehicle(Exception exception) =>
        TryGetErrorCode(exception) is { } code && IndicatesMissingVehicle(code);

    public static bool IndicatesMissingVehicle(EdiabasNet.ErrorCodes code) =>
        code == EdiabasNet.ErrorCodes.EDIABAS_SYS_0010
        || code.ToString().StartsWith(InterfaceErrorPrefix, StringComparison.Ordinal);

    /// <summary>
    /// Finds the interpreter error code on an exception or anywhere in its inner chain, since
    /// the worker thread rethrows across an await boundary.
    /// </summary>
    public static EdiabasNet.ErrorCodes? TryGetErrorCode(Exception? exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is EdiabasNet.EdiabasNetException ediabasException)
            {
                return ediabasException.ErrorCode;
            }
        }

        return null;
    }
}
