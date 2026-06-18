using Bit.Core.Utilities;
using Bit.SharedWeb.Utilities;
using Microsoft.AspNetCore.Http;
using NSubstitute;
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
    public async Task Invoke_WithoutRequestIdHeader_GeneratesGuidAndEnrichesLogging()
    {
        var context = new DefaultHttpContext();
        string? requestIdDuringPipeline = null;
        LogEventPropertyValue? requestIdPropertyDuringPipeline = null;

        _next.When(next => next.Invoke(Arg.Any<HttpContext>()))
            .Do(callInfo =>
            {
                requestIdDuringPipeline = RequestIdMiddleware.GetRequestId(callInfo.Arg<HttpContext>());

                var enricher = new RequestIdEnricher();
                var logEvent = CreateLogEvent();
                enricher.Enrich(logEvent, new TestLogEventPropertyFactory());
                logEvent.Properties.TryGetValue("RequestId", out requestIdPropertyDuringPipeline);
            });

        await _middleware.Invoke(context);

        var responseRequestId = context.Response.Headers[RequestIdMiddleware.RequestIdHeaderName].ToString();
        Assert.False(string.IsNullOrWhiteSpace(responseRequestId));
        Assert.True(Guid.TryParse(responseRequestId, out _));
        Assert.Equal(responseRequestId, context.Items[RequestIdMiddleware.RequestIdItemKey]);
        Assert.Equal(responseRequestId, requestIdDuringPipeline);
        Assert.NotNull(requestIdPropertyDuringPipeline);
        Assert.Equal(responseRequestId, ((ScalarValue)requestIdPropertyDuringPipeline).Value);
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

    private static LogEvent CreateLogEvent()
    {
        return new LogEvent(
            DateTimeOffset.UtcNow,
            LogEventLevel.Information,
            exception: null,
            messageTemplate: new Serilog.Parsing.MessageTemplate("test", []),
            properties: []);
    }

    private sealed class TestLogEventPropertyFactory : ILogEventPropertyFactory
    {
        public LogEventProperty CreateProperty(string name, object? value, bool destructureObjects = false)
        {
            return new LogEventProperty(name, new ScalarValue(value));
        }
    }
}
