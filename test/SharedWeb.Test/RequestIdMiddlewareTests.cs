using Bit.SharedWeb.Utilities;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace SharedWeb.Test;

public class RequestIdMiddlewareTests
{
    private readonly RequestDelegate _next;
    private readonly RequestIdMiddleware _middleware;

    public RequestIdMiddlewareTests()
    {
        _next = Substitute.For<RequestDelegate>();
        _middleware = new RequestIdMiddleware(_next);
    }

    [Fact]
    public async Task Invoke_ReadsOrGeneratesRequestIdAndEchoesOnResponse()
    {
        var contextWithHeader = new DefaultHttpContext();
        contextWithHeader.Request.Headers[RequestIdMiddleware.RequestIdHeaderName] = "trace-abc-123";

        await _middleware.Invoke(contextWithHeader);

        Assert.Equal(
            "trace-abc-123",
            contextWithHeader.Response.Headers[RequestIdMiddleware.RequestIdHeaderName].ToString());
        await _next.Received(1).Invoke(contextWithHeader);

        _next.ClearReceivedCalls();

        var contextWithoutHeader = new DefaultHttpContext();

        await _middleware.Invoke(contextWithoutHeader);

        var responseRequestId = contextWithoutHeader.Response.Headers[RequestIdMiddleware.RequestIdHeaderName]
            .ToString();
        Assert.False(string.IsNullOrWhiteSpace(responseRequestId));
        Assert.True(Guid.TryParse(responseRequestId, out _));
        await _next.Received(1).Invoke(contextWithoutHeader);
    }
}
