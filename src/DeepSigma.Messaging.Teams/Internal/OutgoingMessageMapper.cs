using System.Text.RegularExpressions;
using DeepSigma.Messaging.Teams.Messages;
using DeepSigma.Messaging.Teams.Models;
using GraphModels = Microsoft.Graph.Models;

namespace DeepSigma.Messaging.Teams.Internal;

internal static partial class OutgoingMessageMapper
{
    [GeneratedRegex("<at\\s+id=\"(?<id>-?\\d+)\"", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 200)]
    private static partial Regex AtTagIdRegex();

    public static GraphModels.ChatMessage ToGraphChatMessage(OutgoingMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        ValidateMentions(message);

        var graph = new GraphModels.ChatMessage
        {
            Body = new GraphModels.ItemBody
            {
                Content = message.Content,
                ContentType = message.ContentType == MessageContentType.Html
                    ? GraphModels.BodyType.Html
                    : GraphModels.BodyType.Text,
            },
        };

        if (message.Mentions is { Count: > 0 } mentions)
        {
            graph.Mentions = mentions.Select(ToGraphMention).ToList();
        }

        return graph;
    }

    /// <summary>
    /// Fail fast before the Graph call if mention metadata is inconsistent with the message body.
    /// Graph will otherwise return a generic 400 that's hard to diagnose.
    /// </summary>
    private static void ValidateMentions(OutgoingMessage message)
    {
        if (message.Mentions is not { Count: > 0 } mentions)
        {
            return;
        }

        if (message.ContentType != MessageContentType.Html)
        {
            throw new ArgumentException(
                "OutgoingMessage with mentions must use Html content type — mentions reference <at> tags in the body.",
                nameof(message));
        }

        // Duplicate-id check
        var seen = new HashSet<int>();
        foreach (var mention in mentions)
        {
            if (!seen.Add(mention.Id))
            {
                throw new ArgumentException(
                    $"OutgoingMessage contains duplicate mention id {mention.Id}. Each mention must have a unique id.",
                    nameof(message));
            }
        }

        // Each mention id must appear at least once as <at id="N"> in the body.
        var idsInBody = new HashSet<int>();
        foreach (Match match in AtTagIdRegex().Matches(message.Content))
        {
            if (int.TryParse(match.Groups["id"].ValueSpan, out var id))
            {
                idsInBody.Add(id);
            }
        }

        foreach (var mention in mentions)
        {
            if (!idsInBody.Contains(mention.Id))
            {
                throw new ArgumentException(
                    $"OutgoingMessage mention id {mention.Id} has no matching <at id=\"{mention.Id}\"> tag in the body.",
                    nameof(message));
            }
        }
    }

    private static GraphModels.ChatMessageMention ToGraphMention(Mention m)
    {
        var mentioned = new GraphModels.ChatMessageMentionedIdentitySet();
        switch (m.Target)
        {
            case UserMentionTarget user:
                var userIdentity = new GraphModels.Identity
                {
                    Id = user.UserId,
                    DisplayName = user.DisplayName,
                };
                userIdentity.AdditionalData["userIdentityType"] = "aadUser";
                mentioned.User = userIdentity;
                break;
            case ChannelMentionTarget channel:
                mentioned.Conversation = new GraphModels.TeamworkConversationIdentity
                {
                    Id = channel.ChannelId,
                    DisplayName = channel.DisplayName,
                    ConversationIdentityType = GraphModels.TeamworkConversationIdentityType.Channel,
                };
                break;
            case TeamMentionTarget team:
                mentioned.Conversation = new GraphModels.TeamworkConversationIdentity
                {
                    Id = team.TeamId,
                    DisplayName = team.DisplayName,
                    ConversationIdentityType = GraphModels.TeamworkConversationIdentityType.Team,
                };
                break;
        }

        return new GraphModels.ChatMessageMention
        {
            Id = m.Id,
            MentionText = m.MentionText,
            Mentioned = mentioned,
        };
    }
}
