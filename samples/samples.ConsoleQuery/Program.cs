using DeepSigma.Messaging.Teams;
using DeepSigma.Messaging.Teams.Querying;

var tenantId = Environment.GetEnvironmentVariable("TEAMS_TENANT_ID");
var clientId = Environment.GetEnvironmentVariable("TEAMS_CLIENT_ID");
var clientSecret = Environment.GetEnvironmentVariable("TEAMS_CLIENT_SECRET");
var teamId = Environment.GetEnvironmentVariable("TEAMS_TEAM_ID");
var channelId = Environment.GetEnvironmentVariable("TEAMS_CHANNEL_ID");

if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
{
    Console.Error.WriteLine("Set TEAMS_TENANT_ID, TEAMS_CLIENT_ID, TEAMS_CLIENT_SECRET (and optionally TEAMS_TEAM_ID / TEAMS_CHANNEL_ID).");
    return 1;
}

var client = new TeamsClientBuilder()
    .WithClientSecret(tenantId, clientId, clientSecret)
    .Build();

Console.WriteLine("=== Teams in tenant ===");
await foreach (var team in client.Directory.ListTeamsAsync())
{
    Console.WriteLine($"  {team.Id}  {team.DisplayName}");
}

if (!string.IsNullOrEmpty(teamId))
{
    Console.WriteLine();
    Console.WriteLine($"=== Channels in team {teamId} ===");
    await foreach (var channel in client.Directory.ListChannelsAsync(teamId))
    {
        Console.WriteLine($"  {channel.Id}  {channel.DisplayName}  ({channel.MembershipType})");
    }

    if (!string.IsNullOrEmpty(channelId))
    {
        Console.WriteLine();
        Console.WriteLine($"=== Last 10 messages in channel {channelId} ===");
        var messages = client.Messages.Channel(teamId, channelId)
            .ListAsync(new QueryOptions { Top = 10, MaxPages = 1 });

        var count = 0;
        await foreach (var msg in messages)
        {
            Console.WriteLine($"  [{msg.CreatedAt:u}] {msg.From?.DisplayName ?? "(unknown)"}: {Truncate(msg.Body.Content, 80)}");
            if (++count >= 10)
            {
                break;
            }
        }
    }
}

return 0;

static string Truncate(string s, int max) => s.Length <= max ? s : string.Concat(s.AsSpan(0, max), "…");
