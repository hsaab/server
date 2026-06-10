using Bit.SharedWeb.Utilities;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Xunit;

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
    public async Task Invoke_WithRequestIdHeader_UsesProvidedIdAndEchoesOnResponse()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[RequestIdMiddleware.HeaderName] = "incoming-request-id";

        await _middleware.Invoke(context);

        Assert.Equal("incoming-request-id", context.Response.Headers[RequestIdMiddleware.HeaderName]);
        await _next.Received(1).Invoke(context);
    }

    [Fact]
    public async Task Invoke_WithoutRequestIdHeader_GeneratesUuidAndEchoesOnResponse()
    {
        var context = new DefaultHttpContext();

        await _middleware.Invoke(context);

        var responseRequestId = context.Response.Headers[RequestIdMiddleware.HeaderName].ToString();
        Assert.False(string.IsNullOrWhiteSpace(responseRequestId));
        Assert.True(Guid.TryParse(responseRequestId, out _));
        await _next.Received(1).Invoke(context);
    }

    [Fact]
    public async Task Invoke_PushesRequestIdToLogContext()
    {
        var logEvents = new List<LogEvent>();
        var previousLogger = Log.Logger;

        try
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Sink(new CollectingSink(logEvents.Add))
                .CreateLogger();

            var context = new DefaultHttpContext();
            context.Request.Headers[RequestIdMiddleware.HeaderName] = "trace-123";

            _next.When(next => next.Invoke(Arg.Any<HttpContext>()))
                .Do(_ => Log.Information("during request"));

            await _middleware.Invoke(context);

            var logEvent = Assert.Single(logEvents);
            Assert.Equal("trace-123", ((ScalarValue)logEvent.Properties["RequestId"]).Value);
        }
        finally
        {
            Log.Logger = previousLogger;
        }
    }

    private sealed class CollectingSink(Action<LogEvent> emit) : ILogEventSink
    {
        public void Emit(LogEvent logEvent) => emit(logEvent);
    }
}
