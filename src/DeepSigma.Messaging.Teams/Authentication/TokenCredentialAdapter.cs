using Azure.Core;

namespace DeepSigma.Messaging.Teams.Authentication;

/// <summary>
/// Adapts any <see cref="Azure.Core.TokenCredential"/> (e.g. <c>ManagedIdentityCredential</c>,
/// <c>WorkloadIdentityCredential</c>, <c>InteractiveBrowserCredential</c>, <c>DefaultAzureCredential</c>)
/// into an <see cref="ITeamsCredential"/>. Scopes must be supplied explicitly — the adapter
/// cannot know whether the wrapped credential represents an app-only or delegated flow.
/// </summary>
public sealed class TokenCredentialAdapter : ITeamsCredential
{
    private readonly TokenCredential _credential;

    public TokenCredentialAdapter(TokenCredential credential, IReadOnlyList<string> scopes)
    {
        ArgumentNullException.ThrowIfNull(credential);
        ArgumentNullException.ThrowIfNull(scopes);
        if (scopes.Count == 0)
        {
            throw new ArgumentException("At least one scope must be supplied.", nameof(scopes));
        }
        _credential = credential;
        Scopes = scopes;
    }

    public IReadOnlyList<string> Scopes { get; }

    public TokenCredential ToTokenCredential() => _credential;
}
