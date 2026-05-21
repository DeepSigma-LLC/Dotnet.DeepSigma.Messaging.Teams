namespace DeepSigma.Messaging.Teams.Models;

public sealed record ChatSummary(
    string Id,
    string? Topic,
    ChatKind Kind,
    DateTimeOffset? LastUpdatedAt);

public enum ChatKind
{
    OneOnOne,
    Group,
    Meeting,
    Unknown,
}
