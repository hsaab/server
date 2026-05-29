using Bit.Core.Utilities;
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
    public async Task Invoke_UsesIncomingRequestId_OnResponse_AndCallsNext()
    {
        const string requestId = "550e8400-e29b-41d4-a716-446655440000";
        var context = new DefaultHttpContext();
        context.Request.Headers[RequestIdMiddleware.RequestIdHeaderName] = requestId;

        await _middleware.Invoke(context);

        Assert.Equal(requestId, context.Response.Headers[RequestIdMiddleware.RequestIdHeaderName].ToString());
        await _next.Received(1).Invoke(context);
    }

    [Fact]
    public async Task Invoke_WithoutRequestIdHeader_GeneratesUuid_OnResponse_AndCallsNext()
    {
        var context = new DefaultHttpContext();

        await _middleware.Invoke(context);

        var responseRequestId = context.Response.Headers[RequestIdMiddleware.RequestIdHeaderName].ToString();
        Assert.False(string.IsNullOrWhiteSpace(responseRequestId));
        Assert.True(Guid.TryParse(responseRequestId, out _));
        await _next.Received(1).Invoke(context);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Invoke_WithEmptyOrWhitespaceRequestIdHeader_GeneratesUuid_OnResponse(string requestId)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[RequestIdMiddleware.RequestIdHeaderName] = requestId;

        await _middleware.Invoke(context);

        var responseRequestId = context.Response.Headers[RequestIdMiddleware.RequestIdHeaderName].ToString();
        Assert.True(Guid.TryParse(responseRequestId, out _));
        await _next.Received(1).Invoke(context);
    }
}
