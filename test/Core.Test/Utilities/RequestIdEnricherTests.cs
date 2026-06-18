using Bit.Core.Utilities;
using Serilog.Core;
using Serilog.Events;
using Xunit;

namespace Bit.Core.Test.Utilities;

public class RequestIdEnricherTests
{
    [Fact]
    public void Enrich_WithCurrentRequestId_AddsRequestIdProperty()
    {
        var enricher = new RequestIdEnricher();
        var logEvent = CreateLogEvent();
        RequestIdContext.CurrentRequestId = "test-request-id";

        try
        {
            enricher.Enrich(logEvent, new TestLogEventPropertyFactory());

            Assert.True(logEvent.Properties.TryGetValue("RequestId", out var requestIdProperty));
            Assert.Equal("test-request-id", ((ScalarValue)requestIdProperty).Value);
        }
        finally
        {
            RequestIdContext.CurrentRequestId = null;
        }
    }

    [Fact]
    public void Enrich_WithoutCurrentRequestId_DoesNotAddProperty()
    {
        var enricher = new RequestIdEnricher();
        var logEvent = CreateLogEvent();
        RequestIdContext.CurrentRequestId = null;

        enricher.Enrich(logEvent, new TestLogEventPropertyFactory());

        Assert.False(logEvent.Properties.ContainsKey("RequestId"));
    }

    [Fact]
    public void Enrich_WhenRequestIdAlreadyPresent_DoesNotOverwrite()
    {
        var enricher = new RequestIdEnricher();
        var logEvent = CreateLogEvent(
            new LogEventProperty("RequestId", new ScalarValue("existing-request-id")));
        RequestIdContext.CurrentRequestId = "new-request-id";

        try
        {
            enricher.Enrich(logEvent, new TestLogEventPropertyFactory());

            Assert.True(logEvent.Properties.TryGetValue("RequestId", out var requestIdProperty));
            Assert.Equal("existing-request-id", ((ScalarValue)requestIdProperty).Value);
        }
        finally
        {
            RequestIdContext.CurrentRequestId = null;
        }
    }

    private static LogEvent CreateLogEvent(params LogEventProperty[] properties)
    {
        return new LogEvent(
            DateTimeOffset.UtcNow,
            LogEventLevel.Information,
            exception: null,
            messageTemplate: new Serilog.Parsing.MessageTemplate("test", []),
            properties: properties);
    }

    private sealed class TestLogEventPropertyFactory : ILogEventPropertyFactory
    {
        public LogEventProperty CreateProperty(string name, object? value, bool destructureObjects = false)
        {
            return new LogEventProperty(name, new ScalarValue(value));
        }
    }
}
