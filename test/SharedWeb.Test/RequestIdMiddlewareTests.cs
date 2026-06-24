using Bit.SharedWeb.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace SharedWeb.Test;

public class RequestIdMiddlewareTests
{
    private readonly ScopeCapturingLogger _logger;
    private readonly RequestIdMiddleware _middleware;

    public RequestIdMiddlewareTests()
    {
        _logger = new ScopeCapturingLogger();
        _middleware = new RequestIdMiddleware(StartResponseAsync, _logger);
    }

    [Fact]
    public async Task Invoke_WithoutIncomingHeader_GeneratesRequestIdOnResponse()
    {
        var context = CreateContext();

        await _middleware.Invoke(context);

        var requestId = Assert.Single(context.Response.Headers[RequestIdMiddleware.HeaderName]);
        Assert.True(Guid.TryParse(requestId, out _));
        Assert.Equal(requestId, context.GetRequestId());
    }

    [Fact]
    public async Task Invoke_WithIncomingHeader_PreservesAndEchoesRequestId()
    {
        const string incomingRequestId = "upstream-trace-id-12345";
        var context = CreateContext();
        context.Request.Headers[RequestIdMiddleware.HeaderName] = incomingRequestId;

        await _middleware.Invoke(context);

        var requestId = Assert.Single(context.Response.Headers[RequestIdMiddleware.HeaderName]);
        Assert.Equal(incomingRequestId, requestId);
        Assert.Equal(incomingRequestId, context.GetRequestId());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Invoke_WithEmptyIncomingHeader_GeneratesRequestId(string incomingRequestId)
    {
        var context = CreateContext();
        context.Request.Headers[RequestIdMiddleware.HeaderName] = incomingRequestId;

        await _middleware.Invoke(context);

        var requestId = Assert.Single(context.Response.Headers[RequestIdMiddleware.HeaderName]);
        Assert.True(Guid.TryParse(requestId, out _));
        Assert.Equal(requestId, context.GetRequestId());
    }

    [Fact]
    public async Task Invoke_AddsRequestIdToLogScope()
    {
        var context = CreateContext();
        context.Request.Headers[RequestIdMiddleware.HeaderName] = "scope-test-id";

        await _middleware.Invoke(context);

        var scope = Assert.Single(_logger.Scopes);
        Assert.Equal("scope-test-id", scope["RequestId"]);
    }

    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static Task StartResponseAsync(HttpContext context) => context.Response.StartAsync();

    private sealed class ScopeCapturingLogger : ILogger<RequestIdMiddleware>
    {
        public List<IReadOnlyDictionary<string, object?>> Scopes { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            if (state is IEnumerable<KeyValuePair<string, object?>> scopeState)
            {
                Scopes.Add(scopeState.ToDictionary(static pair => pair.Key, static pair => pair.Value));
            }

            return NullScope.Instance;
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
