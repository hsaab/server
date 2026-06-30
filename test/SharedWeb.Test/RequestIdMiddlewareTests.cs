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

    public RequestIdMiddlewareTests()
    {
        _next = Substitute.For<RequestDelegate>();
    }

    [Fact]
    public async Task Invoke_WithIncomingRequestId_PreservesRequestIdOnResponse()
    {
        var context = CreateContext();
        context.Request.Headers[RequestIdMiddleware.HeaderName] = "existing-request-id";

        var middleware = new RequestIdMiddleware(_ => Task.CompletedTask);
        await middleware.Invoke(context);

        Assert.Equal("existing-request-id", context.Response.Headers[RequestIdMiddleware.HeaderName].ToString());
    }

    [Fact]
    public async Task Invoke_WithMixedCaseHeader_PreservesRequestId()
    {
        var context = CreateContext();
        context.Request.Headers["x-request-id"] = "case-insensitive-id";

        var middleware = new RequestIdMiddleware(_ => Task.CompletedTask);
        await middleware.Invoke(context);

        Assert.Equal("case-insensitive-id", context.Response.Headers[RequestIdMiddleware.HeaderName].ToString());
    }

    [Fact]
    public async Task Invoke_WithoutRequestId_GeneratesNewRequestId()
    {
        var context = CreateContext();

        var middleware = new RequestIdMiddleware(_ => Task.CompletedTask);
        await middleware.Invoke(context);

        var requestId = context.Response.Headers[RequestIdMiddleware.HeaderName].ToString();
        Assert.False(string.IsNullOrWhiteSpace(requestId));
        Assert.True(Guid.TryParse(requestId, out _));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Invoke_WithEmptyRequestId_GeneratesNewRequestId(string requestIdHeader)
    {
        var context = CreateContext();
        context.Request.Headers[RequestIdMiddleware.HeaderName] = requestIdHeader;

        var middleware = new RequestIdMiddleware(_ => Task.CompletedTask);
        await middleware.Invoke(context);

        var requestId = context.Response.Headers[RequestIdMiddleware.HeaderName].ToString();
        Assert.True(Guid.TryParse(requestId, out _));
    }

    [Fact]
    public async Task Invoke_SetsRequestIdOnHttpContextAndAsyncLocal()
    {
        var context = CreateContext();
        context.Request.Headers[RequestIdMiddleware.HeaderName] = "trace-id";
        string? requestIdDuringRequest = null;

        var middleware = new RequestIdMiddleware(_ =>
        {
            requestIdDuringRequest = RequestIdContext.Current;
            return Task.CompletedTask;
        });

        await middleware.Invoke(context);

        Assert.Equal("trace-id", requestIdDuringRequest);
        Assert.Equal("trace-id", context.Items[RequestIdMiddleware.HttpContextItemKey]);
        Assert.Null(RequestIdContext.Current);
    }

    [Fact]
    public async Task Invoke_EnrichesLogsWithRequestIdForRequestLifetime()
    {
        var context = CreateContext();
        context.Request.Headers[RequestIdMiddleware.HeaderName] = "log-trace-id";
        LogEvent? capturedLogEvent = null;

        var middleware = new RequestIdMiddleware(_ =>
        {
            var enricher = new RequestIdEnricher();
            var logEvent = new LogEvent(
                DateTimeOffset.UtcNow,
                LogEventLevel.Information,
                null,
                new MessageTemplate("test", new List<MessageTemplateToken>()),
                new List<LogEventProperty>());

            enricher.Enrich(logEvent, new FixedPropertyFactory());
            capturedLogEvent = logEvent;
            return Task.CompletedTask;
        });

        await middleware.Invoke(context);

        Assert.NotNull(capturedLogEvent);
        Assert.True(capturedLogEvent.Properties.TryGetValue("requestId", out var property));
        Assert.Equal("log-trace-id", ((ScalarValue)property).Value);
    }

    [Fact]
    public async Task Invoke_CallsNextMiddleware()
    {
        var context = CreateContext();
        var middleware = new RequestIdMiddleware(_next);

        await middleware.Invoke(context);

        await _next.Received(1).Invoke(context);
    }

    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private sealed class FixedPropertyFactory : ILogEventPropertyFactory
    {
        public LogEventProperty CreateProperty(string name, object? value, bool destructureObjects = false) =>
            new(name, new ScalarValue(value));
    }
}
