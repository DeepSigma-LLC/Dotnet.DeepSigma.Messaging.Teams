using DeepSigma.Messaging.Teams.Messages;
using DeepSigma.Messaging.Teams.Models;
using Xunit;

namespace DeepSigma.Messaging.Teams.UnitTests;

public class MessageBuilderTests
{
    [Fact]
    public void WithText_BuildsPlainTextMessage()
    {
        var msg = new MessageBuilder().WithText("hello").Build();

        Assert.Equal("hello", msg.Content);
        Assert.Equal(MessageContentType.Text, msg.ContentType);
        Assert.Null(msg.Mentions);
    }

    [Fact]
    public void WithHtml_BuildsHtmlMessage()
    {
        var msg = new MessageBuilder().WithHtml("<b>hi</b>").Build();

        Assert.Equal("<b>hi</b>", msg.Content);
        Assert.Equal(MessageContentType.Html, msg.ContentType);
    }

    [Fact]
    public void MentionUser_AppendsAtTagAndRecordsMention()
    {
        var user = new TeamsUser("user-1", "Ada Lovelace", "ada@example.com", "ada@example.com");

        var msg = new MessageBuilder()
            .WithText("hello ")
            .MentionUser(user)
            .Build();

        Assert.Equal(MessageContentType.Html, msg.ContentType);
        Assert.Contains("<at id=\"0\">Ada Lovelace</at>", msg.Content);
        Assert.NotNull(msg.Mentions);
        var mention = Assert.Single(msg.Mentions);
        Assert.Equal(0, mention.Id);
        Assert.Equal("Ada Lovelace", mention.MentionText);
        var target = Assert.IsType<UserMentionTarget>(mention.Target);
        Assert.Equal("user-1", target.UserId);
    }

    [Fact]
    public void MentionUser_HtmlEncodesDisplayName()
    {
        var user = new TeamsUser("user-1", "O'Reilly & <Sons>", null, null);

        var msg = new MessageBuilder().MentionUser(user).Build();

        Assert.Contains("O&#39;Reilly &amp; &lt;Sons&gt;", msg.Content);
        // Raw display name preserved in the Mention model.
        var mention = Assert.Single(msg.Mentions!);
        Assert.Equal("O'Reilly & <Sons>", mention.MentionText);
    }

    [Fact]
    public void WithText_AfterMentionUser_ClearsMentionsToAvoidOrphans()
    {
        var user = new TeamsUser("user-1", "Ada Lovelace", null, null);

        var msg = new MessageBuilder()
            .MentionUser(user)
            .WithText("oops, starting over")
            .Build();

        Assert.Equal("oops, starting over", msg.Content);
        Assert.Equal(MessageContentType.Text, msg.ContentType);
        Assert.Null(msg.Mentions);
    }

    [Fact]
    public void MultipleMentions_AssignSequentialIds()
    {
        var ada = new TeamsUser("user-1", "Ada", null, null);
        var grace = new TeamsUser("user-2", "Grace", null, null);

        var msg = new MessageBuilder()
            .MentionUser(ada)
            .Append(" and ")
            .MentionUser(grace)
            .Build();

        Assert.Contains("<at id=\"0\">Ada</at>", msg.Content);
        Assert.Contains("<at id=\"1\">Grace</at>", msg.Content);
        Assert.NotNull(msg.Mentions);
        Assert.Equal(2, msg.Mentions.Count);
        Assert.Equal(0, msg.Mentions[0].Id);
        Assert.Equal(1, msg.Mentions[1].Id);
    }
}
