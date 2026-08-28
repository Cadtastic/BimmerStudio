using EdiabasLib;

namespace BimmerStudio.Infrastructure.Ediabas;

/// <summary>
/// Decides what an interpreter error means in the app's terms.
/// </summary>
internal static class EdiabasErrorClassifier
{
    /// <summary>
    /// True when the failure is "there is no vehicle here" rather than a genuine fault.
    /// </summary>
    /// <remarks>
    /// <c>SYS_0010</c> is raised when an SGBD's automatic <c>INITIALISIERUNG</c> job fails, which
    /// is what happens when such an SGBD is loaded with nothing to talk to. The <c>IFH</c> codes
    /// are interface-level failures with the same practical meaning.
    /// </remarks>
    public static bool IndicatesMissingVehicle(Exception exception) =>
        TryGetErrorCode(exception) is { } code && IndicatesMissingVehicle(code);

    public static bool IndicatesMissingVehicle(EdiabasNet.ErrorCodes code) =>
        code is EdiabasNet.ErrorCodes.EDIABAS_SYS_0010
            or EdiabasNet.ErrorCodes.EDIABAS_IFH_0003
            or EdiabasNet.ErrorCodes.EDIABAS_IFH_0006
            or EdiabasNet.ErrorCodes.EDIABAS_IFH_0009
            or EdiabasNet.ErrorCodes.EDIABAS_IFH_0010
            or EdiabasNet.ErrorCodes.EDIABAS_IFH_0011;

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
