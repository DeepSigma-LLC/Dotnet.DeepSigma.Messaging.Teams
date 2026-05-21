using DeepSigma.Messaging.Teams.Models;
using GraphModels = Microsoft.Graph.Models;

namespace DeepSigma.Messaging.Teams.Internal;

internal static class GraphMapper
{
    public static TeamSummary ToTeamSummary(GraphModels.Team team)
    {
        return new TeamSummary(
            Id: team.Id ?? string.Empty,
            DisplayName: team.DisplayName ?? string.Empty,
            Description: team.Description,
            InternalId: team.InternalId,
            IsArchived: team.IsArchived ?? false);
    }

    public static ChannelSummary ToChannelSummary(GraphModels.Channel channel, string teamId)
    {
        return new ChannelSummary(
            Id: channel.Id ?? string.Empty,
            TeamId: teamId,
            DisplayName: channel.DisplayName ?? string.Empty,
            Description: channel.Description,
            WebUrl: channel.WebUrl,
            MembershipType: MapMembershipType(channel.MembershipType));
    }

    private static ChannelMembershipType MapMembershipType(GraphModels.ChannelMembershipType? type) => type switch
    {
        GraphModels.ChannelMembershipType.Standard => ChannelMembershipType.Standard,
        GraphModels.ChannelMembershipType.Private => ChannelMembershipType.Private,
        GraphModels.ChannelMembershipType.Shared => ChannelMembershipType.Shared,
        _ => ChannelMembershipType.Unknown,
    };

    public static ChatSummary ToChatSummary(GraphModels.Chat chat)
    {
        return new ChatSummary(
            Id: chat.Id ?? string.Empty,
            Topic: chat.Topic,
            Kind: MapChatKind(chat.ChatType),
            LastUpdatedAt: chat.LastUpdatedDateTime);
    }

    private static ChatKind MapChatKind(GraphModels.ChatType? type) => type switch
    {
        GraphModels.ChatType.OneOnOne => ChatKind.OneOnOne,
        GraphModels.ChatType.Group => ChatKind.Group,
        GraphModels.ChatType.Meeting => ChatKind.Meeting,
        _ => ChatKind.Unknown,
    };

    public static TeamsMessage ToTeamsMessage(GraphModels.ChatMessage message)
    {
        var body = message.Body;
        var bodyContent = new MessageBody(
            Content: body?.Content ?? string.Empty,
            ContentType: body?.ContentType == GraphModels.BodyType.Html ? MessageContentType.Html : MessageContentType.Text);

        var from = message.From?.User is { } user
            ? new TeamsUser(user.Id ?? string.Empty, user.DisplayName, null, null)
            : null;

        var mentions = message.Mentions?.Select(MapMention).ToList() ?? new List<Mention>();
        var attachments = message.Attachments?.Select(MapAttachment).ToList() ?? new List<Attachment>();

        return new TeamsMessage(
            Id: message.Id ?? string.Empty,
            ReplyToId: message.ReplyToId,
            From: from,
            Body: bodyContent,
            CreatedAt: message.CreatedDateTime,
            LastModifiedAt: message.LastModifiedDateTime,
            WebUrl: message.WebUrl,
            Mentions: mentions,
            Attachments: attachments);
    }

    private static Mention MapMention(GraphModels.ChatMessageMention mention)
    {
        MentionTarget target;
        if (mention.Mentioned?.User is { } u)
        {
            target = new UserMentionTarget(u.Id ?? string.Empty, u.DisplayName);
        }
        else if (mention.Mentioned?.Conversation is { } c)
        {
            target = new ChannelMentionTarget(c.Id ?? string.Empty, c.DisplayName);
        }
        else
        {
            target = new TeamMentionTarget(string.Empty, mention.MentionText);
        }

        return new Mention(
            Id: mention.Id ?? 0,
            MentionText: mention.MentionText ?? string.Empty,
            Target: target);
    }

    private static Attachment MapAttachment(GraphModels.ChatMessageAttachment a)
    {
        return new Attachment(
            Id: a.Id ?? string.Empty,
            ContentType: a.ContentType,
            ContentUrl: a.ContentUrl,
            Name: a.Name,
            ThumbnailUrl: a.ThumbnailUrl);
    }
}
