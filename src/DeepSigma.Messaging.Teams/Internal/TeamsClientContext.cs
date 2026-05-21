using Microsoft.Extensions.Logging;
using Microsoft.Graph;

namespace DeepSigma.Messaging.Teams.Internal;

internal sealed class TeamsClientContext
{
    private readonly GraphServiceClient _graphClient;
    private int _disposed;

    public TeamsClientContext(GraphServiceClient graphClient, ILoggerFactory loggerFactory, TeamsClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(graphClient);
        ArgumentNullException.ThrowIfNull(loggerFactory);
        ArgumentNullException.ThrowIfNull(options);
        _graphClient = graphClient;
        LoggerFactory = loggerFactory;
        Options = options;
    }

    /// <summary>
    /// Throws <see cref="ObjectDisposedException"/> if the owning client has been disposed.
    /// All sub-clients should access Graph through this property.
    /// </summary>
    public GraphServiceClient GraphClient
    {
        get
        {
            ThrowIfDisposed();
            return _graphClient;
        }
    }

    public ILoggerFactory LoggerFactory { get; }

    public TeamsClientOptions Options { get; }

    public bool IsDisposed => Volatile.Read(ref _disposed) != 0;

    public void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(IsDisposed, typeof(TeamsClient));

    /// <summary>
    /// Atomically transitions to the disposed state, disposing the underlying Graph client
    /// exactly once. Subsequent calls are no-ops.
    /// </summary>
    public void DisposeGraphClient()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            _graphClient.Dispose();
        }
    }
}
