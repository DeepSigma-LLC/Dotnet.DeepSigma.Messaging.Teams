using System.Diagnostics;
using DeepSigma.Messaging.Teams.Diagnostics;
using DeepSigma.Messaging.Teams.Internal;
using DeepSigma.Messaging.Teams.Querying;
using Microsoft.Graph.Models;

namespace DeepSigma.Messaging.Teams.Teams;

internal sealed class TeamsDirectory : ITeamsDirectory
{
    private readonly TeamsClientContext _context;

    public TeamsDirectory(TeamsClientContext context)
    {
        _context = context;
    }

    public IAsyncEnumerable<TeamSummary> ListTeamsAsync(
        QueryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        return GraphListing.ListPagedAsync<Team, TeamCollectionResponse, TeamSummary>(
            _context,
            "teams.directory.list_teams",
            a => a?.SetTag(TeamsTelemetry.Tags.Operation, "list_teams"),
            ct => _context.GraphClient.Teams.GetAsync(req => BindTeamsQueryParameters(req.QueryParameters, options), ct),
            p => p.Value,
            p => p.OdataNextLink,
            TeamCollectionResponse.CreateFromDiscriminatorValue,
            GraphMapper.ToTeamSummary,
            options?.MaxPages,
            cancellationToken);
    }

    public IAsyncEnumerable<TeamSummary> ListJoinedTeamsAsync(
        string userId,
        QueryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        return GraphListing.ListPagedAsync<Team, TeamCollectionResponse, TeamSummary>(
            _context,
            "teams.directory.list_joined_teams",
            a => a?.SetTag(TeamsTelemetry.Tags.UserId, userId),
            ct => _context.GraphClient.Users[userId].JoinedTeams.GetAsync(req =>
            {
                if (options?.Top is int top)
                {
                    req.QueryParameters.Top = top;
                }
                if (options?.Select is { Count: > 0 } select)
                {
                    req.QueryParameters.Select = select.ToArray();
                }
                if (options?.Expand is { Count: > 0 } expand)
                {
                    req.QueryParameters.Expand = expand.ToArray();
                }
            }, ct),
            p => p.Value,
            p => p.OdataNextLink,
            TeamCollectionResponse.CreateFromDiscriminatorValue,
            GraphMapper.ToTeamSummary,
            options?.MaxPages,
            cancellationToken);
    }

    public async Task<TeamSummary> GetTeamAsync(string teamId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(teamId);
        using var activity = TeamsTelemetry.ActivitySource.StartActivity("teams.directory.get_team", ActivityKind.Client);
        activity?.SetTag(TeamsTelemetry.Tags.TeamId, teamId);

        var team = await GraphErrorMapper.ExecuteAsync(() =>
            _context.GraphClient.Teams[teamId].GetAsync(cancellationToken: cancellationToken)).ConfigureAwait(false);
        if (team is null)
        {
            throw new Exceptions.TeamsApiException($"Team '{teamId}' returned a null response.");
        }
        return GraphMapper.ToTeamSummary(team);
    }

    public IAsyncEnumerable<ChannelSummary> ListChannelsAsync(
        string teamId,
        QueryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(teamId);
        return GraphListing.ListPagedAsync<Channel, ChannelCollectionResponse, ChannelSummary>(
            _context,
            "teams.directory.list_channels",
            a => a?.SetTag(TeamsTelemetry.Tags.TeamId, teamId),
            ct => _context.GraphClient.Teams[teamId].Channels.GetAsync(req =>
            {
                if (options?.Filter is string filter)
                {
                    req.QueryParameters.Filter = filter;
                }
                if (options?.Select is { Count: > 0 } select)
                {
                    req.QueryParameters.Select = select.ToArray();
                }
            }, ct),
            p => p.Value,
            p => p.OdataNextLink,
            ChannelCollectionResponse.CreateFromDiscriminatorValue,
            channel => GraphMapper.ToChannelSummary(channel, teamId),
            options?.MaxPages,
            cancellationToken);
    }

    public async Task<ChannelSummary> GetChannelAsync(string teamId, string channelId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(teamId);
        ArgumentException.ThrowIfNullOrWhiteSpace(channelId);
        using var activity = TeamsTelemetry.ActivitySource.StartActivity("teams.directory.get_channel", ActivityKind.Client);
        activity?.SetTag(TeamsTelemetry.Tags.TeamId, teamId);
        activity?.SetTag(TeamsTelemetry.Tags.ChannelId, channelId);

        var channel = await GraphErrorMapper.ExecuteAsync(() =>
            _context.GraphClient.Teams[teamId].Channels[channelId].GetAsync(cancellationToken: cancellationToken))
            .ConfigureAwait(false);
        if (channel is null)
        {
            throw new Exceptions.TeamsApiException($"Channel '{channelId}' returned a null response.");
        }
        return GraphMapper.ToChannelSummary(channel, teamId);
    }

    public IAsyncEnumerable<TeamsUser> ListMembersAsync(
        string teamId,
        QueryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(teamId);
        return GraphListing.ListPagedAsync<ConversationMember, ConversationMemberCollectionResponse, TeamsUser>(
            _context,
            "teams.directory.list_members",
            a => a?.SetTag(TeamsTelemetry.Tags.TeamId, teamId),
            ct => _context.GraphClient.Teams[teamId].Members.GetAsync(req =>
            {
                if (options?.Top is int top)
                {
                    req.QueryParameters.Top = top;
                }
                if (options?.Filter is string filter)
                {
                    req.QueryParameters.Filter = filter;
                }
                if (options?.Select is { Count: > 0 } select)
                {
                    req.QueryParameters.Select = select.ToArray();
                }
            }, ct),
            p => p.Value,
            p => p.OdataNextLink,
            ConversationMemberCollectionResponse.CreateFromDiscriminatorValue,
            ToTeamsUser,
            options?.MaxPages,
            cancellationToken);
    }

    private static TeamsUser ToTeamsUser(ConversationMember member)
    {
        var aad = member as AadUserConversationMember;
        return new TeamsUser(
            Id: aad?.UserId ?? member.Id ?? string.Empty,
            DisplayName: member.DisplayName,
            UserPrincipalName: null,
            Email: aad?.Email);
    }

    private static void BindTeamsQueryParameters(Microsoft.Graph.Teams.TeamsRequestBuilder.TeamsRequestBuilderGetQueryParameters qp, QueryOptions? options)
    {
        if (options is null)
        {
            return;
        }
        if (options.Top is int top)
        {
            qp.Top = top;
        }
        if (options.Filter is string filter)
        {
            qp.Filter = filter;
        }
        if (options.OrderBy is string orderBy)
        {
            qp.Orderby = new[] { orderBy };
        }
        if (options.Select is { Count: > 0 } select)
        {
            qp.Select = select.ToArray();
        }
        if (options.Expand is { Count: > 0 } expand)
        {
            qp.Expand = expand.ToArray();
        }
        if (options.Search is string search)
        {
            qp.Search = search;
        }
    }
}
