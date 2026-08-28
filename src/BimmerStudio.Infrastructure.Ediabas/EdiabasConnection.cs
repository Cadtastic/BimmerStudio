using System.Threading.Channels;
using BimmerStudio.Application.Abstractions;
using BimmerStudio.Domain.Connections;
using BimmerStudio.Domain.Diagnostics;
using EdiabasLib;
using Microsoft.Extensions.Logging;

namespace BimmerStudio.Infrastructure.Ediabas;

/// <summary>
/// Wraps one <see cref="EdiabasNet"/> instance so the rest of the app can await it.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="EdiabasNet.ExecuteJob"/> blocks, and an instance is not safe to use from more than
/// one thread. Rather than make the interpreter asynchronous — which would buy nothing, since a
/// vehicle bus carries one request/response exchange at a time — this class gives each connection
/// a single dedicated thread with affinity to its interpreter and feeds it through a channel.
/// Callers get tasks; the interpreter keeps the deterministic timing that K-line depends on.
/// </para>
/// <para>
/// <b>One connection at a time per process.</b> Running several concurrently is not safe, and not
/// because of this class: <c>EdiabasNet</c> keeps process-wide static state, including an SGBD
/// cache that is cleared whenever the last live instance is disposed. Two connections used at
/// once fail intermittently on unpredictable SGBDs. Serialise interpreter work rather than
/// pooling instances; scale by doing less work, not by doing it in parallel.
/// </para>
/// </remarks>
internal sealed class EdiabasConnection : IDiagnosticConnection
{
    private readonly EdiabasNet _ediabas;
    private readonly ILogger<EdiabasConnection> _logger;
    private readonly Channel<WorkItem> _queue;
    private readonly Thread _worker;
    private readonly CancellationTokenSource _shutdown = new();
    private readonly SemaphoreSlim _sessionGate = new(1, 1);

    private volatile ConnectionState _state = ConnectionState.Ready;
    private EdiabasSession? _activeSession;
    private int _streamingReservations;
    private int _disposed;

