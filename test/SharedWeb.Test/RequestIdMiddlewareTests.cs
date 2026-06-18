using Bit.Core.Utilities;
using Bit.SharedWeb.Utilities;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;

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
    public async Task Invoke_WithoutIncomingHeader_SetsResponseHeaderToGuid()
    {
        var context = new DefaultHttpContext();

        await _middleware.Invoke(context);

        var requestId = context.Response.Headers[RequestIdMiddleware.HeaderName].ToString();
        Assert.False(string.IsNullOrEmpty(requestId));
        Assert.True(Guid.TryParse(requestId, out _));
        Assert.Equal(requestId, RequestIdHttpContext.GetRequestId(context));
        await _next.Received(1).Invoke(context);
    }

    [Fact]
    public async Task Invoke_WithIncomingHeader_EchoesSameValue()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[RequestIdMiddleware.HeaderName] = "incoming-id-123";

        await _middleware.Invoke(context);

        Assert.Equal("incoming-id-123", context.Response.Headers[RequestIdMiddleware.HeaderName].ToString());
        Assert.Equal("incoming-id-123", RequestIdHttpContext.GetRequestId(context));
        Assert.Equal("incoming-id-123", context.TraceIdentifier);
        await _next.Received(1).Invoke(context);
    }

    [Fact]
    public async Task Invoke_LogEventWrittenDuringRequest_IncludesExpectedRequestId()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[RequestIdMiddleware.HeaderName] = "log-test-id";

        LogEvent? capturedLogEvent = null;
        _next.When(next => next.Invoke(context)).Do(_ =>
        {
            var httpContextAccessor = new HttpContextAccessor { HttpContext = context };
            var enricher = new RequestIdEnricher(httpContextAccessor);
            var logEvent = new LogEvent(
                DateTimeOffset.UtcNow,
                LogEventLevel.Information,
                exception: null,
                new MessageTemplate("Test message", new List<MessageTemplateToken>()),
                properties: Array.Empty<LogEventProperty>());

            enricher.Enrich(logEvent, new TestLogEventPropertyFactory());
            capturedLogEvent = logEvent;
        });

        await _middleware.Invoke(context);

        Assert.NotNull(capturedLogEvent);
        Assert.True(capturedLogEvent!.Properties.ContainsKey("RequestId"));
        Assert.Equal("log-test-id", ((ScalarValue)capturedLogEvent.Properties["RequestId"].LiteralValue()).Value);
    }

    private sealed class TestLogEventPropertyFactory : ILogEventPropertyFactory
    {
        public LogEventProperty CreateProperty(string name, object? value, bool destructureObjects = false)
        {
            return new LogEventProperty(name, new ScalarValue(value));
        }
    }
}
