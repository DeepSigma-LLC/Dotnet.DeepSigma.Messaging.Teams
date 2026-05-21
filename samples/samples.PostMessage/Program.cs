using DeepSigma.Messaging.Teams;
using DeepSigma.Messaging.Teams.Authentication;
using DeepSigma.Messaging.Teams.Exceptions;
using DeepSigma.Messaging.Teams.Messages;
using DeepSigma.Messaging.Teams.Models;

// samples.PostMessage — demonstrates posting messages to a channel and to a 1:1 chat.
//
// IMPORTANT: Posting channel messages requires DELEGATED auth (a signed-in user).
// Microsoft Graph does not support app-only posting outside of migration scenarios
// (Teamwork.Migrate.All). This sample uses device-code flow for that reason.
//
// Required env vars:
//   TEAMS_TENANT_ID, TEAMS_CLIENT_ID           — your app registration
//   TEAMS_TEAM_ID,   TEAMS_CHANNEL_ID          — target channel
//   TEAMS_OTHER_USER_ID                        — (optional) AAD user id for 1:1 chat demo
//
// Required Graph delegated scopes on the app registration:
//   ChannelMessage.Send, Chat.ReadWrite, Team.ReadBasic.All, User.Read

var tenantId = Require("TEAMS_TENANT_ID");
var clientId = Require("TEAMS_CLIENT_ID");
var teamId = Require("TEAMS_TEAM_ID");
var channelId = Require("TEAMS_CHANNEL_ID");
var otherUserId = Environment.GetEnvironmentVariable("TEAMS_OTHER_USER_ID");
var myUserId = Environment.GetEnvironmentVariable("TEAMS_MY_USER_ID");

using var client = (TeamsClient)new TeamsClientBuilder()
    .WithDeviceCode(tenantId, clientId, (info, _) =>
    {
        Console.WriteLine();
        Console.WriteLine(info.Message);
        Console.WriteLine();
        return Task.CompletedTask;
    })
    .Build();

Console.WriteLine("Posting a plain-text message…");
var posted1 = await client.Messages.Channel(teamId, channelId).SendAsync(
    new MessageBuilder().WithText("Hello from samples.PostMessage 👋").Build());
Console.WriteLine($"  → {posted1.WebUrl ?? posted1.Id}");

Console.WriteLine("Posting an HTML message with a self-mention…");
// You can only mention users you can resolve. The signed-in user is the easiest case.
// If TEAMS_MY_USER_ID is provided we'll mention that user; otherwise we send plain HTML.
OutgoingMessage second;
if (!string.IsNullOrEmpty(myUserId))
{
    var me = new TeamsUser(myUserId, "me", null, null);
    second = new MessageBuilder()
        .WithText("Heads up ")
        .MentionUser(me)
        .Append(" — build #42 passed ✅")
        .Build();
}
else
{
    second = new MessageBuilder()
        .WithHtml("<b>Build #42</b> passed ✅ (set TEAMS_MY_USER_ID to demo mentions)")
        .Build();
}
var posted2 = await client.Messages.Channel(teamId, channelId).SendAsync(second);
Console.WriteLine($"  → {posted2.WebUrl ?? posted2.Id}");

Console.WriteLine("Posting a reply to that message…");
var reply = await client.Messages.Channel(teamId, channelId).ReplyAsync(
    posted2.Id,
    new MessageBuilder().WithText("…and the deploy too.").Build());
Console.WriteLine($"  → reply {reply.Id}");

if (!string.IsNullOrEmpty(myUserId) && !string.IsNullOrEmpty(otherUserId))
{
    Console.WriteLine("Creating a 1:1 chat and sending a message…");
    try
    {
        var chat = await client.Chats.CreateOneOnOneAsync(myUserId, otherUserId);
        var chatMessage = await client.Chats.Messages(chat.Id).SendAsync(
            new MessageBuilder().WithText("hi 👋 sent from the SDK sample").Build());
        Console.WriteLine($"  → chat {chat.Id}, message {chatMessage.Id}");
    }
    catch (TeamsApiException ex)
    {
        Console.Error.WriteLine($"  ! chat send failed: {ex.Message}");
    }
}
else
{
    Console.WriteLine("Skipping 1:1 chat demo (set TEAMS_MY_USER_ID and TEAMS_OTHER_USER_ID to enable).");
}

return 0;

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
