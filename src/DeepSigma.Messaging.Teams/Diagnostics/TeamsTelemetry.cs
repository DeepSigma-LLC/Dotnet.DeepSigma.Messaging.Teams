using System.Diagnostics;
using System.Reflection;

namespace DeepSigma.Messaging.Teams.Diagnostics;

public static class TeamsTelemetry
{
    public const string ActivitySourceName = "DeepSigma.Messaging.Teams";

    public static readonly ActivitySource ActivitySource = new(
        ActivitySourceName,
        typeof(TeamsTelemetry).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? typeof(TeamsTelemetry).Assembly.GetName().Version?.ToString()
            ?? "0.0.0");

    public static class Tags
    {
        public const string Operation = "teams.operation";
        public const string TeamId = "teams.team_id";
        public const string ChannelId = "teams.channel_id";
        public const string ChatId = "teams.chat_id";
        public const string MessageId = "teams.message_id";
        public const string UserId = "teams.user_id";
        public const string SubscriptionResource = "teams.subscription.resource";
        public const string HttpStatusCode = "http.response.status_code";
        public const string GraphRequestId = "teams.graph_request_id";
    }
}
