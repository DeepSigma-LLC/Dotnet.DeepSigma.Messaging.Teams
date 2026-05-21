using System.Net;

namespace DeepSigma.Messaging.Teams.Exceptions;

public class TeamsApiException : Exception
{
    public HttpStatusCode? StatusCode { get; }
    public string? ErrorCode { get; }
    public string? RequestId { get; }

    public TeamsApiException(string message) : base(message) { }

    public TeamsApiException(string message, Exception innerException) : base(message, innerException) { }

    public TeamsApiException(
        string message,
        HttpStatusCode? statusCode,
        string? errorCode,
        string? requestId,
        Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
        RequestId = requestId;
    }
}
