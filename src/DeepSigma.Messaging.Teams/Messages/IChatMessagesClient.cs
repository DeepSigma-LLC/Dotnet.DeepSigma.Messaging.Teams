using DeepSigma.Messaging.Teams.Models;
using DeepSigma.Messaging.Teams.Querying;

namespace DeepSigma.Messaging.Teams.Messages;

public interface IChatMessagesClient
{
    IAsyncEnumerable<TeamsMessage> ListAsync(QueryOptions? options = null, CancellationToken cancellationToken = default);

    Task<TeamsMessage> GetAsync(string messageId, CancellationToken cancellationToken = default);

    Task<TeamsMessage> SendAsync(OutgoingMessage message, CancellationToken cancellationToken = default);
}
