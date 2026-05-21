using System.Text.RegularExpressions;
using DeepSigma.Messaging.Teams;
using DeepSigma.Messaging.Teams.AspNetCore;
using DeepSigma.Messaging.Teams.DependencyInjection;
using DeepSigma.Messaging.Teams.Messages;
using DeepSigma.Messaging.Teams.Subscriptions;

// samples.ChatBot — end-to-end demonstration of a Teams chatbot.
//
// What it does:
//   1. On startup, subscribes to ChannelMessage notifications for a single channel.
//   2. Hosts a webhook endpoint at /teams/notify that receives Graph change notifications.
//   3. For each new message, fetches the full message text and posts a reply.
//   4. On shutdown, deletes the subscription.
//
// Requirements:
//   • Public HTTPS URL for the webhook (use ngrok, Cloudflare Tunnel, or similar in dev).
//   • An app registration with these Graph permissions granted with admin consent:
//       - Application: ChannelMessage.Read.All, Subscription.Read.All
//       - For replying as the app, you need a configured Teams app/bot or use
//         delegated auth instead — see samples.PostMessage.
//
// Env vars:
//   TEAMS_TENANT_ID, TEAMS_CLIENT_ID, TEAMS_CLIENT_SECRET — app registration
//   TEAMS_TEAM_ID, TEAMS_CHANNEL_ID                      — channel to listen on
//   TEAMS_BOT_PUBLIC_URL                                 — public https URL of THIS app
//                                                          (e.g. https://abc123.ngrok.io)
//   TEAMS_BOT_CLIENT_STATE                               — any random shared secret
//
// Run:
//   ngrok http 5000          # in another terminal, copy the https URL into TEAMS_BOT_PUBLIC_URL
//   dotnet run --project samples/samples.ChatBot

var tenantId = Require("TEAMS_TENANT_ID");
var clientId = Require("TEAMS_CLIENT_ID");
var clientSecret = Require("TEAMS_CLIENT_SECRET");
var teamId = Require("TEAMS_TEAM_ID");
var channelId = Require("TEAMS_CHANNEL_ID");
var publicUrl = Require("TEAMS_BOT_PUBLIC_URL").TrimEnd('/');
var clientState = Require("TEAMS_BOT_CLIENT_STATE");

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTeamsClient(b => b.WithClientSecret(tenantId, clientId, clientSecret));
builder.Services.AddHostedService<SubscriptionLifecycle>();
builder.Services.AddSingleton(new BotConfig(teamId, channelId, publicUrl, clientState));

var app = builder.Build();

app.MapGet("/", () => "samples.ChatBot is running. Webhook is at /teams/notify.");

app.MapTeamsWebhook("/teams/notify", async (ctx, batch) =>
{
    var teams = ctx.RequestServices.GetRequiredService<ITeamsClient>();
    var log = ctx.RequestServices.GetRequiredService<ILogger<Program>>();

    foreach (var n in batch.Notifications)
    {
        // Resource format: teams('{teamId}')/channels('{channelId}')/messages('{messageId}')
        var match = ResourceRegex().Match(n.Resource);
        if (!match.Success)
        {
            log.LogWarning("Unrecognized resource format: {Resource}", n.Resource);
            continue;
        }

        var notifiedTeam = match.Groups["teamId"].Value;
        var notifiedChannel = match.Groups["channelId"].Value;
        var messageId = match.Groups["messageId"].Value;

        if (n.ChangeType != "created")
        {
            continue;
        }

        try
        {
            var msg = await teams.Messages.Channel(notifiedTeam, notifiedChannel).GetAsync(messageId);

            // Don't reply to our own replies — would otherwise create an infinite loop.
            if (msg.ReplyToId is not null || msg.From is null)
            {
                continue;
            }

            log.LogInformation("Replying to message {MessageId} from {Sender}", messageId, msg.From.DisplayName);

            await teams.Messages.Channel(notifiedTeam, notifiedChannel).ReplyAsync(
                messageId,
                new MessageBuilder().WithText($"hi {msg.From.DisplayName} 👋 — I see you typed something").Build());
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to handle notification {SubscriptionId}", n.SubscriptionId);
        }
    }
}, expectedClientState: clientState);

await app.RunAsync();

static string Require(string name)
{
    var value = Environment.GetEnvironmentVariable(name);
    if (string.IsNullOrEmpty(value))
    {
        Console.Error.WriteLine($"Set {name}.");
        Environment.Exit(1);
    }
    return value!;
}

internal sealed record BotConfig(string TeamId, string ChannelId, string PublicUrl, string ClientState);

internal sealed partial class Program
{
    [GeneratedRegex(@"teams\('(?<teamId>[^']+)'\)/channels\('(?<channelId>[^']+)'\)/messages\('(?<messageId>[^']+)'\)")]
    private static partial Regex ResourceRegex();
}

/// <summary>
/// Hosted service that creates a Graph subscription on startup, renews it periodically,
/// and tears it down on shutdown. Channel-message subscriptions max out at 60 minutes.
/// </summary>
internal sealed class SubscriptionLifecycle : BackgroundService
{
    private readonly ITeamsClient _teams;
    private readonly BotConfig _config;
    private readonly ILogger<SubscriptionLifecycle> _logger;
    private string? _subscriptionId;

    public SubscriptionLifecycle(ITeamsClient teams, BotConfig config, ILogger<SubscriptionLifecycle> logger)
    {
        _teams = teams;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var resource = SubscriptionResources.ChannelMessages(_config.TeamId, _config.ChannelId);
        var sub = await _teams.Subscriptions.SubscribeAsync(
            resource: resource,
            callbackUrl: $"{_config.PublicUrl}/teams/notify",
            clientState: _config.ClientState,
            expiresAt: DateTimeOffset.UtcNow.AddMinutes(55),
            changeTypes: ChangeTypes.Created,
            cancellationToken: stoppingToken);

        _subscriptionId = sub.Id;
        _logger.LogInformation("Subscribed: {SubscriptionId} → {Resource}, expires {ExpiresAt:u}",
            sub.Id, sub.Resource, sub.ExpiresAt);

        // Renew every 45 minutes (subscription lives 55, leaving a 10-minute cushion).
        var renewInterval = TimeSpan.FromMinutes(45);
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(renewInterval, stoppingToken);
                var renewed = await _teams.Subscriptions.RenewAsync(
                    sub.Id, DateTimeOffset.UtcNow.AddMinutes(55), stoppingToken);
                _logger.LogInformation("Renewed: expires {ExpiresAt:u}", renewed.ExpiresAt);
            }
        }
        catch (OperationCanceledException)
        {
            // Shutdown requested — fall through to teardown.
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        if (_subscriptionId is not null)
        {
            try
            {
                await _teams.Subscriptions.DeleteAsync(_subscriptionId, cancellationToken);
                _logger.LogInformation("Subscription {SubscriptionId} deleted on shutdown", _subscriptionId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete subscription {SubscriptionId} on shutdown", _subscriptionId);
            }
        }
    }
}
