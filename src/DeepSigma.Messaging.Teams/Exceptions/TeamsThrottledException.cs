using System.Net;

namespace DeepSigma.Messaging.Teams.Exceptions;

public sealed class TeamsThrottledException : TeamsApiException
{
    public TimeSpan? RetryAfter { get; }

    public TeamsThrottledException(string message, TimeSpan? retryAfter, string? requestId, Exception? innerException = null)
        : base(message, HttpStatusCode.TooManyRequests, "TooManyRequests", requestId, innerException)
    {
        RetryAfter = retryAfter;
    }
}
