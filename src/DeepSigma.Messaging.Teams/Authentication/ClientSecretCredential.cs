using Azure.Core;
using AzureClientSecretCredential = Azure.Identity.ClientSecretCredential;

namespace DeepSigma.Messaging.Teams.Authentication;

public sealed class ClientSecretCredential : ITeamsCredential
{
    private readonly AzureClientSecretCredential _credential;

    public ClientSecretCredential(string tenantId, string clientId, string clientSecret)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientSecret);
        _credential = new AzureClientSecretCredential(tenantId, clientId, clientSecret);
    }

    public IReadOnlyList<string> Scopes => TeamsScopes.AppOnlyDefault;

    public TokenCredential ToTokenCredential() => _credential;
}
