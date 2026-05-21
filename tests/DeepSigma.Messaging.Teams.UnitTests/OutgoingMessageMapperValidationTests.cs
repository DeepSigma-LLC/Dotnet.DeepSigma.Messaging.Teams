using DeepSigma.Messaging.Teams.Internal;
using DeepSigma.Messaging.Teams.Messages;
using DeepSigma.Messaging.Teams.Models;
using Xunit;

namespace DeepSigma.Messaging.Teams.UnitTests;

public class OutgoingMessageMapperValidationTests
{
    [Fact]
    public void ToGraphChatMessage_MentionsWithTextContentType_Throws()
    {
        var mention = new Mention(0, "Ada", new UserMentionTarget("u-1", "Ada"));
        var msg = new OutgoingMessage("hi @Ada", MessageContentType.Text, new[] { mention });

        var ex = Assert.Throws<ArgumentException>(() => OutgoingMessageMapper.ToGraphChatMessage(msg));
        Assert.Contains("Html", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ToGraphChatMessage_DuplicateMentionIds_Throws()
    {
        var mentions = new[]
        {
            new Mention(0, "Ada", new UserMentionTarget("u-1", "Ada")),
            new Mention(0, "Grace", new UserMentionTarget("u-2", "Grace")),
        };
        var msg = new OutgoingMessage("<at id=\"0\">Ada</at> <at id=\"0\">Grace</at>", MessageContentType.Html, mentions);

        var ex = Assert.Throws<ArgumentException>(() => OutgoingMessageMapper.ToGraphChatMessage(msg));
        Assert.Contains("duplicate", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ToGraphChatMessage_MentionIdMissingFromBody_Throws()
    {
        var mention = new Mention(7, "Ada", new UserMentionTarget("u-1", "Ada"));
        var msg = new OutgoingMessage("hello there", MessageContentType.Html, new[] { mention });

        var ex = Assert.Throws<ArgumentException>(() => OutgoingMessageMapper.ToGraphChatMessage(msg));
        Assert.Contains("7", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ToGraphChatMessage_ValidMentions_Succeeds()
    {
        var mention = new Mention(0, "Ada", new UserMentionTarget("u-1", "Ada"));
        var msg = new OutgoingMessage("<at id=\"0\">Ada</at> hello", MessageContentType.Html, new[] { mention });

        var graph = OutgoingMessageMapper.ToGraphChatMessage(msg);

        Assert.NotNull(graph.Mentions);
        Assert.Single(graph.Mentions!);
    }

    [Fact]
    public void ToGraphChatMessage_NullMessage_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => OutgoingMessageMapper.ToGraphChatMessage(null!));
    }
}
