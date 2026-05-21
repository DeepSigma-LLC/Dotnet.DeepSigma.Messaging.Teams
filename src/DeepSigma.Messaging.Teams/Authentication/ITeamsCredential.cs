using Azure.Core;

namespace DeepSigma.Messaging.Teams.Authentication;

public interface ITeamsCredential
{
    TokenCredential ToTokenCredential();

    IReadOnlyList<string> Scopes { get; }
}
