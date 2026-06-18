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
        _middleware = new RequestIdMiddleware(_next);
    }

    [Fact]
    public async Task Invoke_WithRequestIdHeader_EchoesHeaderAndStoresOnContext()
    {
        var knownRequestId = Guid.NewGuid().ToString();
        var context = new DefaultHttpContext();
        context.Request.Headers[RequestIdMiddleware.RequestIdHeaderName] = knownRequestId;
        string? requestIdDuringPipeline = null;

        _next.When(next => next.Invoke(Arg.Any<HttpContext>()))
            .Do(callInfo =>
            {
                requestIdDuringPipeline = RequestIdMiddleware.GetRequestId(callInfo.Arg<HttpContext>());
            });

        await _middleware.Invoke(context);

        Assert.Equal(knownRequestId, context.Response.Headers[RequestIdMiddleware.RequestIdHeaderName].ToString());
        Assert.Equal(knownRequestId, context.Items[RequestIdMiddleware.RequestIdItemKey]);
        Assert.Equal(knownRequestId, context.TraceIdentifier);
        Assert.Equal(knownRequestId, requestIdDuringPipeline);
        await _next.Received(1).Invoke(context);
    }

    [Fact]
    public async Task Invoke_WithoutRequestIdHeader_GeneratesGuidAndSetsRequestContext()
    {
        var context = new DefaultHttpContext();
        string? requestIdDuringPipeline = null;
        string? requestIdContextDuringPipeline = null;

        _next.When(next => next.Invoke(Arg.Any<HttpContext>()))
            .Do(callInfo =>
            {
                requestIdDuringPipeline = RequestIdMiddleware.GetRequestId(callInfo.Arg<HttpContext>());
                requestIdContextDuringPipeline = RequestIdContext.CurrentRequestId;
            });

        await _middleware.Invoke(context);

        var responseRequestId = context.Response.Headers[RequestIdMiddleware.RequestIdHeaderName].ToString();
        Assert.False(string.IsNullOrWhiteSpace(responseRequestId));
        Assert.True(Guid.TryParse(responseRequestId, out _));
        Assert.Equal(responseRequestId, context.Items[RequestIdMiddleware.RequestIdItemKey]);
        Assert.Equal(responseRequestId, requestIdDuringPipeline);
        Assert.Equal(responseRequestId, requestIdContextDuringPipeline);
        await _next.Received(1).Invoke(context);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Invoke_WithEmptyRequestIdHeader_GeneratesNewGuid(string headerValue)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[RequestIdMiddleware.RequestIdHeaderName] = headerValue;

        await _middleware.Invoke(context);

        var responseRequestId = context.Response.Headers[RequestIdMiddleware.RequestIdHeaderName].ToString();
        Assert.True(Guid.TryParse(responseRequestId, out _));
        await _next.Received(1).Invoke(context);
    }
}
