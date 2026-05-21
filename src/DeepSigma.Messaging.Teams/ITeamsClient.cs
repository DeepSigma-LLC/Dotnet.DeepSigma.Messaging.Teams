using DeepSigma.Messaging.Teams.Chats;
using DeepSigma.Messaging.Teams.Messages;
using DeepSigma.Messaging.Teams.Subscriptions;
using DeepSigma.Messaging.Teams.Teams;

namespace DeepSigma.Messaging.Teams;

public interface ITeamsClient : IDisposable, IAsyncDisposable
{
    ITeamsDirectory Directory { get; }

    IMessagesGateway Messages { get; }

    IChatsClient Chats { get; }

    ISubscriptionsClient Subscriptions { get; }
}
