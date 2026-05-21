namespace DeepSigma.Messaging.Teams.Models;

public sealed record Mention(
    int Id,
    string MentionText,
    MentionTarget Target);

public abstract record MentionTarget;

public sealed record UserMentionTarget(string UserId, string? DisplayName) : MentionTarget;

public sealed record ChannelMentionTarget(string ChannelId, string? DisplayName) : MentionTarget;

public sealed record TeamMentionTarget(string TeamId, string? DisplayName) : MentionTarget;
