using DeepSigma.Messaging.Teams.Internal;
using DeepSigma.Messaging.Teams.Messages;
using DeepSigma.Messaging.Teams.Models;
using GraphModels = Microsoft.Graph.Models;
using Xunit;

namespace DeepSigma.Messaging.Teams.UnitTests;

public class OutgoingMessageMapperTests
{
    [Fact]
    public void ToGraphChatMessage_PlainText_SetsTextContentType()
    {
        var msg = new OutgoingMessage("hello", MessageContentType.Text);

        var graph = OutgoingMessageMapper.ToGraphChatMessage(msg);

        Assert.NotNull(graph.Body);
        Assert.Equal("hello", graph.Body!.Content);
        Assert.Equal(GraphModels.BodyType.Text, graph.Body.ContentType);
        Assert.Null(graph.Mentions);
    }

    [Fact]
    public void ToGraphChatMessage_Html_SetsHtmlContentType()
    {
        var msg = new OutgoingMessage("<b>hi</b>", MessageContentType.Html);

        var graph = OutgoingMessageMapper.ToGraphChatMessage(msg);

        Assert.Equal(GraphModels.BodyType.Html, graph.Body!.ContentType);
    }

    [Fact]
    public void ToGraphChatMessage_UserMention_EmitsAadUserIdentity()
    {
        var mention = new Mention(0, "Ada Lovelace", new UserMentionTarget("user-1", "Ada Lovelace"));
        var msg = new OutgoingMessage("hi <at id=\"0\">Ada Lovelace</at>", MessageContentType.Html, new[] { mention });

        var graph = OutgoingMessageMapper.ToGraphChatMessage(msg);

        Assert.NotNull(graph.Mentions);
        var graphMention = Assert.Single(graph.Mentions!);
        Assert.Equal(0, graphMention.Id);
        Assert.Equal("Ada Lovelace", graphMention.MentionText);

        Assert.NotNull(graphMention.Mentioned);
        Assert.NotNull(graphMention.Mentioned!.User);
        Assert.Equal("user-1", graphMention.Mentioned.User!.Id);
        Assert.Equal("Ada Lovelace", graphMention.Mentioned.User.DisplayName);
        Assert.True(graphMention.Mentioned.User.AdditionalData.ContainsKey("userIdentityType"));
        Assert.Equal("aadUser", graphMention.Mentioned.User.AdditionalData["userIdentityType"]);
    }

    [Fact]
    public void ToGraphChatMessage_ChannelMention_EmitsChannelConversationIdentity()
    {
        var mention = new Mention(0, "General", new ChannelMentionTarget("channel-1", "General"));
        var msg = new OutgoingMessage("hi <at id=\"0\">General</at>", MessageContentType.Html, new[] { mention });

        var graph = OutgoingMessageMapper.ToGraphChatMessage(msg);

        var graphMention = Assert.Single(graph.Mentions!);
        Assert.NotNull(graphMention.Mentioned!.Conversation);
        Assert.Equal("channel-1", graphMention.Mentioned.Conversation!.Id);
        Assert.Equal(GraphModels.TeamworkConversationIdentityType.Channel, graphMention.Mentioned.Conversation.ConversationIdentityType);
    }

    [Fact]
    public void ToGraphChatMessage_TeamMention_EmitsTeamConversationIdentity()
    {
        var mention = new Mention(0, "Engineering", new TeamMentionTarget("team-1", "Engineering"));
        var msg = new OutgoingMessage("ping <at id=\"0\">Engineering</at>", MessageContentType.Html, new[] { mention });

        var graph = OutgoingMessageMapper.ToGraphChatMessage(msg);

        var graphMention = Assert.Single(graph.Mentions!);
        Assert.NotNull(graphMention.Mentioned!.Conversation);
        Assert.Equal("team-1", graphMention.Mentioned.Conversation!.Id);
        Assert.Equal(GraphModels.TeamworkConversationIdentityType.Team, graphMention.Mentioned.Conversation.ConversationIdentityType);
    }
}
