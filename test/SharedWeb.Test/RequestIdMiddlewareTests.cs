using Bit.SharedWeb.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace SharedWeb.Test;

public class RequestIdMiddlewareTests
{
    private readonly RequestDelegate _next;
    private readonly RequestIdMiddleware _middleware;
    private readonly CapturingLoggerProvider _loggerProvider;
    private readonly ILogger<RequestIdMiddleware> _logger;

    public RequestIdMiddlewareTests()
    {
        _next = Substitute.For<RequestDelegate>();
        _loggerProvider = new CapturingLoggerProvider();
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddProvider(_loggerProvider);
        });
        _logger = loggerFactory.CreateLogger<RequestIdMiddleware>();
        _middleware = new RequestIdMiddleware(_next, _logger);
    }

    [Fact]
    public async Task Invoke_WithIncomingHeader_PreservesAndEchoesRequestId()
    {
        const string requestId = "incoming-request-id";
        var context = CreateContext();
        context.Request.Headers[RequestIdMiddleware.HeaderName] = requestId;

        await _middleware.Invoke(context);

        Assert.Equal(requestId, context.Items[RequestIdMiddleware.HttpContextItemKey]);
        Assert.Equal(requestId, context.Response.Headers[RequestIdMiddleware.HeaderName].ToString());
        await _next.Received(1).Invoke(context);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Invoke_WithEmptyIncomingHeader_GeneratesAndEchoesRequestId(string headerValue)
    {
        var context = CreateContext();
        context.Request.Headers[RequestIdMiddleware.HeaderName] = headerValue;

        await _middleware.Invoke(context);

        var requestId = Assert.IsType<string>(context.Items[RequestIdMiddleware.HttpContextItemKey]);
        Assert.True(Guid.TryParse(requestId, out _));
        Assert.Equal(requestId, context.Response.Headers[RequestIdMiddleware.HeaderName].ToString());
        await _next.Received(1).Invoke(context);
    }

    [Fact]
    public async Task Invoke_WithoutIncomingHeader_GeneratesAndEchoesRequestId()
    {
        var context = CreateContext();

        await _middleware.Invoke(context);

        var requestId = Assert.IsType<string>(context.Items[RequestIdMiddleware.HttpContextItemKey]);
        Assert.True(Guid.TryParse(requestId, out _));
        Assert.Equal(requestId, context.Response.Headers[RequestIdMiddleware.HeaderName].ToString());
        await _next.Received(1).Invoke(context);
    }

    [Fact]
    public async Task Invoke_LogsWithinPipelineIncludeRequestId()
    {
        const string requestId = "log-request-id";
        var context = CreateContext();
        context.Request.Headers[RequestIdMiddleware.HeaderName] = requestId;

        _next.When(next => next.Invoke(Arg.Any<HttpContext>()))
            .Do(_ =>
            {
                var pipelineLogger = _loggerProvider.CreateLogger("Pipeline");
                pipelineLogger.LogInformation("Handled request");
            });

        await _middleware.Invoke(context);

        var logEntry = Assert.Single(_loggerProvider.Entries);
        Assert.Equal(requestId, logEntry.ScopeProperties[RequestIdMiddleware.LogPropertyName]);
    }

    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext
        {
            Request =
            {
                Path = "/test",
            },
        };
        context.Response.Body = new MemoryStream();
        return context;
    }

    private sealed class CapturingLoggerProvider : ILoggerProvider, ISupportExternalScope
    {
        private IExternalScopeProvider? _scopeProvider;

        public List<LogEntry> Entries { get; } = [];

        public ILogger CreateLogger(string categoryName) =>
            new CapturingLogger(categoryName, Entries, _scopeProvider ?? throw new InvalidOperationException("Scope provider is not configured."));

        public void Dispose()
        {
        }

        public ILogger<T> CreateLogger<T>() => (ILogger<T>)CreateLogger(typeof(T).FullName ?? typeof(T).Name);

        public void SetScopeProvider(IExternalScopeProvider scopeProvider)
        {
            _scopeProvider = scopeProvider;
        }
    }

    private sealed class CapturingLogger(
        string categoryName,
        List<LogEntry> entries,
        IExternalScopeProvider scopeProvider) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull =>
            scopeProvider.Push(state);

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var scopeProperties = new Dictionary<string, object?>();
            scopeProvider.ForEachScope((scope, state) =>
            {
                if (scope is IEnumerable<KeyValuePair<string, object?>> scopeValues)
                {
                    foreach (var property in scopeValues)
                    {
                        state[property.Key] = property.Value;
                    }
                }
            }, scopeProperties);

            entries.Add(new LogEntry(
                categoryName,
                logLevel,
                formatter(state, exception),
                scopeProperties));
        }
    }

    private sealed record LogEntry(
        string CategoryName,
        LogLevel LogLevel,
        string Message,
        IReadOnlyDictionary<string, object?> ScopeProperties);
}
