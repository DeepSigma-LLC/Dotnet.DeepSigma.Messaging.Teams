using DeepSigma.Messaging.Teams.Models;

namespace DeepSigma.Messaging.Teams.Messages;

public sealed record OutgoingMessage(
    string Content,
    MessageContentType ContentType,
    IReadOnlyList<Mention>? Mentions = null);
