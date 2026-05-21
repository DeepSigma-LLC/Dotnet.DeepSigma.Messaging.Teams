using DeepSigma.Messaging.Teams.Messages;
using DeepSigma.Messaging.Teams.Models;
using DeepSigma.Messaging.Teams.Querying;

namespace DeepSigma.Messaging.Teams.Chats;

public interface IChatsClient
{
    IAsyncEnumerable<ChatSummary> ListMyChatsAsync(QueryOptions? options = null, CancellationToken cancellationToken = default);

    Task<ChatSummary> GetAsync(string chatId, CancellationToken cancellationToken = default);

    Task<ChatSummary> CreateOneOnOneAsync(string userIdA, string userIdB, CancellationToken cancellationToken = default);

    Task<ChatSummary> CreateGroupAsync(string topic, IEnumerable<string> userIds, CancellationToken cancellationToken = default);

    IChatMessagesClient Messages(string chatId);
}
