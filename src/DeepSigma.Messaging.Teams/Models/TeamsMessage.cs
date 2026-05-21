namespace DeepSigma.Messaging.Teams.Models;

public sealed record TeamsMessage(
    string Id,
    string? ReplyToId,
    TeamsUser? From,
    MessageBody Body,
    DateTimeOffset? CreatedAt,
    DateTimeOffset? LastModifiedAt,
    string? WebUrl,
    IReadOnlyList<Mention> Mentions,
    IReadOnlyList<Attachment> Attachments);

public sealed record MessageBody(string Content, MessageContentType ContentType);

public enum MessageContentType
{
    Text,
    Html,
}