    public EdiabasConnection(
        ConnectionProfile profile,
        EdiabasNet ediabas,
        ILogger<EdiabasConnection> logger)
    {
        Profile = profile;
        _ediabas = ediabas;
        _logger = logger;

        _queue = Channel.CreateUnbounded<WorkItem>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
        });

        _worker = new Thread(RunWorkerLoop)
        {
            Name = $"ediabas-{profile.Name}",
            IsBackground = true,
        };
        _worker.Start();
    }

    public ConnectionProfile Profile { get; }

    public ConnectionState State => _state;

    public async Task<IDiagnosticSession> OpenSessionAsync(
        SgbdIdentifier sgbd,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed == 1, this);
        ThrowIfStreaming();

        await _sessionGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // A group file is resolved by interrogating the vehicle, so this can fail for
            // reasons that are about the car rather than the file.
            var resolvedVariant = await RunAsync(
                ediabas =>
                {
                    ediabas.ResolveSgbdFile(sgbd.BaseName);
                    return ediabas.SgbdFileName ?? sgbd.BaseName;
                },
                cancellationToken).ConfigureAwait(false);

            var variantName = Path.GetFileNameWithoutExtension(resolvedVariant);
            _logger.LogDebug(
                "Loaded SGBD {Requested} on {Profile}, resolved variant {Variant}",
                sgbd.BaseName,
                Profile.Name,
                variantName);

            _activeSession?.Invalidate();
            var session = new EdiabasSession(this, sgbd, variantName);
            _activeSession = session;
            return session;
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not DiagnosticConnectionException)
        {
            if (EdiabasErrorClassifier.IndicatesMissingVehicle(ex))
            {
                throw new VehicleConnectionRequiredException(
                    sgbd.BaseName,
                    $"'{sgbd.BaseName}' cannot be loaded without a responding vehicle. "
                    + (sgbd.Kind == SgbdKind.Group
                        ? "Group files identify the fitted ECU by interrogating the car."
                        : "This ECU's description file runs an initialisation job on load."));
            }

            throw new DiagnosticConnectionException(
                $"Could not load SGBD '{sgbd.BaseName}': {ex.Message}", ex);
        }
        finally
        {
            _sessionGate.Release();
        }
    }

    /// <summary>
    /// Queues work onto the interpreter thread. The interpreter polls
    /// <see cref="EdiabasNet.AbortJobFunc"/> between operations, so a token cancelled mid-job
    /// aborts it rather than waiting for it to finish.
    /// </summary>
    internal async Task<T> RunAsync<T>(Func<EdiabasNet, T> work, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed == 1, this);
        cancellationToken.ThrowIfCancellationRequested();

        var item = new WorkItem(
            ediabas => work(ediabas),
            cancellationToken);

        if (!_queue.Writer.TryWrite(item))
        {
            throw new DiagnosticConnectionException("The connection is shutting down.");
        }

        // Cancelling before the worker picks the item up completes it without touching the wire.
        await using var registration = cancellationToken
            .Register(static state => ((WorkItem)state!).TryCancel(), item)
            .ConfigureAwait(false);

        var result = await item.Completion.ConfigureAwait(false);
        return (T)result!;
    }

    private void RunWorkerLoop()
    {
        var reader = _queue.Reader;

        try
        {
            while (!_shutdown.IsCancellationRequested)
            {
                if (!reader.WaitToReadAsync(_shutdown.Token).AsTask().GetAwaiter().GetResult())
                {
                    break;
                }

                while (reader.TryRead(out var item))
                {
                    ExecuteWorkItem(item);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
        catch (Exception ex)
        {
            _state = ConnectionState.Faulted;
            _logger.LogError(ex, "Interpreter worker for {Profile} stopped unexpectedly", Profile.Name);
        }
        finally
        {
            DrainPendingWork();
        }
    }

    private void ExecuteWorkItem(WorkItem item)
    {
        if (item.IsCompleted)
        {
            return;
        }

        var previousState = _state;
        _state = ConnectionState.Busy;

        try
        {
            // Must be assigned between jobs: the setter rejects changes while one is running.
            _ediabas.AbortJobFunc = item.ShouldAbort;
            item.Run(_ediabas);
        }
        finally
        {
            _ediabas.AbortJobFunc = null;
            if (_state != ConnectionState.Faulted)
            {
                _state = previousState == ConnectionState.Busy ? ConnectionState.Ready : previousState;
            }
        }
    }

    private void DrainPendingWork()
    {
        _queue.Writer.TryComplete();
        while (_queue.Reader.TryRead(out var pending))
        {
            pending.TryFail(new DiagnosticConnectionException("The connection was closed."));
        }
    }

    internal void ThrowIfStreaming()
    {
        if (Volatile.Read(ref _streamingReservations) > 0)
        {
            throw new SessionBusyException(
                "A continuous job is running on this connection. Stop it before starting other work.");
        }
    }

    /// <summary>Reserves the connection for a continuous job, excluding other execution.</summary>
    internal IDisposable ReserveForStreaming()
    {
        ThrowIfStreaming();
        Interlocked.Increment(ref _streamingReservations);
        return new StreamingReservation(this);
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
        {
            return;
        }

        _state = ConnectionState.Disconnected;
        await _shutdown.CancelAsync().ConfigureAwait(false);
        _queue.Writer.TryComplete();

        // Bounded so a wedged transport read cannot hang application shutdown.
        if (!_worker.Join(TimeSpan.FromSeconds(5)))
        {
            _logger.LogWarning("Interpreter worker for {Profile} did not stop in time", Profile.Name);
        }

        try
        {
            _ediabas.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error disposing interpreter for {Profile}", Profile.Name);
        }

        _shutdown.Dispose();
        _sessionGate.Dispose();
    }

    private sealed class StreamingReservation(EdiabasConnection connection) : IDisposable
    {
        private int _released;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _released, 1) == 0)
            {
                Interlocked.Decrement(ref connection._streamingReservations);
            }
        }
    }

    private sealed class WorkItem(Func<EdiabasNet, object?> work, CancellationToken cancellationToken)
    {
        private readonly TaskCompletionSource<object?> _completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<object?> Completion => _completion.Task;

        public bool IsCompleted => _completion.Task.IsCompleted;

        public bool ShouldAbort() => cancellationToken.IsCancellationRequested;

        public void Run(EdiabasNet ediabas)
        {
            try
            {
                var value = work(ediabas);

                // A job aborted through AbortJobFunc returns normally, so the token decides.
                if (cancellationToken.IsCancellationRequested)
                {
                    TryCancel();
                    return;
                }

                _completion.TrySetResult(value);
            }
            catch (Exception ex)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    TryCancel();
                    return;
                }

                _completion.TrySetException(ex);
            }
        }

        public void TryCancel() => _completion.TrySetCanceled(cancellationToken);

        public void TryFail(Exception exception) => _completion.TrySetException(exception);
    }
}
