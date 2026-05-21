using System.Diagnostics;
using DeepSigma.Messaging.Teams.Diagnostics;
using DeepSigma.Messaging.Teams.Internal;
using DeepSigma.Messaging.Teams.Querying;
using Microsoft.Graph.Models;

namespace DeepSigma.Messaging.Teams.Messages;

internal sealed class ChatMessagesClient : IChatMessagesClient
{
    private readonly TeamsClientContext _context;
    private readonly string _chatId;

    public ChatMessagesClient(TeamsClientContext context, string chatId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(chatId);
        _context = context;
        _chatId = chatId;
    }

    public IAsyncEnumerable<TeamsMessage> ListAsync(
        QueryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return GraphListing.ListPagedAsync<ChatMessage, ChatMessageCollectionResponse, TeamsMessage>(
            _context,
            "teams.chat_messages.list",
            a => a?.SetTag(TeamsTelemetry.Tags.ChatId, _chatId),
            ct => _context.GraphClient.Chats[_chatId].Messages.GetAsync(req =>
            {
                if (options?.Top is int top)
                {
                    req.QueryParameters.Top = top;
                }
            }, ct),
            p => p.Value,
            p => p.OdataNextLink,
            ChatMessageCollectionResponse.CreateFromDiscriminatorValue,
            GraphMapper.ToTeamsMessage,
            options?.MaxPages,
            cancellationToken);
    }

    public async Task<TeamsMessage> GetAsync(string messageId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        using var activity = TeamsTelemetry.ActivitySource.StartActivity("teams.chat_messages.get", ActivityKind.Client);
        activity?.SetTag(TeamsTelemetry.Tags.ChatId, _chatId);
        activity?.SetTag(TeamsTelemetry.Tags.MessageId, messageId);

        var message = await GraphErrorMapper.ExecuteAsync(() =>
            _context.GraphClient.Chats[_chatId].Messages[messageId]
                .GetAsync(cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (message is null)
        {
            throw new Exceptions.TeamsApiException($"Message '{messageId}' returned a null response.");
        }
        return GraphMapper.ToTeamsMessage(message);
    }

    public async Task<TeamsMessage> SendAsync(OutgoingMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        using var activity = TeamsTelemetry.ActivitySource.StartActivity("teams.chat_messages.send", ActivityKind.Client);
        activity?.SetTag(TeamsTelemetry.Tags.ChatId, _chatId);

        var graphMessage = OutgoingMessageMapper.ToGraphChatMessage(message);
        var posted = await GraphErrorMapper.ExecuteAsync(() =>
            _context.GraphClient.Chats[_chatId].Messages
                .PostAsync(graphMessage, cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (posted is null)
        {
            throw new Exceptions.TeamsApiException("Posting chat message returned a null response.");
        }
        return GraphMapper.ToTeamsMessage(posted);
    }
}
