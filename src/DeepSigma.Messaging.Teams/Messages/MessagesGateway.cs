using DeepSigma.Messaging.Teams.Internal;

namespace DeepSigma.Messaging.Teams.Messages;

internal sealed class MessagesGateway : IMessagesGateway
{
    private readonly TeamsClientContext _context;

    public MessagesGateway(TeamsClientContext context)
    {
        _context = context;
    }

    public IChannelMessagesClient Channel(string teamId, string channelId)
        => new ChannelMessagesClient(_context, teamId, channelId);

    public IChatMessagesClient Chat(string chatId)
        => new ChatMessagesClient(_context, chatId);
}
