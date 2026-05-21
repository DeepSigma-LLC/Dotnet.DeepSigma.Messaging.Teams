namespace DeepSigma.Messaging.Teams;

public sealed class TeamsClientOptions
{
    /// <summary>
    /// Override the Microsoft Graph base URL (for sovereign clouds, e.g. US Gov: <c>https://graph.microsoft.us/v1.0</c>).
    /// Leave null to use the global Graph endpoint.
    /// </summary>
    public string? BaseUrl { get; set; }
}
