using Bit.Core.Utilities;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;

namespace Bit.Core.Test.Utilities;

public class RequestIdEnricherTests
{
    [Fact]
    public void Enrich_WithRequestIdInHttpContext_AddsRequestIdProperty()
    {
        var expectedRequestId = Guid.NewGuid().ToString();
        var httpContext = new DefaultHttpContext();
        httpContext.Items[RequestIdConstants.ItemKey] = expectedRequestId;

        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);
        RequestIdEnricher.SetHttpContextAccessor(httpContextAccessor);

        var enricher = new RequestIdEnricher();
        var logEvent = CreateLogEvent();

        enricher.Enrich(logEvent, new PropertyFactory());

        Assert.True(logEvent.Properties.TryGetValue(RequestIdConstants.LogPropertyName, out var property));
        Assert.Equal(expectedRequestId, ((ScalarValue)property).Value);
    }

    [Fact]
    public void Enrich_WithoutRequestIdInHttpContext_DoesNotAddProperty()
    {
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(new DefaultHttpContext());
        RequestIdEnricher.SetHttpContextAccessor(httpContextAccessor);

        var enricher = new RequestIdEnricher();
        var logEvent = CreateLogEvent();

        enricher.Enrich(logEvent, new PropertyFactory());

        Assert.False(logEvent.Properties.ContainsKey(RequestIdConstants.LogPropertyName));
    }

    private static LogEvent CreateLogEvent()
    {
        return new LogEvent(
            DateTimeOffset.UtcNow,
            LogEventLevel.Information,
            exception: null,
            messageTemplate: new MessageTemplateParser().Parse("Test message"),
            properties: []);
    }

    private sealed class PropertyFactory : ILogEventPropertyFactory
    {
        public LogEventProperty CreateProperty(string name, object? value, bool destructureObjects = false)
        {
            return new LogEventProperty(name, new ScalarValue(value));
        }
    }
}
