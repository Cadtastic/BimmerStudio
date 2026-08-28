namespace BimmerStudio.Domain.Diagnostics;

/// <summary>
/// Distinguishes the two kinds of ECU description file EDIABAS can load.
/// </summary>
public enum SgbdKind
{
    /// <summary>
    /// A concrete ECU variant (<c>.prg</c>), for example <c>CAS</c> or <c>MSV70</c>.
    /// </summary>
    Variant,

    /// <summary>
    /// A group file (<c>.grp</c>, conventionally <c>d_*</c>), for example <c>d_motor</c>.
    /// Loading one makes EDIABAS interrogate the vehicle to resolve which variant is fitted,
    /// so it requires a live connection in a way a variant does not.
    /// </summary>
    Group,
}
