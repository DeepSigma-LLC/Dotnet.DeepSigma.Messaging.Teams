using Azure.Core;
using Azure.Identity;

namespace DeepSigma.Messaging.Teams.Authentication;

/// <summary>
/// Interactive device-code flow for delegated scenarios (CLIs, dev tools).
/// You must supply a callback to display the verification code and URL to the user —
/// the library will not write to the console on your behalf.
/// </summary>
public sealed class DeviceCodeCredential : ITeamsCredential
{
    private readonly Azure.Identity.DeviceCodeCredential _credential;

    public DeviceCodeCredential(
        string tenantId,
        string clientId,
        Func<DeviceCodeInfo, CancellationToken, Task> deviceCodeCallback,
        IReadOnlyList<string>? scopes = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentNullException.ThrowIfNull(deviceCodeCallback);

        var options = new DeviceCodeCredentialOptions
        {
            TenantId = tenantId,
            ClientId = clientId,
            DeviceCodeCallback = deviceCodeCallback,
        };

        _credential = new Azure.Identity.DeviceCodeCredential(options);
        Scopes = scopes ?? TeamsScopes.DelegatedDefault;
    }

    public IReadOnlyList<string> Scopes { get; }

    public TokenCredential ToTokenCredential() => _credential;
}
