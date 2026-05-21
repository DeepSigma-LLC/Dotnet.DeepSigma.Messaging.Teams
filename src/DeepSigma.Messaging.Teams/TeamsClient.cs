using DeepSigma.Messaging.Teams.Authentication;
using DeepSigma.Messaging.Teams.Chats;
using DeepSigma.Messaging.Teams.Internal;
using DeepSigma.Messaging.Teams.Messages;
using DeepSigma.Messaging.Teams.Subscriptions;
using DeepSigma.Messaging.Teams.Teams;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Graph;

namespace DeepSigma.Messaging.Teams;

public sealed class TeamsClient : ITeamsClient
{
    private readonly TeamsClientContext _context;

    public TeamsClient(ITeamsCredential credential, TeamsClientOptions? options = null, ILoggerFactory? loggerFactory = null)
    {
        ArgumentNullException.ThrowIfNull(credential);
        var resolvedOptions = options ?? new TeamsClientOptions();
        var resolvedLoggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        var graph = CreateGraphClient(credential, resolvedOptions);
        _context = new TeamsClientContext(graph, resolvedLoggerFactory, resolvedOptions);

        Directory = new TeamsDirectory(_context);
        Messages = new MessagesGateway(_context);
        Chats = new ChatsClient(_context);
        Subscriptions = new SubscriptionsClient(_context);
    }

    internal TeamsClient(TeamsClientContext context)
    {
        _context = context;
        Directory = new TeamsDirectory(_context);
        Messages = new MessagesGateway(_context);
        Chats = new ChatsClient(_context);
        Subscriptions = new SubscriptionsClient(_context);
    }

    public ITeamsDirectory Directory { get; }

    public IMessagesGateway Messages { get; }

    public IChatsClient Chats { get; }

    public ISubscriptionsClient Subscriptions { get; }

    public void Dispose() => _context.DisposeGraphClient();

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }

    internal static GraphServiceClient CreateGraphClient(ITeamsCredential credential, TeamsClientOptions options)
    {
        ArgumentNullException.ThrowIfNull(credential);
        var scopes = credential.Scopes.ToArray();
        return string.IsNullOrEmpty(options.BaseUrl)
            ? new GraphServiceClient(credential.ToTokenCredential(), scopes)
            : new GraphServiceClient(credential.ToTokenCredential(), scopes, options.BaseUrl);
    }
}
