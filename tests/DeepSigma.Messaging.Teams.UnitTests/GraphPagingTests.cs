using DeepSigma.Messaging.Teams.Internal;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Serialization;
using NSubstitute;
using Xunit;

namespace DeepSigma.Messaging.Teams.UnitTests;

public class GraphPagingTests
{
    private static GraphServiceClient CreateClient(IRequestAdapter? adapter = null)
    {
        return new GraphServiceClient(adapter ?? Substitute.For<IRequestAdapter>());
    }

    [Fact]
    public async Task EnumerateAsync_NullFirstPage_YieldsNothing()
    {
        var client = CreateClient();

        var items = new List<Team>();
        await foreach (var item in GraphPaging.EnumerateAsync<Team, TeamCollectionResponse>(
            client, firstPage: null,
            p => p.Value,
            p => p.OdataNextLink,
            TeamCollectionResponse.CreateFromDiscriminatorValue,
            maxPages: null,
            CancellationToken.None))
        {
            items.Add(item);
        }

        Assert.Empty(items);
    }

    [Fact]
    public async Task EnumerateAsync_SinglePageNoNextLink_YieldsAllItems()
    {
        var client = CreateClient();
        var firstPage = new TeamCollectionResponse
        {
            Value = new List<Team>
            {
                new() { Id = "1", DisplayName = "Alpha" },
                new() { Id = "2", DisplayName = "Bravo" },
            },
            OdataNextLink = null,
        };

        var items = new List<Team>();
        await foreach (var item in GraphPaging.EnumerateAsync<Team, TeamCollectionResponse>(
            client, firstPage,
            p => p.Value,
            p => p.OdataNextLink,
            TeamCollectionResponse.CreateFromDiscriminatorValue,
            maxPages: null,
            CancellationToken.None))
        {
            items.Add(item);
        }

        Assert.Equal(2, items.Count);
        Assert.Equal("1", items[0].Id);
        Assert.Equal("2", items[1].Id);
    }

    [Fact]
    public async Task EnumerateAsync_MaxPagesOne_DoesNotFollowNextLink()
    {
        var adapter = Substitute.For<IRequestAdapter>();
        var client = CreateClient(adapter);
        var firstPage = new TeamCollectionResponse
        {
            Value = new List<Team> { new() { Id = "1" } },
            OdataNextLink = "https://graph.microsoft.com/v1.0/teams?$skiptoken=abc",
        };

        var items = new List<Team>();
        await foreach (var item in GraphPaging.EnumerateAsync<Team, TeamCollectionResponse>(
            client, firstPage,
            p => p.Value,
            p => p.OdataNextLink,
            TeamCollectionResponse.CreateFromDiscriminatorValue,
            maxPages: 1,
            CancellationToken.None))
        {
            items.Add(item);
        }

        Assert.Single(items);
        await adapter.DidNotReceive().SendAsync(
            Arg.Any<RequestInformation>(),
            Arg.Any<ParsableFactory<TeamCollectionResponse>>(),
            Arg.Any<Dictionary<string, ParsableFactory<IParsable>>>()!,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnumerateAsync_NextLink_BuildsRequestWithUri()
    {
        var adapter = Substitute.For<IRequestAdapter>();
        var client = CreateClient(adapter);
        var firstPage = new TeamCollectionResponse
        {
            Value = new List<Team> { new() { Id = "1" } },
            OdataNextLink = "https://graph.microsoft.com/v1.0/teams?$skiptoken=abc",
        };
        var secondPage = new TeamCollectionResponse
        {
            Value = new List<Team> { new() { Id = "2" } },
            OdataNextLink = null,
        };

        RequestInformation? capturedRequest = null;
        adapter.SendAsync(
                Arg.Do<RequestInformation>(r => capturedRequest = r),
                Arg.Any<ParsableFactory<TeamCollectionResponse>>(),
                Arg.Any<Dictionary<string, ParsableFactory<IParsable>>>()!,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<TeamCollectionResponse?>(secondPage));

        var items = new List<Team>();
        await foreach (var item in GraphPaging.EnumerateAsync<Team, TeamCollectionResponse>(
            client, firstPage,
            p => p.Value,
            p => p.OdataNextLink,
            TeamCollectionResponse.CreateFromDiscriminatorValue,
            maxPages: null,
            CancellationToken.None))
        {
            items.Add(item);
        }

        Assert.Equal(2, items.Count);
        Assert.NotNull(capturedRequest);
        Assert.Equal(Method.GET, capturedRequest!.HttpMethod);
        Assert.Equal(new Uri("https://graph.microsoft.com/v1.0/teams?$skiptoken=abc"), capturedRequest.URI);
    }
}
