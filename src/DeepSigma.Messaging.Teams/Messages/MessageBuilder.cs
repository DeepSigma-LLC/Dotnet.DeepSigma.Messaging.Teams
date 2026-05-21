using System.Net;
using System.Text;
using DeepSigma.Messaging.Teams.Models;

namespace DeepSigma.Messaging.Teams.Messages;

/// <summary>
/// Fluent builder for an <see cref="OutgoingMessage"/>. The builder switches to HTML content mode
/// automatically when a mention is added; subsequent <see cref="Append(string)"/> calls in that
/// mode HTML-encode their input. Use <see cref="AppendHtml(string)"/> to insert raw HTML.
/// Not thread-safe — use a single builder per message.
/// </summary>
public sealed class MessageBuilder
{
    private readonly StringBuilder _content = new();
    private MessageContentType _contentType = MessageContentType.Text;
    private readonly List<Mention> _mentions = new();
    private int _nextMentionId;

    /// <summary>Resets the builder to plain-text mode with the given content.</summary>
    public MessageBuilder WithText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        Reset();
        _content.Append(text);
        _contentType = MessageContentType.Text;
        return this;
    }

    /// <summary>Resets the builder to HTML mode with the given raw HTML content.</summary>
    public MessageBuilder WithHtml(string html)
    {
        ArgumentNullException.ThrowIfNull(html);
        Reset();
        _content.Append(html);
        _contentType = MessageContentType.Html;
        return this;
    }

    /// <summary>
    /// Appends user-safe text. In HTML mode the input is HTML-encoded so it always renders as
    /// the literal characters supplied (no markup injection). In Text mode it is appended verbatim.
    /// </summary>
    public MessageBuilder Append(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (_contentType == MessageContentType.Html)
        {
            _content.Append(WebUtility.HtmlEncode(text));
        }
        else
        {
            _content.Append(text);
        }
        return this;
    }

    /// <summary>
    /// Appends raw HTML, switching the builder to HTML mode. The caller is responsible for
    /// ensuring the input is well-formed HTML — no escaping is applied.
    /// </summary>
    public MessageBuilder AppendHtml(string html)
    {
        ArgumentNullException.ThrowIfNull(html);
        _content.Append(html);
        _contentType = MessageContentType.Html;
        return this;
    }

    public MessageBuilder MentionUser(TeamsUser user)
    {
        ArgumentNullException.ThrowIfNull(user);
        var id = _nextMentionId++;
        var rawDisplayName = user.DisplayName ?? user.UserPrincipalName ?? user.Id;
        var encodedDisplayName = WebUtility.HtmlEncode(rawDisplayName);
        _content.Append("<at id=\"").Append(id).Append("\">").Append(encodedDisplayName).Append("</at>");
        _mentions.Add(new Mention(id, rawDisplayName, new UserMentionTarget(user.Id, rawDisplayName)));
        _contentType = MessageContentType.Html;
        return this;
    }

    public OutgoingMessage Build()
    {
        return new OutgoingMessage(
            _content.ToString(),
            _contentType,
            _mentions.Count == 0 ? null : _mentions.ToArray());
    }

    private void Reset()
    {
        _content.Clear();
        _mentions.Clear();
        _nextMentionId = 0;
    }
}
