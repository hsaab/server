using Bit.Api.Middleware;
using Bit.Core.Utilities;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace Bit.Api.Test.Middleware;

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
    public async Task Invoke_WithValidRequestIdHeader_PassesThroughRequestId()
    {
        var expectedRequestId = Guid.NewGuid().ToString();
        var context = CreateHttpContext();
        context.Request.Headers[RequestIdConstants.HeaderName] = expectedRequestId;

        await _middleware.Invoke(context);

        Assert.Equal(expectedRequestId, context.Items[RequestIdConstants.ItemKey]);
        Assert.Equal(expectedRequestId, context.TraceIdentifier);
        Assert.Equal(expectedRequestId, context.Response.Headers[RequestIdConstants.HeaderName].ToString());
        await _next.Received(1).Invoke(context);
    }

    [Fact]
    public async Task Invoke_WithoutRequestIdHeader_GeneratesRequestId()
    {
        var context = CreateHttpContext();

        await _middleware.Invoke(context);

        var requestId = Assert.IsType<string>(context.Items[RequestIdConstants.ItemKey]);
        Assert.True(Guid.TryParse(requestId, out _));
        Assert.Equal(requestId, context.TraceIdentifier);
        Assert.Equal(requestId, context.Response.Headers[RequestIdConstants.HeaderName].ToString());
        await _next.Received(1).Invoke(context);
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("")]
    [InlineData("12345")]
    public async Task Invoke_WithInvalidRequestIdHeader_GeneratesRequestId(string invalidRequestId)
    {
        var context = CreateHttpContext();
        context.Request.Headers[RequestIdConstants.HeaderName] = invalidRequestId;

        await _middleware.Invoke(context);

        var requestId = Assert.IsType<string>(context.Items[RequestIdConstants.ItemKey]);
        Assert.True(Guid.TryParse(requestId, out _));
        Assert.Equal(requestId, context.Response.Headers[RequestIdConstants.HeaderName].ToString());
        await _next.Received(1).Invoke(context);
    }

    [Fact]
    public async Task Invoke_SetsMatchingResponseHeader()
    {
        var context = CreateHttpContext();
        var expectedRequestId = Guid.NewGuid().ToString();
        context.Request.Headers[RequestIdConstants.HeaderName] = expectedRequestId;

        await _middleware.Invoke(context);

        Assert.True(context.Response.Headers.ContainsKey(RequestIdConstants.HeaderName));
        Assert.Equal(expectedRequestId, context.Response.Headers[RequestIdConstants.HeaderName].ToString());
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }
}
