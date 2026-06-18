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
    public async Task Invoke_WithoutRequestIdHeader_GeneratesRequestIdAndEchoesOnResponse()
    {
        var context = new DefaultHttpContext();

        await _middleware.Invoke(context);

        var responseRequestId = Assert.Single(context.Response.Headers[RequestIdMiddleware.RequestIdHeaderName]);
        Assert.True(Guid.TryParse(responseRequestId, out _));
        await _next.Received(1).Invoke(context);
    }

    [Fact]
    public async Task Invoke_WithRequestIdHeader_UsesIncomingValueAndEchoesOnResponse()
    {
        var context = new DefaultHttpContext();
        const string requestId = "upstream-trace-id-12345";
        context.Request.Headers[RequestIdMiddleware.RequestIdHeaderName] = requestId;

        await _middleware.Invoke(context);

        Assert.Equal(requestId, context.Response.Headers[RequestIdMiddleware.RequestIdHeaderName]);
        await _next.Received(1).Invoke(context);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public async Task Invoke_WithEmptyOrWhitespaceRequestIdHeader_GeneratesRequestId(string requestId)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[RequestIdMiddleware.RequestIdHeaderName] = requestId;

        await _middleware.Invoke(context);

        var responseRequestId = Assert.Single(context.Response.Headers[RequestIdMiddleware.RequestIdHeaderName]);
        Assert.True(Guid.TryParse(responseRequestId, out _));
        await _next.Received(1).Invoke(context);
    }

    [Fact]
    public async Task Invoke_WithRequestIdHeader_IsCaseInsensitive()
    {
        var context = new DefaultHttpContext();
        const string requestId = "case-insensitive-trace-id";
        context.Request.Headers["x-request-id"] = requestId;

        await _middleware.Invoke(context);

        Assert.Equal(requestId, context.Response.Headers[RequestIdMiddleware.RequestIdHeaderName]);
        await _next.Received(1).Invoke(context);
    }
}
