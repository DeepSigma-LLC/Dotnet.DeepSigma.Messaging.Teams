using System.Security.Cryptography.X509Certificates;
using Azure.Core;
using DeepSigma.Messaging.Teams.Authentication;
using Microsoft.Extensions.Logging;

namespace DeepSigma.Messaging.Teams;

public sealed class TeamsClientBuilder
{
    private ITeamsCredential? _credential;
    private TeamsClientOptions _options = new();
    private ILoggerFactory? _loggerFactory;

    public TeamsClientBuilder WithClientSecret(string tenantId, string clientId, string clientSecret)
    {
        _credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
        return this;
    }

    public TeamsClientBuilder WithClientCertificate(string tenantId, string clientId, X509Certificate2 certificate)
    {
        _credential = new ClientCertificateCredential(tenantId, clientId, certificate);
        return this;
    }

    /// <summary>
    /// Interactive device-code flow. You must supply <paramref name="deviceCodeCallback"/>
    /// to display the verification message to the user (e.g. write to console, show a dialog).
    /// </summary>
    public TeamsClientBuilder WithDeviceCode(
        string tenantId,
        string clientId,
        Func<Azure.Identity.DeviceCodeInfo, CancellationToken, Task> deviceCodeCallback,
        IReadOnlyList<string>? scopes = null)
    {
        _credential = new DeviceCodeCredential(tenantId, clientId, deviceCodeCallback, scopes);
        return this;
    }

    /// <summary>
    /// Wrap any <see cref="Azure.Core.TokenCredential"/> (managed identity, workload identity,
    /// interactive browser, etc.). You must supply scopes explicitly — pass
    /// <see cref="TeamsScopes.AppOnlyDefault"/> for service scenarios or a list of granular
    /// delegated scopes for user scenarios.
    /// </summary>
    public TeamsClientBuilder WithTokenCredential(TokenCredential credential, IReadOnlyList<string> scopes)
    {
        _credential = new TokenCredentialAdapter(credential, scopes);
        return this;
    }

    public TeamsClientBuilder WithCredential(ITeamsCredential credential)
    {
        _credential = credential;
        return this;
    }

    public TeamsClientBuilder WithOptions(Action<TeamsClientOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        configure(_options);
        return this;
    }

    public TeamsClientBuilder WithLoggerFactory(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
        return this;
    }

    public ITeamsClient Build()
    {
        if (_credential is null)
        {
            throw new InvalidOperationException("A credential must be configured. Call one of the With*Credential methods first.");
        }
        return new TeamsClient(_credential, _options, _loggerFactory);
    }
}
