namespace DeepSigma.Messaging.Teams.Models;

public sealed record TeamsUser(
    string Id,
    string? DisplayName,
    string? UserPrincipalName,
    string? Email);
