using Bit.Core.Utilities;
using Microsoft.AspNetCore.Http;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;

namespace Bit.Core.Test.Utilities;

public class RequestIdEnricherTests
{
    [Fact]
    public void Enrich_WithRequestIdOnHttpContext_AddsRequestIdProperty()
    {
        var context = new DefaultHttpContext();
        RequestIdHttpContext.SetRequestId(context, "enricher-test-id");

        var httpContextAccessor = new HttpContextAccessor { HttpContext = context };
        var enricher = new RequestIdEnricher(httpContextAccessor);
        var logEvent = CreateLogEvent();

        enricher.Enrich(logEvent, new TestLogEventPropertyFactory());

        Assert.True(logEvent.Properties.ContainsKey("RequestId"));
        Assert.Equal("enricher-test-id", ((ScalarValue)logEvent.Properties["RequestId"].LiteralValue()).Value);
    }

    [Fact]
    public void Enrich_WithoutHttpContext_DoesNotAddRequestIdProperty()
    {
        var httpContextAccessor = new HttpContextAccessor();
        var enricher = new RequestIdEnricher(httpContextAccessor);
        var logEvent = CreateLogEvent();

        enricher.Enrich(logEvent, new TestLogEventPropertyFactory());

        Assert.False(logEvent.Properties.ContainsKey("RequestId"));
    }

    private static LogEvent CreateLogEvent()
    {
        return new LogEvent(
            DateTimeOffset.UtcNow,
            LogEventLevel.Information,
            exception: null,
            new MessageTemplate("Test message", new List<MessageTemplateToken>()),
            properties: Array.Empty<LogEventProperty>());
    }

    private sealed class TestLogEventPropertyFactory : ILogEventPropertyFactory
    {
        public LogEventProperty CreateProperty(string name, object? value, bool destructureObjects = false)
        {
            return new LogEventProperty(name, new ScalarValue(value));
        }
    }
}
