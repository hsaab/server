using Bit.SharedWeb.Utilities;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Serilog;
using Serilog.Context;
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

    [Theory]
    [InlineData("existing-request-id")]
    [InlineData(null)]
    public async Task Invoke_SetsResponseHeaderAndEnrichesLogs(string? incomingRequestId)
    {
        var context = new DefaultHttpContext();
        if (incomingRequestId != null)
        {
            context.Request.Headers[RequestIdMiddleware.HeaderName] = incomingRequestId;
        }

        var logEvents = await InvokeAndCaptureLogEventsAsync(context);

        var responseRequestId = context.Response.Headers[RequestIdMiddleware.HeaderName].ToString();
        if (incomingRequestId != null)
        {
            Assert.Equal(incomingRequestId, responseRequestId);
        }
        else
        {
            Assert.False(string.IsNullOrWhiteSpace(responseRequestId));
            Assert.True(Guid.TryParse(responseRequestId, out _));
        }

        await _next.Received(1).Invoke(context);

        var logEvent = Assert.Single(logEvents);
        var loggedRequestId = Assert.IsType<ScalarValue>(logEvent.Properties["RequestId"]).Value;
        Assert.Equal(responseRequestId, loggedRequestId);
    }

    private async Task<IReadOnlyList<LogEvent>> InvokeAndCaptureLogEventsAsync(HttpContext context)
    {
        var collectingSink = new CollectingSink();
        var logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.Sink(collectingSink)
            .CreateLogger();

        Log.Logger = logger;

        _next
            .When(next => next.Invoke(context))
            .Do(_ => Log.Information("test log"));

        await _middleware.Invoke(context);

        return collectingSink.Events;
    }

    private sealed class CollectingSink : ILogEventSink
    {
        public List<LogEvent> Events { get; } = [];

        public void Emit(LogEvent logEvent) => Events.Add(logEvent);
    }
}
