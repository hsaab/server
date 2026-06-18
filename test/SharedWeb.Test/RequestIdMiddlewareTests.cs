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
    public async Task Invoke_WithIncomingRequestId_EchoesRequestIdInResponseAndContext()
    {
        const string requestId = "550e8400-e29b-41d4-a716-446655440000";
        var context = new DefaultHttpContext();
        context.Request.Headers[RequestIdMiddleware.HeaderName] = requestId;

        await _middleware.Invoke(context);

        Assert.Equal(requestId, context.Response.Headers[RequestIdMiddleware.HeaderName].ToString());
        Assert.Equal(requestId, context.Items[RequestIdMiddleware.HttpContextItemKey]);
        await _next.Received(1).Invoke(context);
    }

    [Fact]
    public async Task Invoke_WithoutRequestIdHeader_GeneratesUuidAndEchoesInResponse()
    {
        var context = new DefaultHttpContext();

        await _middleware.Invoke(context);

        var requestId = Assert.IsType<string>(context.Items[RequestIdMiddleware.HttpContextItemKey]);
        Assert.True(Guid.TryParse(requestId, out _));
        Assert.Equal(requestId, context.Response.Headers[RequestIdMiddleware.HeaderName].ToString());
        await _next.Received(1).Invoke(context);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public async Task Invoke_WithEmptyOrWhitespaceRequestId_GeneratesUuid(string requestId)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[RequestIdMiddleware.HeaderName] = requestId;

        await _middleware.Invoke(context);

        var generatedRequestId = Assert.IsType<string>(context.Items[RequestIdMiddleware.HttpContextItemKey]);
        Assert.True(Guid.TryParse(generatedRequestId, out _));
        Assert.Equal(generatedRequestId, context.Response.Headers[RequestIdMiddleware.HeaderName].ToString());
        await _next.Received(1).Invoke(context);
    }

    [Fact]
    public async Task Invoke_WithIncomingRequestId_PreservesUpstreamValueWhenNotUuid()
    {
        const string requestId = "upstream-trace-id-12345";
        var context = new DefaultHttpContext();
        context.Request.Headers[RequestIdMiddleware.HeaderName] = requestId;

        await _middleware.Invoke(context);

        Assert.Equal(requestId, context.Items[RequestIdMiddleware.HttpContextItemKey]);
        Assert.Equal(requestId, context.Response.Headers[RequestIdMiddleware.HeaderName].ToString());
    }
}
