namespace DeepSigma.Messaging.Teams.Subscriptions;

public sealed record ChangeNotification(
    string SubscriptionId,
    string ChangeType,
    string Resource,
    string? TenantId,
    string? ClientState,
    DateTimeOffset? SubscriptionExpiresAt);

public sealed record ChangeNotificationBatch(IReadOnlyList<ChangeNotification> Notifications);
