using Bit.Core.Utilities;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;
using Xunit;

namespace Bit.Core.Test.Utilities;

public class RequestIdEnricherTests
{
    [Fact]
    public void Enrich_WhenRequestContextIsSet_AddsRequestIdProperty()
    {
        RequestIdContext.Current = "abc-123";
        try
        {
            var enricher = new RequestIdEnricher();
            var logEvent = CreateLogEvent();
            enricher.Enrich(logEvent, new TestLogEventPropertyFactory());

            Assert.True(logEvent.Properties.TryGetValue("requestId", out var property));
            Assert.Equal("abc-123", ((ScalarValue)property).Value);
        }
        finally
        {
            RequestIdContext.Current = null;
        }
    }

    [Fact]
    public void Enrich_WhenRequestContextIsMissing_DoesNotAddRequestIdProperty()
    {
        RequestIdContext.Current = null;
        var enricher = new RequestIdEnricher();
        var logEvent = CreateLogEvent();
        enricher.Enrich(logEvent, new TestLogEventPropertyFactory());

        Assert.False(logEvent.Properties.ContainsKey("requestId"));
    }

    private static LogEvent CreateLogEvent()
    {
        return new LogEvent(
            DateTimeOffset.UtcNow,
            LogEventLevel.Information,
            exception: null,
            new MessageTemplate("test", new List<MessageTemplateToken>()),
            properties: new List<LogEventProperty>());
    }

    private sealed class TestLogEventPropertyFactory : ILogEventPropertyFactory
    {
        public LogEventProperty CreateProperty(string name, object? value, bool destructureObjects = false)
        {
            return new LogEventProperty(name, new ScalarValue(value));
        }
    }
}
