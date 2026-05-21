using System.Net;

namespace DeepSigma.Messaging.Teams.Exceptions;

public sealed class TeamsAuthenticationException : TeamsApiException
{
    public TeamsAuthenticationException(string message) : base(message) { }

    public TeamsAuthenticationException(string message, Exception innerException) : base(message, innerException) { }

    public TeamsAuthenticationException(
        string message,
        HttpStatusCode? statusCode,
        string? errorCode,
        string? requestId,
        Exception? innerException = null)
        : base(message, statusCode, errorCode, requestId, innerException)
    {
    }
}
