using Bit.SharedWeb.Utilities;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace SharedWeb.Test;

public class RequestIdMiddlewareTests
{
    private readonly RequestDelegate _next = Substitute.For<RequestDelegate>();
    private readonly RequestIdMiddleware _middleware;

    public RequestIdMiddlewareTests()
    {
        _middleware = new RequestIdMiddleware(_next);
    }

    [Fact]
    public async Task Invoke_PropagatesOrGeneratesRequestIdOnResponse()
    {
        var contextWithHeader = new DefaultHttpContext();
        contextWithHeader.Request.Headers[RequestIdMiddleware.RequestIdHeaderName] = "incoming-trace-id";
        await _middleware.Invoke(contextWithHeader);
        Assert.Equal("incoming-trace-id", contextWithHeader.Response.Headers[RequestIdMiddleware.RequestIdHeaderName]);

        var contextWithoutHeader = new DefaultHttpContext();
        await _middleware.Invoke(contextWithoutHeader);
        var generatedRequestId = contextWithoutHeader.Response.Headers[RequestIdMiddleware.RequestIdHeaderName].ToString();
        Assert.False(string.IsNullOrWhiteSpace(generatedRequestId));
        Assert.True(Guid.TryParse(generatedRequestId, out _));

        await _next.Received(2).Invoke(Arg.Any<HttpContext>());
    }

    [Fact]
    public async Task Invoke_RejectsRequestIdHeaderOverMaxLength()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[RequestIdMiddleware.RequestIdHeaderName] = new string('a', 257);

        await _middleware.Invoke(context);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.False(context.Response.Headers.ContainsKey(RequestIdMiddleware.RequestIdHeaderName));
        await _next.DidNotReceive().Invoke(Arg.Any<HttpContext>());
    }

    [Fact]
    public async Task Invoke_RejectsRequestIdHeaderWithControlCharacters()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[RequestIdMiddleware.RequestIdHeaderName] = "incoming\u001ftrace-id";

        await _middleware.Invoke(context);

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        Assert.False(context.Response.Headers.ContainsKey(RequestIdMiddleware.RequestIdHeaderName));
        await _next.DidNotReceive().Invoke(Arg.Any<HttpContext>());
    }
}
