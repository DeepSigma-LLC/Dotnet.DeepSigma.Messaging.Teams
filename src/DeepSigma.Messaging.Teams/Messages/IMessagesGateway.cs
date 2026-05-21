namespace DeepSigma.Messaging.Teams.Messages;

public interface IMessagesGateway
{
    IChannelMessagesClient Channel(string teamId, string channelId);

    IChatMessagesClient Chat(string chatId);
}
