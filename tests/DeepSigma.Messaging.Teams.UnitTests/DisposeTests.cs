using DeepSigma.Messaging.Teams;
using Xunit;

namespace DeepSigma.Messaging.Teams.UnitTests;

public class DisposeTests
{
    [Fact]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        var client = new TeamsClientBuilder()
            .WithClientSecret("tenant-id", "client-id", "client-secret")
            .Build();

        client.Dispose();
        var exception = Record.Exception(() => client.Dispose());

        Assert.Null(exception);
    }

    [Fact]
    public async Task DisposeAsync_CalledAfterDispose_DoesNotThrow()
    {
        var client = new TeamsClientBuilder()
            .WithClientSecret("tenant-id", "client-id", "client-secret")
            .Build();

        client.Dispose();
        var exception = await Record.ExceptionAsync(async () => await client.DisposeAsync());

        Assert.Null(exception);
    }

    [Fact]
    public async Task ListTeamsAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        var client = new TeamsClientBuilder()
            .WithClientSecret("tenant-id", "client-id", "client-secret")
            .Build();

        client.Dispose();

        var enumerator = client.Directory.ListTeamsAsync().GetAsyncEnumerator();
        await Assert.ThrowsAsync<ObjectDisposedException>(async () => await enumerator.MoveNextAsync());
    }
}
