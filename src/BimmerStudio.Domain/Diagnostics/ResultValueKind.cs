namespace BimmerStudio.Domain.Diagnostics;

/// <summary>
/// The value types an SGBD job result can carry.
/// </summary>
public enum ResultValueKind
{
    Text,
    Integer,
    Real,
    Binary,
}
