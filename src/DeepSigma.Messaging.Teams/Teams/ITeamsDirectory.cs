using DeepSigma.Messaging.Teams.Models;
using DeepSigma.Messaging.Teams.Querying;

namespace DeepSigma.Messaging.Teams.Teams;

public interface ITeamsDirectory
{
    IAsyncEnumerable<TeamSummary> ListTeamsAsync(QueryOptions? options = null, CancellationToken cancellationToken = default);

    IAsyncEnumerable<TeamSummary> ListJoinedTeamsAsync(string userId, QueryOptions? options = null, CancellationToken cancellationToken = default);

    Task<TeamSummary> GetTeamAsync(string teamId, CancellationToken cancellationToken = default);

    IAsyncEnumerable<ChannelSummary> ListChannelsAsync(string teamId, QueryOptions? options = null, CancellationToken cancellationToken = default);

    Task<ChannelSummary> GetChannelAsync(string teamId, string channelId, CancellationToken cancellationToken = default);

    IAsyncEnumerable<TeamsUser> ListMembersAsync(string teamId, QueryOptions? options = null, CancellationToken cancellationToken = default);
}
