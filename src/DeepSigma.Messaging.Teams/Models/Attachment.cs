namespace DeepSigma.Messaging.Teams.Models;

public sealed record Attachment(
    string Id,
    string? ContentType,
    string? ContentUrl,
    string? Name,
    string? ThumbnailUrl);
