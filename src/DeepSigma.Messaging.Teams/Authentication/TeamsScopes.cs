using System.Collections.Immutable;

namespace DeepSigma.Messaging.Teams.Authentication;

public static class TeamsScopes
{
    public static readonly IReadOnlyList<string> AppOnlyDefault = ImmutableArray.Create(
        "https://graph.microsoft.com/.default");

    public static readonly IReadOnlyList<string> DelegatedDefault = ImmutableArray.Create(
        "Team.ReadBasic.All",
        "Channel.ReadBasic.All",
        "ChannelMessage.Read.All",
        "ChannelMessage.Send",
        "Chat.ReadWrite");
}
