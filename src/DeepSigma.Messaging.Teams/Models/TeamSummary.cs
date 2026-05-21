namespace DeepSigma.Messaging.Teams.Models;

public sealed record TeamSummary(
    string Id,
    string DisplayName,
    string? Description,
    string? InternalId,
    bool IsArchived);
