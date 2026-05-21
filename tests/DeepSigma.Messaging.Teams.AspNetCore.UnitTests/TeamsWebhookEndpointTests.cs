using System.Net;
using System.Net.Http.Json;
using System.Text;
using DeepSigma.Messaging.Teams.AspNetCore;
using DeepSigma.Messaging.Teams.Subscriptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace DeepSigma.Messaging.Teams.AspNetCore.UnitTests;

public class TeamsWebhookEndpointTests
{
    private static (TestServer server, List<ChangeNotificationBatch> received) BuildServer(string? expectedClientState = null)
    {
        var received = new List<ChangeNotificationBatch>();

        var builder = Host.CreateDefaultBuilder()
            .ConfigureWebHost(web => web
                .UseTestServer()
                .ConfigureServices(services => services.AddRouting())
                .Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapTeamsWebhook("/notify",
                            (ctx, batch) =>
                            {
                                received.Add(batch);
                                return Task.CompletedTask;
                            },
                            expectedClientState);
                    });
                }));

        var host = builder.Start();
        var server = host.GetTestServer();
        return (server, received);
    }

    [Fact]
    public async Task GET_ValidationToken_EchoesTokenAsPlainText()
    {
        var (server, _) = BuildServer();
        using var client = server.CreateClient();

        var response = await client.PostAsync("/notify?validationToken=hello-world", new StringContent(""));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/plain", response.Content.Headers.ContentType!.MediaType);
        Assert.Equal("hello-world", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Post_ValidBatch_InvokesHandlerAndReturns202()
    {
        var (server, received) = BuildServer();
        using var client = server.CreateClient();

        var payload = new
        {
            value = new[]
            {
                new
                {
                    subscriptionId = "sub-1",
                    changeType = "created",
                    resource = "teams/abc/channels/def/messages/123",
                    tenantId = "tenant-1",
                    clientState = "secret",
                    subscriptionExpirationDateTime = "2026-06-01T12:00:00Z",
                },
            },
        };

        var response = await client.PostAsJsonAsync("/notify", payload);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var batch = Assert.Single(received);
        var notification = Assert.Single(batch.Notifications);
        Assert.Equal("sub-1", notification.SubscriptionId);
        Assert.Equal("created", notification.ChangeType);
        Assert.Equal("teams/abc/channels/def/messages/123", notification.Resource);
        Assert.Equal("tenant-1", notification.TenantId);
        Assert.Equal("secret", notification.ClientState);
        Assert.Equal(new DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero), notification.SubscriptionExpiresAt);
    }

    [Fact]
    public async Task Post_WrongClientState_Returns401AndDoesNotInvokeHandler()
    {
        var (server, received) = BuildServer(expectedClientState: "expected");
        using var client = server.CreateClient();

        var payload = new
        {
            value = new[]
            {
                new { subscriptionId = "sub-1", changeType = "created", resource = "r", clientState = "tampered" },
            },
        };

        var response = await client.PostAsJsonAsync("/notify", payload);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Empty(received);
    }

    [Fact]
    public async Task Post_MatchingClientState_Accepted()
    {
        var (server, received) = BuildServer(expectedClientState: "expected");
        using var client = server.CreateClient();

        var payload = new
        {
            value = new[]
            {
                new { subscriptionId = "sub-1", changeType = "updated", resource = "r", clientState = "expected" },
            },
        };

        var response = await client.PostAsJsonAsync("/notify", payload);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Single(received);
    }

    [Fact]
    public async Task Post_MissingValueArray_Returns400()
    {
        var (server, received) = BuildServer();
        using var client = server.CreateClient();

        var response = await client.PostAsync(
            "/notify",
            new StringContent("{\"unrelated\":42}", Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(received);
    }
}
