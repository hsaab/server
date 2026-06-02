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
    public async Task Invoke_WithIncomingRequestId_EchoesSameIdOnResponse()
    {
        const string requestId = "incoming-request-id";
        var context = CreateContext();
        context.Request.Headers[RequestIdMiddleware.HeaderName] = requestId;

        await _middleware.Invoke(context);
        await context.Response.StartAsync();

        Assert.Equal(requestId, context.Response.Headers[RequestIdMiddleware.HeaderName].ToString());
        await _next.Received(1).Invoke(context);
    }

    [Fact]
    public async Task Invoke_WithoutRequestId_GeneratesUuidAndEchoesOnResponse()
    {
        var context = CreateContext();

        await _middleware.Invoke(context);
        await context.Response.StartAsync();

        var responseRequestId = context.Response.Headers[RequestIdMiddleware.HeaderName].ToString();
        Assert.False(string.IsNullOrWhiteSpace(responseRequestId));
        Assert.True(Guid.TryParse(responseRequestId, out _));
        await _next.Received(1).Invoke(context);
    }

    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }
}
