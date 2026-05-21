namespace DeepSigma.Messaging.Teams.Subscriptions;

/// <summary>
/// Licensing/billing model for the Graph change-notification firehose endpoints.
/// See https://learn.microsoft.com/graph/teams-licenses for current rates.
/// </summary>
public enum SubscriptionModel
{
    /// <summary>Model A — pay per notification (best for high signal, low volume).</summary>
    A,
    /// <summary>Model B — pay per active user per month (best for steady high-volume streams).</summary>
    B,
}

public static class SubscriptionResources
{
    /// <summary>
    /// Firehose: every channel message in every team in the tenant. Requires explicit licensing model.
    /// </summary>
    public static string AllChannelMessages(SubscriptionModel model) =>
        $"teams/getAllMessages?model={(model == SubscriptionModel.A ? "A" : "B")}";

    /// <summary>
    /// Firehose: every chat message in the tenant. Requires explicit licensing model.
    /// </summary>
    public static string AllChatMessages(SubscriptionModel model) =>
        $"chats/getAllMessages?model={(model == SubscriptionModel.A ? "A" : "B")}";

    public static string ChannelMessages(string teamId, string channelId) =>
        $"teams/{teamId}/channels/{channelId}/messages";

    public static string ChatMessages(string chatId) =>
        $"chats/{chatId}/messages";
}
