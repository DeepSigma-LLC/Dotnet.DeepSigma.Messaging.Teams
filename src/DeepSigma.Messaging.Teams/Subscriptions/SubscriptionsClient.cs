using System.Diagnostics;
using DeepSigma.Messaging.Teams.Diagnostics;
using DeepSigma.Messaging.Teams.Internal;
using Microsoft.Graph.Models;

namespace DeepSigma.Messaging.Teams.Subscriptions;

internal sealed class SubscriptionsClient : ISubscriptionsClient
{
    private readonly TeamsClientContext _context;

    public SubscriptionsClient(TeamsClientContext context)
    {
        _context = context;
    }

    public async Task<SubscriptionInfo> SubscribeAsync(
        string resource,
        string callbackUrl,
        string clientState,
        DateTimeOffset expiresAt,
        ChangeTypes changeTypes = ChangeTypes.All,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resource);
        ArgumentException.ThrowIfNullOrWhiteSpace(callbackUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientState);

        using var activity = TeamsTelemetry.ActivitySource.StartActivity("teams.subscriptions.create", ActivityKind.Client);
        activity?.SetTag(TeamsTelemetry.Tags.SubscriptionResource, resource);

        var sub = new Subscription
        {
            ChangeType = changeTypes.ToGraphString(),
            NotificationUrl = callbackUrl,
            Resource = resource,
            ExpirationDateTime = expiresAt,
            ClientState = clientState,
        };

        var created = await GraphErrorMapper.ExecuteAsync(() =>
            _context.GraphClient.Subscriptions.PostAsync(sub, cancellationToken: cancellationToken)).ConfigureAwait(false);
        if (created is null)
        {
            throw new Exceptions.TeamsApiException("Subscription creation returned a null response.");
        }
        return Map(created);
    }

    public async Task<SubscriptionInfo> RenewAsync(string subscriptionId, DateTimeOffset newExpiresAt, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subscriptionId);
        using var activity = TeamsTelemetry.ActivitySource.StartActivity("teams.subscriptions.renew", ActivityKind.Client);

        var update = new Subscription { ExpirationDateTime = newExpiresAt };
        var renewed = await GraphErrorMapper.ExecuteAsync(() =>
            _context.GraphClient.Subscriptions[subscriptionId].PatchAsync(update, cancellationToken: cancellationToken))
            .ConfigureAwait(false);
        if (renewed is null)
        {
            throw new Exceptions.TeamsApiException("Subscription renewal returned a null response.");
        }
        return Map(renewed);
    }

    public async Task DeleteAsync(string subscriptionId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subscriptionId);
        using var activity = TeamsTelemetry.ActivitySource.StartActivity("teams.subscriptions.delete", ActivityKind.Client);

        await GraphErrorMapper.ExecuteAsync(() =>
            _context.GraphClient.Subscriptions[subscriptionId].DeleteAsync(cancellationToken: cancellationToken))
            .ConfigureAwait(false);
    }

    private static SubscriptionInfo Map(Subscription sub) => new(
        Id: sub.Id ?? string.Empty,
        Resource: sub.Resource ?? string.Empty,
        NotificationUrl: sub.NotificationUrl ?? string.Empty,
        ExpiresAt: sub.ExpirationDateTime ?? DateTimeOffset.MinValue);
}
