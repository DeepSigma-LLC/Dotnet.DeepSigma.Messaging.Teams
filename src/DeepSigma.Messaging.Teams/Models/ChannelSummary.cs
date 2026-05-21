namespace DeepSigma.Messaging.Teams.Models;

public sealed record ChannelSummary(
    string Id,
    string TeamId,
    string DisplayName,
    string? Description,
    string? WebUrl,
    ChannelMembershipType MembershipType);

public enum ChannelMembershipType
{
    Standard,
    Private,
    Shared,
    Unknown,
}
