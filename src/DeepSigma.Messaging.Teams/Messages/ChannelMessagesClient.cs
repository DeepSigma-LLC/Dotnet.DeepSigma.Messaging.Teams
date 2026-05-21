using System.Diagnostics;
using DeepSigma.Messaging.Teams.Diagnostics;
using DeepSigma.Messaging.Teams.Internal;
using DeepSigma.Messaging.Teams.Querying;
using Microsoft.Graph.Models;

namespace DeepSigma.Messaging.Teams.Messages;

internal sealed class ChannelMessagesClient : IChannelMessagesClient
{
    private readonly TeamsClientContext _context;
    private readonly string _teamId;
    private readonly string _channelId;

    public ChannelMessagesClient(TeamsClientContext context, string teamId, string channelId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(teamId);
        ArgumentException.ThrowIfNullOrWhiteSpace(channelId);
        _context = context;
        _teamId = teamId;
        _channelId = channelId;
    }

    public IAsyncEnumerable<TeamsMessage> ListAsync(
        QueryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return GraphListing.ListPagedAsync<ChatMessage, ChatMessageCollectionResponse, TeamsMessage>(
            _context,
            "teams.channel_messages.list",
            a =>
            {
                a?.SetTag(TeamsTelemetry.Tags.TeamId, _teamId);
                a?.SetTag(TeamsTelemetry.Tags.ChannelId, _channelId);
            },
            ct => _context.GraphClient.Teams[_teamId].Channels[_channelId].Messages.GetAsync(req =>
            {
                if (options?.Top is int top)
                {
                    req.QueryParameters.Top = top;
                }
                if (options?.Expand is { Count: > 0 } expand)
                {
                    req.QueryParameters.Expand = expand.ToArray();
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
        using var activity = TeamsTelemetry.ActivitySource.StartActivity("teams.channel_messages.get", ActivityKind.Client);
        activity?.SetTag(TeamsTelemetry.Tags.TeamId, _teamId);
        activity?.SetTag(TeamsTelemetry.Tags.ChannelId, _channelId);
        activity?.SetTag(TeamsTelemetry.Tags.MessageId, messageId);

        var message = await GraphErrorMapper.ExecuteAsync(() =>
            _context.GraphClient.Teams[_teamId].Channels[_channelId].Messages[messageId]
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
        using var activity = TeamsTelemetry.ActivitySource.StartActivity("teams.channel_messages.send", ActivityKind.Client);
        activity?.SetTag(TeamsTelemetry.Tags.TeamId, _teamId);
        activity?.SetTag(TeamsTelemetry.Tags.ChannelId, _channelId);

        var graphMessage = OutgoingMessageMapper.ToGraphChatMessage(message);
        var posted = await GraphErrorMapper.ExecuteAsync(() =>
            _context.GraphClient.Teams[_teamId].Channels[_channelId].Messages
                .PostAsync(graphMessage, cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (posted is null)
        {
            throw new Exceptions.TeamsApiException("Posting message returned a null response.");
        }
        return GraphMapper.ToTeamsMessage(posted);
    }

    public async Task<TeamsMessage> ReplyAsync(string parentMessageId, OutgoingMessage reply, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(parentMessageId);
        ArgumentNullException.ThrowIfNull(reply);
        using var activity = TeamsTelemetry.ActivitySource.StartActivity("teams.channel_messages.reply", ActivityKind.Client);
        activity?.SetTag(TeamsTelemetry.Tags.TeamId, _teamId);
        activity?.SetTag(TeamsTelemetry.Tags.ChannelId, _channelId);
        activity?.SetTag(TeamsTelemetry.Tags.MessageId, parentMessageId);

        var graphMessage = OutgoingMessageMapper.ToGraphChatMessage(reply);
        var posted = await GraphErrorMapper.ExecuteAsync(() =>
            _context.GraphClient.Teams[_teamId].Channels[_channelId].Messages[parentMessageId].Replies
                .PostAsync(graphMessage, cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (posted is null)
        {
            throw new Exceptions.TeamsApiException("Reply returned a null response.");
        }
        return GraphMapper.ToTeamsMessage(posted);
    }

    public IAsyncEnumerable<TeamsMessage> ListRepliesAsync(
        string parentMessageId,
        QueryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(parentMessageId);
        return GraphListing.ListPagedAsync<ChatMessage, ChatMessageCollectionResponse, TeamsMessage>(
            _context,
            "teams.channel_messages.list_replies",
            a =>
            {
                a?.SetTag(TeamsTelemetry.Tags.TeamId, _teamId);
                a?.SetTag(TeamsTelemetry.Tags.ChannelId, _channelId);
                a?.SetTag(TeamsTelemetry.Tags.MessageId, parentMessageId);
            },
            ct => _context.GraphClient.Teams[_teamId].Channels[_channelId].Messages[parentMessageId].Replies.GetAsync(req =>
            {
                if (options?.Top is int top)
                {
                    req.QueryParameters.Top = top;
                }
                if (options?.Expand is { Count: > 0 } expand)
                {
                    req.QueryParameters.Expand = expand.ToArray();
                }
            }, ct),
            p => p.Value,
            p => p.OdataNextLink,
            ChatMessageCollectionResponse.CreateFromDiscriminatorValue,
            GraphMapper.ToTeamsMessage,
            options?.MaxPages,
            cancellationToken);
    }
}
