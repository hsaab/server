using Bit.Core.Utilities;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;
using Xunit;

namespace Bit.Core.Test.Utilities;

public class RequestIdEnricherTests
{
    [Fact]
    public void Enrich_WithRequestId_AddsRequestIdProperty()
    {
        RequestIdContext.Current = "request-id-123";
        try
        {
            var enricher = new RequestIdEnricher();
            var logEvent = CreateLogEvent();

            enricher.Enrich(logEvent, new FixedPropertyFactory());

            Assert.True(logEvent.Properties.TryGetValue("requestId", out var property));
            Assert.Equal("request-id-123", ((ScalarValue)property).Value);
        }
        finally
        {
            RequestIdContext.Current = null;
        }
    }

    [Fact]
    public void Enrich_WithoutRequestId_DoesNotAddProperty()
    {
        RequestIdContext.Current = null;

        var enricher = new RequestIdEnricher();
        var logEvent = CreateLogEvent();

        enricher.Enrich(logEvent, new FixedPropertyFactory());

        Assert.False(logEvent.Properties.ContainsKey("requestId"));
    }

    private static LogEvent CreateLogEvent() =>
        new(
            DateTimeOffset.UtcNow,
            LogEventLevel.Information,
            null,
            new MessageTemplate("test", new List<MessageTemplateToken>()),
            new List<LogEventProperty>());

    private sealed class FixedPropertyFactory : ILogEventPropertyFactory
    {
        public LogEventProperty CreateProperty(string name, object? value, bool destructureObjects = false) =>
            new(name, new ScalarValue(value));
    }
}
