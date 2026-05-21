# DeepSigma.Messaging.Teams

A general-purpose .NET 10 library for interacting with Microsoft Teams. Query teams, channels, and chats; read and post messages; subscribe to change notifications. Built on top of [Microsoft Graph](https://learn.microsoft.com/graph/) so consumers get the full Teams surface area without writing Graph plumbing.

This library is **not chatbot-specific** — it provides the primitives that a chatbot (or any other Teams integration) needs.

> **Status:** pre-release scaffolding. The public API surface is in place and the solution builds and tests green, but it has not yet been smoke-tested against a real tenant. Treat the API as unstable until v1.0.

---

## Packages

| Package                                    | Description |
|---|---|
| `DeepSigma.Messaging.Teams`                | Core: client, auth, directory, messages, chats, subscriptions, telemetry. |
| `DeepSigma.Messaging.Teams.AspNetCore`     | ASP.NET Core minimal-API endpoint for receiving Graph change notifications. |

Author / Owner: **DeepSigma LLC**. License: see [LICENSE](LICENSE).

---

## Prerequisites — what you need before any code will work

To call the Microsoft Graph Teams APIs you need **three things**:

1. A **Microsoft 365 tenant** (an Entra ID / Azure AD directory) that has Teams enabled.
2. An **app registration** inside that tenant (gives you `tenantId`, `clientId`, plus a `clientSecret` or certificate).
3. The right **Microsoft Graph permissions** granted to that app, with admin consent.

You **cannot** use a personal Microsoft account (outlook.com, hotmail.com, etc.) for Teams Graph APIs. You need a work/school account that lives in an Entra ID tenant.

### Getting a tenant when you don't already have one

| Option | Cost | Notes |
|---|---|---|
| **Use your employer's tenant** (sandbox app reg) | free if your IT admin agrees | Easiest if available. Ask for a dedicated app registration scoped to a test team. |
| **[Microsoft 365 Developer Program](https://developer.microsoft.com/en-us/microsoft-365/dev-program)** — E5 sandbox | free, but **requires a qualifying subscription** | As of 2025, Microsoft restricted eligibility to **Visual Studio Professional / Enterprise *standard* subscribers** and members of qualifying Microsoft partner programs. *Monthly* VS subscriptions and free personal accounts no longer qualify. |
| **Microsoft 365 Business Basic** (or higher) | ~$6 / user / month after 30-day trial | Gets you a tenant, an admin account, and Teams. Cheapest paid path if you don't have a VS subscription. |
| **Visual Studio Subscription perk** | included with active standard VS Pro/Enterprise subscription | Activate your M365 E5 dev subscription via your Visual Studio benefits portal — it renews for the life of the subscription. |
| **Azure free tier only** | free | Gives you a directory (Entra ID), but **no Teams license** — you can register the app and get tokens but you won't be able to post or read any actual Teams content. Useful only for testing auth wiring. |

In short: if you don't have a Visual Studio standard subscription and you don't have access to a corporate tenant, expect to pay for at least one Microsoft 365 Business Basic seat.

### Creating the app registration (once you have a tenant)

In the Microsoft Entra admin center → **App registrations** → **New registration**:

1. Give it a name (e.g. `DeepSigma Teams Bot`).
2. **Supported account types**: *Accounts in this organizational directory only* (single tenant).
3. Click Register. Note the **Application (client) ID** and **Directory (tenant) ID**.
4. **Certificates & secrets → New client secret** → copy the secret *value* immediately (you can't see it again).
5. **API permissions → Add a permission → Microsoft Graph** → add the permissions appropriate to your scenario (see the table below).
6. Click **Grant admin consent for {tenant}**. Without admin consent your app will get `403 Forbidden`.

For step-by-step screenshots, follow Microsoft's tutorial: [Register an application with the Microsoft identity platform](https://learn.microsoft.com/entra/identity-platform/quickstart-register-app).

### Microsoft Graph permissions (and the big gotcha)

Pick the row that matches the operation. *App* = application/client-credentials, *Delegated* = signed-in user.

| Operation | App permission | Delegated permission | Notes |
|---|---|---|---|
| List teams | `Team.ReadBasic.All` | `Team.ReadBasic.All` | |
| List channels | `Channel.ReadBasic.All` | `Channel.ReadBasic.All` | |
| List channel members | `TeamMember.Read.All` | `TeamMember.Read.All` | |
| **List channel messages** | `ChannelMessage.Read.All` *(admin consent)* or `ChannelMessage.Read.Group` *(RSC)* | `ChannelMessage.Read.All` | |
| **Post a channel message** | **`Teamwork.Migrate.All`** — *only* for [migration](https://learn.microsoft.com/microsoftteams/platform/graph-api/import-messages/import-external-messages-to-teams), not regular posting | `ChannelMessage.Send` | **🚨 See below.** |
| List a user's chats | `Chat.ReadBasic.All` | `Chat.ReadBasic` | |
| Read / post chat messages | `Chat.Read.All` / `Chat.ReadWrite.All` | `Chat.Read` / `Chat.ReadWrite` | |
| Create change-notification subscription | `Subscription.Read.All` (and the resource permission) | same | |

**🚨 The app-only posting limitation.** Microsoft's documented stance is that *application permissions for posting channel messages are supported only for [Teams migration](https://learn.microsoft.com/graph/api/chatmessage-post#permissions) (`Teamwork.Migrate.All`)*. There is no regular app-only path to post a normal channel message. If you want a "service account bot" that posts on its own, you have two real choices:

- **Delegated auth** (the library supports this via `WithDeviceCode` or any `TokenCredential` like `InteractiveBrowserCredential`) — the message will be attributed to a specific user.
- **An Azure Bot Service / Teams app** with [Resource-Specific Consent](https://learn.microsoft.com/microsoftteams/platform/graph-api/rsc/resource-specific-consent) installed in the team — out of scope for this library, but compatible (you'd still use this library to read data).

For *reading* messages and for everything else (listing, subscriptions), app-only works fine with the permissions in the table.

The library doesn't try to hide this — it will surface the underlying Graph 403 as a `TeamsAuthenticationException` so you see the real reason.

---

## Installation

NuGet packages will be published as part of v1.0. For now, reference the project directly:

```xml
<ItemGroup>
  <ProjectReference Include="path/to/src/DeepSigma.Messaging.Teams/DeepSigma.Messaging.Teams.csproj" />
</ItemGroup>
```

Targets **`net10.0`**.

---

## Getting started

### 1. Construct a client

```csharp
using DeepSigma.Messaging.Teams;

var teams = new TeamsClientBuilder()
    .WithClientSecret(
        tenantId:    "00000000-0000-0000-0000-000000000000",
        clientId:    "11111111-1111-1111-1111-111111111111",
        clientSecret:"<your secret>")
    .Build();
```

Other ways to authenticate:

```csharp
// Certificate-based (recommended for production over client secrets)
.WithClientCertificate(tenantId, clientId, X509Certificate2.CreateFromPemFile("app.pem"))

// Device code (delegated — interactive, browser-based; great for CLIs)
.WithDeviceCode(tenantId, clientId)

// Bring-your-own Azure.Core TokenCredential
// (e.g. ManagedIdentityCredential, WorkloadIdentityCredential,
//  InteractiveBrowserCredential, DefaultAzureCredential)
.WithTokenCredential(new ManagedIdentityCredential())
```

### 2. List teams and channels

```csharp
await foreach (var team in teams.Directory.ListTeamsAsync())
{
    Console.WriteLine($"{team.Id}  {team.DisplayName}");

    await foreach (var channel in teams.Directory.ListChannelsAsync(team.Id))
    {
        Console.WriteLine($"   ↳ {channel.DisplayName} ({channel.MembershipType})");
    }
}
```

All `List*Async` methods return `IAsyncEnumerable<T>` and transparently follow `@odata.nextLink`.

### 3. Read messages from a channel

```csharp
using DeepSigma.Messaging.Teams.Querying;

var messages = teams.Messages
    .Channel(teamId, channelId)
    .ListAsync(new QueryOptions { Top = 25, MaxPages = 2 });

await foreach (var msg in messages)
{
    Console.WriteLine($"[{msg.CreatedAt:u}] {msg.From?.DisplayName}: {msg.Body.Content}");
}
```

### 4. Post a message (delegated auth — see the gotcha above)

```csharp
using DeepSigma.Messaging.Teams.Messages;

var outgoing = new MessageBuilder()
    .WithText("Build #42 passed ✅")
    .Build();

await teams.Messages.Channel(teamId, channelId).SendAsync(outgoing);
```

Reply to a thread:

```csharp
await teams.Messages
    .Channel(teamId, channelId)
    .ReplyAsync(parentMessageId, new MessageBuilder().WithText("got it").Build());
```

Mention a user (renders an `<at>` tag and attaches a Mention with the right user id):

```csharp
var user = new TeamsUser(userId, "Ada Lovelace", "ada@example.com", "ada@example.com");

var msg = new MessageBuilder()
    .WithText("Hey ")
    .MentionUser(user)
    .Append(" — heads up.")
    .Build();

await teams.Messages.Channel(teamId, channelId).SendAsync(msg);
```

### 5. 1:1 and group chats

```csharp
// Create a 1:1 chat between two users (both must exist in your tenant), then send a message.
// Pass the caller's user id explicitly — no "me" magic — so the same call works under app-only auth.
var chat = await teams.Chats.CreateOneOnOneAsync(myUserId, otherUserId);
await teams.Chats.Messages(chat.Id).SendAsync(new MessageBuilder().WithText("hi 👋").Build());

// Or send into an existing chat by id
await teams.Messages.Chat(chatId).SendAsync(new MessageBuilder().WithText("hi 👋").Build());

// List the signed-in user's chats
await foreach (var c in teams.Chats.ListMyChatsAsync())
{
    Console.WriteLine($"{c.Id}  {c.Topic ?? "(no topic)"}  [{c.Kind}]");
}
```

### 6. Reacting to messages in real time (chatbot pattern)

```csharp
using DeepSigma.Messaging.Teams.Subscriptions;

// Subscribe — Microsoft Graph will POST notifications to your callbackUrl.
// AllChannelMessages requires an explicit licensing model (see Microsoft's Teams API licensing docs).
var sub = await teams.Subscriptions.SubscribeAsync(
    resource:      SubscriptionResources.AllChannelMessages(SubscriptionModel.B),
    callbackUrl:   "https://my.bot.example.com/teams/notify",
    clientState:   "shared-secret-only-i-know",
    expiresAt:     DateTimeOffset.UtcNow.AddMinutes(60),  // max 60 min for messages
    changeTypes:   ChangeTypes.Created);                  // bots typically only care about new messages

// You must renew before ExpiresAt or the subscription dies
await teams.Subscriptions.RenewAsync(sub.Id, DateTimeOffset.UtcNow.AddMinutes(60));

// And clean up when you're done
await teams.Subscriptions.DeleteAsync(sub.Id);
```

Then in your ASP.NET Core app, install `DeepSigma.Messaging.Teams.AspNetCore` and wire up the receiver:

```csharp
using DeepSigma.Messaging.Teams.AspNetCore;

var app = WebApplication.Create();

app.MapTeamsWebhook("/teams/notify", async (ctx, batch) =>
{
    foreach (var n in batch.Notifications)
    {
        // n.Resource looks like "teams('{teamId}')/channels('{channelId}')/messages('{messageId}')"
        // Fetch the full message via the library and react.
        Console.WriteLine($"{n.ChangeType} on {n.Resource}");
    }
}, expectedClientState: "shared-secret-only-i-know");

app.Run();
```

The endpoint handles Graph's `validationToken` handshake automatically and rejects notifications whose `clientState` doesn't match.

---

## Dependency injection

```csharp
using DeepSigma.Messaging.Teams.DependencyInjection;

builder.Services.AddTeamsClient(b => b
    .WithClientSecret(tenantId, clientId, clientSecret));

// or pre-built credential
builder.Services.AddTeamsClient(new ClientSecretCredential(tenantId, clientId, clientSecret));
```

`ITeamsClient` is then injectable anywhere. An `ILoggerFactory` resolved from DI is wired through automatically so all internal logs flow into your existing logging pipeline.

---

## Logging and OpenTelemetry

The library uses **`ILogger<T>`** via constructor injection — no special hook to enable.

It also emits **`Activity` spans** under the ActivitySource named `DeepSigma.Messaging.Teams`. Subscribe to it from your OpenTelemetry tracer provider:

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(t => t.AddSource("DeepSigma.Messaging.Teams"));
```

Common tags on each span:

| Tag | Description |
|---|---|
| `teams.operation` | Short operation name, e.g. `list_teams`, `send` |
| `teams.team_id` / `teams.channel_id` / `teams.chat_id` / `teams.message_id` / `teams.user_id` | Subject ids when applicable |
| `teams.graph_request_id` | Graph's `request-id` response header (when present) |

---

## Error handling

All Graph failures are translated to typed exceptions:

| Exception | When |
|---|---|
| `TeamsAuthenticationException` | 401 / 403 — bad credentials, missing consent, or app-only attempting an op that requires delegated auth (see the posting gotcha). |
| `TeamsThrottledException` | 429 — exposes `RetryAfter`. The library does **not** auto-retry on 429 today; the Graph SDK does some internal retries but you should still handle this in your code. |
| `TeamsApiException` | Everything else. Carries `StatusCode`, `RequestId`, and the underlying Kiota `ApiException` as the inner exception. |

---

## What's in v1, what's not

| In v1 | Not in v1 |
|---|---|
| List teams / channels / members | Adaptive Cards |
| List & post channel messages | Online meetings |
| 1:1 & group chats (read + post + create) | Calls |
| Subscriptions (create / renew / delete) | Files / SharePoint resolution |
| Webhook receiver (ASP.NET Core companion package) | Presence |
| Mentions in `MessageBuilder` | Delta queries |
| OTel `ActivitySource`, `ILogger<T>` via DI |  |

Adaptive Cards, meetings, and calls are **explicitly out of scope** — use the Graph SDK directly if you need them.

---

## Running the sample

```powershell
$env:TEAMS_TENANT_ID    = "<your-tenant-id>"
$env:TEAMS_CLIENT_ID    = "<your-app-client-id>"
$env:TEAMS_CLIENT_SECRET= "<your-app-client-secret>"
$env:TEAMS_TEAM_ID      = "<a-team-id>"      # optional
$env:TEAMS_CHANNEL_ID   = "<a-channel-id>"   # optional, requires ChannelMessage.Read.All
dotnet run --project samples/samples.ConsoleQuery
```

---

## Useful references

- [Microsoft Graph permissions reference](https://learn.microsoft.com/graph/permissions-reference)
- [Permission types: delegated vs. application](https://learn.microsoft.com/graph/permissions-overview#permission-types)
- [Resource-specific consent for Teams](https://learn.microsoft.com/microsoftteams/platform/graph-api/rsc/resource-specific-consent)
- [Get change notifications for messages](https://learn.microsoft.com/graph/teams-changenotifications-chatmessage)
- [Register an app with the Microsoft identity platform](https://learn.microsoft.com/entra/identity-platform/quickstart-register-app)
- [Microsoft 365 Developer Program FAQ](https://learn.microsoft.com/office/developer-program/microsoft-365-developer-program-faq)

---

## License

See [LICENSE](LICENSE).
