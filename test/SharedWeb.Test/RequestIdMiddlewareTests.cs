using Bit.Core.Utilities;
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
        _next.Invoke(Arg.Any<HttpContext>()).Returns(callInfo =>
        {
            var context = callInfo.Arg<HttpContext>();
            return context.Response.StartAsync();
        });
        _middleware = new RequestIdMiddleware(_next);
    }

    [Fact]
    public async Task Invoke_WithIncomingRequestId_EchoesSameValueOnResponse()
    {
        var context = CreateContext();
        context.Request.Headers[RequestIdUtilities.HeaderName] = "incoming-request-id";

        await _middleware.Invoke(context);

        Assert.Equal("incoming-request-id", context.Response.Headers[RequestIdUtilities.HeaderName].ToString());
        Assert.Equal("incoming-request-id", context.Items[RequestIdUtilities.HttpContextItemKey]);
        Assert.Equal("incoming-request-id", context.TraceIdentifier);
        await _next.Received(1).Invoke(context);
    }

    [Fact]
    public async Task Invoke_WithoutIncomingRequestId_GeneratesUuidResponseHeader()
    {
        var context = CreateContext();

        await _middleware.Invoke(context);

        var responseRequestId = context.Response.Headers[RequestIdUtilities.HeaderName].ToString();
        Assert.True(Guid.TryParse(responseRequestId, out _));
        Assert.Equal(responseRequestId, context.Items[RequestIdUtilities.HttpContextItemKey]);
        Assert.Equal(responseRequestId, context.TraceIdentifier);
        await _next.Received(1).Invoke(context);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Invoke_WithEmptyIncomingRequestId_GeneratesUuidResponseHeader(string incomingRequestId)
    {
        var context = CreateContext();
        context.Request.Headers[RequestIdUtilities.HeaderName] = incomingRequestId;

        await _middleware.Invoke(context);

        var responseRequestId = context.Response.Headers[RequestIdUtilities.HeaderName].ToString();
        Assert.True(Guid.TryParse(responseRequestId, out _));
        await _next.Received(1).Invoke(context);
    }

    [Fact]
    public async Task Invoke_SetsRequestIdContextForLogging()
    {
        var context = CreateContext();
        context.Request.Headers[RequestIdUtilities.HeaderName] = "logging-request-id";
        string? capturedRequestId = null;

        _next.Invoke(Arg.Any<HttpContext>()).Returns(callInfo =>
        {
            capturedRequestId = RequestIdContext.Current;
            var httpContext = callInfo.Arg<HttpContext>();
            return httpContext.Response.StartAsync();
        });

        await _middleware.Invoke(context);

        Assert.Equal("logging-request-id", capturedRequestId);
        Assert.Null(RequestIdContext.Current);
    }

    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }
}
