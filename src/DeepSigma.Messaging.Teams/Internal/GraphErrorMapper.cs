using System.Net;
using DeepSigma.Messaging.Teams.Exceptions;
using Microsoft.Graph.Models.ODataErrors;
using Microsoft.Kiota.Abstractions;

namespace DeepSigma.Messaging.Teams.Internal;

internal static class GraphErrorMapper
{
    public static async Task<T> ExecuteAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return await action().ConfigureAwait(false);
        }
        catch (ApiException ex)
        {
            throw Translate(ex);
        }
    }

    public static async Task ExecuteAsync(Func<Task> action)
    {
        try
        {
            await action().ConfigureAwait(false);
        }
        catch (ApiException ex)
        {
            throw Translate(ex);
        }
    }

    internal static TeamsApiException Translate(ApiException ex)
    {
        var status = ex.ResponseStatusCode > 0 ? (HttpStatusCode?)ex.ResponseStatusCode : null;
        var requestId = TryGetHeader(ex.ResponseHeaders, "request-id");

        string? errorCode = null;
        string? errorMessage = null;
        if (ex is ODataError odata)
        {
            errorCode = odata.Error?.Code;
            errorMessage = odata.Error?.Message;
        }

        var message = errorMessage is not null
            ? (errorCode is not null ? $"{errorCode}: {errorMessage}" : errorMessage)
            : ex.Message;

        if (status == HttpStatusCode.TooManyRequests)
        {
            TimeSpan? retryAfter = null;
            var retryValue = TryGetHeader(ex.ResponseHeaders, "Retry-After");
            if (int.TryParse(retryValue, out var seconds))
            {
                retryAfter = TimeSpan.FromSeconds(seconds);
            }
            return new TeamsThrottledException(message, retryAfter, requestId, ex);
        }

        if (status == HttpStatusCode.Unauthorized || status == HttpStatusCode.Forbidden)
        {
            return new TeamsAuthenticationException(message, status, errorCode, requestId, ex);
        }

        return new TeamsApiException(message, status, errorCode, requestId, ex);
    }

    private static string? TryGetHeader(IDictionary<string, IEnumerable<string>>? headers, string name)
    {
        if (headers is null || !headers.TryGetValue(name, out var values) || values is null)
        {
            return null;
        }
        return values.FirstOrDefault();
    }
}
