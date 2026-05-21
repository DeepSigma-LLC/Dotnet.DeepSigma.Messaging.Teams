using System.Security.Cryptography.X509Certificates;
using Azure.Core;
using AzureClientCertificateCredential = Azure.Identity.ClientCertificateCredential;

namespace DeepSigma.Messaging.Teams.Authentication;

public sealed class ClientCertificateCredential : ITeamsCredential
{
    private readonly AzureClientCertificateCredential _credential;

    public ClientCertificateCredential(string tenantId, string clientId, X509Certificate2 certificate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentNullException.ThrowIfNull(certificate);
        _credential = new AzureClientCertificateCredential(tenantId, clientId, certificate);
    }

    public IReadOnlyList<string> Scopes => TeamsScopes.AppOnlyDefault;

    public TokenCredential ToTokenCredential() => _credential;
}
