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
    public async Task Invoke_UsesProvidedRequestIdOrGeneratesAndEchoesOnResponse()
    {
        var providedRequestId = "client-request-id-123";
        var providedContext = new DefaultHttpContext();
        providedContext.Request.Headers[RequestIdMiddleware.RequestIdHeaderName] = providedRequestId;

        await _middleware.Invoke(providedContext);

        Assert.Equal(providedRequestId, providedContext.Response.Headers[RequestIdMiddleware.RequestIdHeaderName].ToString());
        await _next.Received(1).Invoke(providedContext);

        _next.ClearReceivedCalls();

        var generatedContext = new DefaultHttpContext();
        LogEvent? capturedEvent = null;
        var testLogger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.Sink(new CapturingSink(logEvent => capturedEvent = logEvent))
            .CreateLogger();

        _next.When(next => next.Invoke(generatedContext))
            .Do(_ => testLogger.Information("within request"));

        await _middleware.Invoke(generatedContext);

        var responseRequestId = generatedContext.Response.Headers[RequestIdMiddleware.RequestIdHeaderName].ToString();

        Assert.False(string.IsNullOrWhiteSpace(responseRequestId));
        Assert.True(Guid.TryParse(responseRequestId, out _));
        Assert.NotNull(capturedEvent);
        Assert.True(capturedEvent!.Properties.TryGetValue("RequestId", out var requestIdProperty));
        Assert.Equal(responseRequestId, ((ScalarValue)requestIdProperty).Value?.ToString());
        await _next.Received(1).Invoke(generatedContext);
    }

    private sealed class CapturingSink(Action<LogEvent> onEmit) : ILogEventSink
    {
        public void Emit(LogEvent logEvent) => onEmit(logEvent);
    }
}
