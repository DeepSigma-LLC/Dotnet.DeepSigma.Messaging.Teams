using System.Net;
using DeepSigma.Messaging.Teams.Exceptions;
using DeepSigma.Messaging.Teams.Internal;
using Microsoft.Graph.Models.ODataErrors;
using Microsoft.Kiota.Abstractions;
using Xunit;

namespace DeepSigma.Messaging.Teams.UnitTests;

public class GraphErrorMapperTests
{
    [Fact]
    public void Translate_Unauthorized_ReturnsAuthenticationException()
    {
        var ex = new ApiException("denied") { ResponseStatusCode = 401 };

        var translated = GraphErrorMapper.Translate(ex);

        Assert.IsType<TeamsAuthenticationException>(translated);
        Assert.Equal(HttpStatusCode.Unauthorized, translated.StatusCode);
    }

    [Fact]
    public void Translate_Forbidden_ReturnsAuthenticationException()
    {
        var ex = new ApiException("forbidden") { ResponseStatusCode = 403 };

        var translated = GraphErrorMapper.Translate(ex);

        Assert.IsType<TeamsAuthenticationException>(translated);
    }

    [Fact]
    public void Translate_TooManyRequests_ReturnsThrottledExceptionWithRetryAfter()
    {
        var ex = new ApiException("slow down")
        {
            ResponseStatusCode = 429,
            ResponseHeaders = new Dictionary<string, IEnumerable<string>>
            {
                ["Retry-After"] = new[] { "12" },
                ["request-id"] = new[] { "rid-abc" },
            },
        };

        var translated = GraphErrorMapper.Translate(ex);

        var throttled = Assert.IsType<TeamsThrottledException>(translated);
        Assert.Equal(TimeSpan.FromSeconds(12), throttled.RetryAfter);
        Assert.Equal("rid-abc", throttled.RequestId);
    }

    [Fact]
    public void Translate_BadRequest_ReturnsBaseTeamsApiException()
    {
        var ex = new ApiException("nope") { ResponseStatusCode = 400 };

        var translated = GraphErrorMapper.Translate(ex);

        Assert.Equal(typeof(TeamsApiException), translated.GetType());
        Assert.Equal(HttpStatusCode.BadRequest, translated.StatusCode);
    }

    [Fact]
    public void Translate_ODataError_ExtractsErrorCodeAndMessage()
    {
        var odata = new ODataError
        {
            ResponseStatusCode = 400,
            Error = new MainError
            {
                Code = "InvalidRequest",
                Message = "The team ID is malformed.",
            },
        };

        var translated = GraphErrorMapper.Translate(odata);

        Assert.Equal("InvalidRequest", translated.ErrorCode);
        Assert.Contains("InvalidRequest", translated.Message, StringComparison.Ordinal);
        Assert.Contains("malformed", translated.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Translate_MissingResponseHeaders_DoesNotThrow()
    {
        var ex = new ApiException("oops") { ResponseStatusCode = 500, ResponseHeaders = null! };

        var translated = GraphErrorMapper.Translate(ex);

        Assert.Equal(HttpStatusCode.InternalServerError, translated.StatusCode);
        Assert.Null(translated.RequestId);
    }
}
