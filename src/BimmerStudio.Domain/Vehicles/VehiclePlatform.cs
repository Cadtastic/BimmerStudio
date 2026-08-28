namespace BimmerStudio.Domain.Vehicles;

/// <summary>
/// Vehicle generations, which differ in both their data format and how they are reached.
/// </summary>
public enum VehiclePlatform
{
    /// <summary>
    /// E-series. Coding data is SP-Daten (NCS <c>DATEN</c> tables); the car is reached over
    /// K-line or D-CAN.
    /// </summary>
    ESeries,

    /// <summary>
    /// F-series. Coding data is PSdZData and the car is reached over ENET/DoIP.
    /// Reserved: the transport works, the data layer is not implemented.
    /// </summary>
    FSeries,

    /// <summary>G-series. Reserved, as for <see cref="FSeries"/>.</summary>
    GSeries,
}
