using DeepSigma.Messaging.Teams;
using Xunit;

namespace DeepSigma.Messaging.Teams.UnitTests;

public class TeamsClientBuilderTests
{
    [Fact]
    public void Build_WithoutCredential_Throws()
    {
        var builder = new TeamsClientBuilder();
        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public void Build_WithClientSecret_ReturnsClient()
    {
        var builder = new TeamsClientBuilder()
            .WithClientSecret("tenant-id", "client-id", "client-secret");

        var client = builder.Build();

        Assert.NotNull(client);
        Assert.NotNull(client.Directory);
        Assert.NotNull(client.Messages);
        Assert.NotNull(client.Chats);
        Assert.NotNull(client.Subscriptions);
    }
}
