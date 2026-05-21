using DeepSigma.Messaging.Teams.Models;
using DeepSigma.Messaging.Teams.Querying;

namespace DeepSigma.Messaging.Teams.Messages;

public interface IChannelMessagesClient
{
    IAsyncEnumerable<TeamsMessage> ListAsync(QueryOptions? options = null, CancellationToken cancellationToken = default);

    Task<TeamsMessage> GetAsync(string messageId, CancellationToken cancellationToken = default);

    Task<TeamsMessage> SendAsync(OutgoingMessage message, CancellationToken cancellationToken = default);

    Task<TeamsMessage> ReplyAsync(string parentMessageId, OutgoingMessage reply, CancellationToken cancellationToken = default);

    IAsyncEnumerable<TeamsMessage> ListRepliesAsync(string parentMessageId, QueryOptions? options = null, CancellationToken cancellationToken = default);
}
