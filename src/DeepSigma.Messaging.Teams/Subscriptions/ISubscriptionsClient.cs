namespace DeepSigma.Messaging.Teams.Subscriptions;

public interface ISubscriptionsClient
{
    Task<SubscriptionInfo> SubscribeAsync(
        string resource,
        string callbackUrl,
        string clientState,
        DateTimeOffset expiresAt,
        ChangeTypes changeTypes = ChangeTypes.All,
        CancellationToken cancellationToken = default);

    Task<SubscriptionInfo> RenewAsync(
        string subscriptionId,
        DateTimeOffset newExpiresAt,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(string subscriptionId, CancellationToken cancellationToken = default);
}

public sealed record SubscriptionInfo(
    string Id,
    string Resource,
    string NotificationUrl,
    DateTimeOffset ExpiresAt);
