using System.Text.Json;
using DeepSigma.Messaging.Teams.Subscriptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace DeepSigma.Messaging.Teams.AspNetCore;

public static partial class TeamsWebhookEndpoint
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Rejecting Teams webhook notification with unexpected clientState.")]
    private static partial void LogUnexpectedClientState(ILogger logger);


    public static IEndpointRouteBuilder MapTeamsWebhook(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        Func<HttpContext, ChangeNotificationBatch, Task> handler,
        string? expectedClientState = null)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        endpoints.MapPost(pattern, async (HttpContext context, ILoggerFactory? loggerFactory) =>
        {
            var logger = loggerFactory?.CreateLogger("DeepSigma.Messaging.Teams.AspNetCore.TeamsWebhook");

            if (context.Request.Query.TryGetValue("validationToken", out var token))
            {
                context.Response.ContentType = "text/plain";
                await context.Response.WriteAsync(token.ToString()).ConfigureAwait(false);
                return;
            }

            var documentOptions = new JsonDocumentOptions { MaxDepth = 16 };
            using var doc = await JsonDocument
                .ParseAsync(context.Request.Body, documentOptions, context.RequestAborted)
                .ConfigureAwait(false);

            if (!doc.RootElement.TryGetProperty("value", out var valueElement) || valueElement.ValueKind != JsonValueKind.Array)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            var notifications = new List<ChangeNotification>();
            foreach (var n in valueElement.EnumerateArray())
            {
                var subId = n.GetPropertyOrDefault("subscriptionId");
                var changeType = n.GetPropertyOrDefault("changeType");
                var resource = n.GetPropertyOrDefault("resource");
                var tenantId = n.GetPropertyOrDefault("tenantId");
                var clientState = n.GetPropertyOrDefault("clientState");
                DateTimeOffset? expiration = null;
                if (n.TryGetProperty("subscriptionExpirationDateTime", out var exp)
                    && exp.ValueKind == JsonValueKind.String
                    && DateTimeOffset.TryParse(exp.GetString(), System.Globalization.CultureInfo.InvariantCulture, out var parsed))
                {
                    expiration = parsed;
                }

                if (expectedClientState is not null && clientState != expectedClientState)
                {
                    if (logger is not null)
                    {
                        LogUnexpectedClientState(logger);
                    }
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;
                }

                notifications.Add(new ChangeNotification(
                    SubscriptionId: subId ?? string.Empty,
                    ChangeType: changeType ?? string.Empty,
                    Resource: resource ?? string.Empty,
                    TenantId: tenantId,
                    ClientState: clientState,
                    SubscriptionExpiresAt: expiration));
            }

            await handler(context, new ChangeNotificationBatch(notifications)).ConfigureAwait(false);
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = StatusCodes.Status202Accepted;
            }
        });

        return endpoints;
    }

    private static string? GetPropertyOrDefault(this JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }
}
