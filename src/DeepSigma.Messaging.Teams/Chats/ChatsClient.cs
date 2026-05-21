using System.Diagnostics;
using DeepSigma.Messaging.Teams.Diagnostics;
using DeepSigma.Messaging.Teams.Internal;
using DeepSigma.Messaging.Teams.Messages;
using DeepSigma.Messaging.Teams.Querying;
using Microsoft.Graph.Models;

namespace DeepSigma.Messaging.Teams.Chats;

internal sealed class ChatsClient : IChatsClient
{
    private readonly TeamsClientContext _context;

    public ChatsClient(TeamsClientContext context)
    {
        _context = context;
    }

    public IAsyncEnumerable<ChatSummary> ListMyChatsAsync(
        QueryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return GraphListing.ListPagedAsync<Chat, ChatCollectionResponse, ChatSummary>(
            _context,
            "teams.chats.list",
            configureActivity: null,
            ct => _context.GraphClient.Me.Chats.GetAsync(req =>
            {
                if (options?.Top is int top)
                {
                    req.QueryParameters.Top = top;
                }
                if (options?.Filter is string filter)
                {
                    req.QueryParameters.Filter = filter;
                }
                if (options?.Expand is { Count: > 0 } expand)
                {
                    req.QueryParameters.Expand = expand.ToArray();
                }
            }, ct),
            p => p.Value,
            p => p.OdataNextLink,
            ChatCollectionResponse.CreateFromDiscriminatorValue,
            GraphMapper.ToChatSummary,
            options?.MaxPages,
            cancellationToken);
    }

    public async Task<ChatSummary> GetAsync(string chatId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(chatId);
        using var activity = TeamsTelemetry.ActivitySource.StartActivity("teams.chats.get", ActivityKind.Client);
        activity?.SetTag(TeamsTelemetry.Tags.ChatId, chatId);

        var chat = await GraphErrorMapper.ExecuteAsync(() =>
            _context.GraphClient.Chats[chatId].GetAsync(cancellationToken: cancellationToken)).ConfigureAwait(false);
        if (chat is null)
        {
            throw new Exceptions.TeamsApiException($"Chat '{chatId}' returned a null response.");
        }
        return GraphMapper.ToChatSummary(chat);
    }

    public async Task<ChatSummary> CreateOneOnOneAsync(string userIdA, string userIdB, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userIdA);
        ArgumentException.ThrowIfNullOrWhiteSpace(userIdB);
        using var activity = TeamsTelemetry.ActivitySource.StartActivity("teams.chats.create_one_on_one", ActivityKind.Client);
        activity?.SetTag(TeamsTelemetry.Tags.UserId, userIdB);

        var chat = new Chat
        {
            ChatType = ChatType.OneOnOne,
            Members = new List<ConversationMember>
            {
                BuildOwnerMember(userIdA),
                BuildOwnerMember(userIdB),
            },
        };
        var posted = await GraphErrorMapper.ExecuteAsync(() =>
            _context.GraphClient.Chats.PostAsync(chat, cancellationToken: cancellationToken)).ConfigureAwait(false);
        if (posted is null)
        {
            throw new Exceptions.TeamsApiException("Creating chat returned a null response.");
        }
        return GraphMapper.ToChatSummary(posted);
    }

    public async Task<ChatSummary> CreateGroupAsync(string topic, IEnumerable<string> userIds, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentNullException.ThrowIfNull(userIds);
        using var activity = TeamsTelemetry.ActivitySource.StartActivity("teams.chats.create_group", ActivityKind.Client);

        var members = userIds.Select(BuildOwnerMember).Cast<ConversationMember>().ToList();
        if (members.Count < 2)
        {
            throw new ArgumentException("A group chat requires at least two members.", nameof(userIds));
        }

        var chat = new Chat
        {
            ChatType = ChatType.Group,
            Topic = topic,
            Members = members,
        };
        var posted = await GraphErrorMapper.ExecuteAsync(() =>
            _context.GraphClient.Chats.PostAsync(chat, cancellationToken: cancellationToken)).ConfigureAwait(false);
        if (posted is null)
        {
            throw new Exceptions.TeamsApiException("Creating group chat returned a null response.");
        }
        return GraphMapper.ToChatSummary(posted);
    }

    public IChatMessagesClient Messages(string chatId) => new ChatMessagesClient(_context, chatId);

    private AadUserConversationMember BuildOwnerMember(string userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        var baseUrl = _context.GraphClient.RequestAdapter.BaseUrl?.TrimEnd('/');
        if (string.IsNullOrEmpty(baseUrl))
        {
            throw new InvalidOperationException(
                "Graph base URL is not configured. Cannot construct user@odata.bind for chat member.");
        }
        var member = new AadUserConversationMember
        {
            Roles = new List<string> { "owner" },
        };
        member.AdditionalData["user@odata.bind"] = $"{baseUrl}/users('{userId}')";
        return member;
    }
}
