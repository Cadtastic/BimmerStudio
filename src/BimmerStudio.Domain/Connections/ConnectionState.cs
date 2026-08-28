namespace BimmerStudio.Domain.Connections;

public enum ConnectionState
{
    Disconnected,
    Connecting,
    Ready,

    /// <summary>Executing a job, or streaming a continuous one.</summary>
    Busy,

    /// <summary>The link failed and will not recover without reconnecting.</summary>
    Faulted,
}
