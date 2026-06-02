using Bit.SharedWeb.Utilities;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Serilog;
using Serilog.Core;
using Serilog.Events;

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
    public async Task Invoke_WithIncomingRequestId_EchoesHeaderAndPushesLogContext()
    {
        const string requestId = "incoming-request-id";
        var context = new DefaultHttpContext();
        context.Request.Headers[RequestIdMiddleware.HeaderName] = requestId;

        var logEvents = new List<LogEvent>();
        _next.Invoke(Arg.Any<HttpContext>()).Returns(callInfo =>
        {
            var logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Sink(new DelegatingSink(logEvents.Add))
                .CreateLogger();

            logger.Information("test");
            return Task.CompletedTask;
        });

        await _middleware.Invoke(context);

        Assert.Equal(requestId, context.Response.Headers[RequestIdMiddleware.HeaderName].ToString());
        var loggedRequestId = Assert.Single(logEvents).Properties["RequestId"].LiteralValue();
        Assert.Equal(requestId, loggedRequestId);
        await _next.Received(1).Invoke(context);
    }

    [Fact]
    public async Task Invoke_WithoutIncomingRequestId_GeneratesUuidAndEchoesHeader()
    {
        var context = new DefaultHttpContext();

        await _middleware.Invoke(context);

        var responseRequestId = context.Response.Headers[RequestIdMiddleware.HeaderName].ToString();
        Assert.False(string.IsNullOrWhiteSpace(responseRequestId));
        Assert.True(Guid.TryParse(responseRequestId, out _));
        await _next.Received(1).Invoke(context);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Invoke_WithEmptyIncomingRequestId_GeneratesUuid(string requestId)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[RequestIdMiddleware.HeaderName] = requestId;

        await _middleware.Invoke(context);

        var responseRequestId = context.Response.Headers[RequestIdMiddleware.HeaderName].ToString();
        Assert.True(Guid.TryParse(responseRequestId, out _));
        await _next.Received(1).Invoke(context);
    }

    private sealed class DelegatingSink(Action<LogEvent> write) : ILogEventSink
    {
        public void Emit(LogEvent logEvent) => write(logEvent);
    }
}
