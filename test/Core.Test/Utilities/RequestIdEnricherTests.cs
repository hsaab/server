﻿using Bit.Core.Utilities;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Xunit;

namespace Bit.Core.Test.Utilities;

public class RequestIdEnricherTests
{
    [Fact]
    public void Enrich_WithCurrentRequestId_AddsRequestIdProperty()
    {
        var events = CreateLoggerAndLog("test-request-id");

        var logEvent = Assert.Single(events);
        Assert.True(logEvent.Properties.TryGetValue("RequestId", out var requestIdProperty));
        Assert.Equal("test-request-id", ((ScalarValue)requestIdProperty).Value);
    }

    [Fact]
    public void Enrich_WithoutCurrentRequestId_DoesNotAddProperty()
    {
        var events = CreateLoggerAndLog(null);

        var logEvent = Assert.Single(events);
        Assert.False(logEvent.Properties.ContainsKey("RequestId"));
    }

    [Fact]
    public void Enrich_WhenRequestIdAlreadyPresent_DoesNotOverwrite()
    {
        var events = new List<LogEvent>();
        var logger = new LoggerConfiguration()
            .Enrich.With(new RequestIdEnricher())
            .Enrich.WithProperty("RequestId", "existing-request-id")
            .WriteTo.Sink(new CollectingSink(events))
            .CreateLogger();

        RequestIdContext.CurrentRequestId = "new-request-id";

        try
        {
            logger.Information("hello");
        }
        finally
        {
            RequestIdContext.CurrentRequestId = null;
            logger.Dispose();
        }

        var logEvent = Assert.Single(events);
        Assert.True(logEvent.Properties.TryGetValue("RequestId", out var requestIdProperty));
        Assert.Equal("existing-request-id", ((ScalarValue)requestIdProperty).Value);
    }

    private static List<LogEvent> CreateLoggerAndLog(string? requestId)
    {
        var events = new List<LogEvent>();
        var logger = new LoggerConfiguration()
            .Enrich.With<RequestIdEnricher>()
            .WriteTo.Sink(new CollectingSink(events))
            .CreateLogger();

        RequestIdContext.CurrentRequestId = requestId;

        try
        {
            logger.Information("hello");
        }
        finally
        {
            RequestIdContext.CurrentRequestId = null;
            logger.Dispose();
        }

        return events;
    }

    private sealed class CollectingSink(List<LogEvent> events) : ILogEventSink
    {
        public void Emit(LogEvent logEvent) => events.Add(logEvent);
    }
}
