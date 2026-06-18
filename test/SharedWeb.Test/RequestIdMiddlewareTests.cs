using System.Globalization;
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
    public async Task Invoke_UsesIncomingRequestId_EchoesOnResponse_AndEnrichesLogs()
    {
        var context = new DefaultHttpContext();
        const string requestId = "incoming-request-id";
        context.Request.Headers[RequestIdMiddleware.RequestIdHeaderName] = requestId;

        LogEvent? capturedLogEvent = null;
        var logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.Sink(new CaptureSink(logEvent => capturedLogEvent = logEvent))
            .CreateLogger();

        _next.When(next => next.Invoke(context)).Do(_ => logger.Information("during request"));

        await _middleware.Invoke(context);

        await _next.Received(1).Invoke(context);
        Assert.Equal(requestId, context.Response.Headers[RequestIdMiddleware.RequestIdHeaderName].ToString());

        Assert.NotNull(capturedLogEvent);
        Assert.Equal("during request", capturedLogEvent.RenderMessage(CultureInfo.InvariantCulture));
        Assert.Equal(requestId, capturedLogEvent.Properties[RequestIdMiddleware.RequestIdPropertyName].ToString().Trim('"'));
    }

    [Fact]
    public async Task Invoke_WithoutRequestIdHeader_GeneratesUuidAndEchoesOnResponse()
    {
        var context = new DefaultHttpContext();

        await _middleware.Invoke(context);

        await _next.Received(1).Invoke(context);

        var responseRequestId = context.Response.Headers[RequestIdMiddleware.RequestIdHeaderName].ToString();
        Assert.False(string.IsNullOrWhiteSpace(responseRequestId));
        Assert.True(Guid.TryParse(responseRequestId, out _));
    }

    private sealed class CaptureSink(Action<LogEvent> onEvent) : ILogEventSink
    {
        public void Emit(LogEvent logEvent) => onEvent(logEvent);
    }
}
