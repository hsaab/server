using Bit.SharedWeb.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace SharedWeb.Test;

public class RequestIdMiddlewareTests
{
    private readonly RequestDelegate _next;
    private readonly RequestIdMiddleware _middleware;
    private readonly ScopeCapturingLogger _logger;

    public RequestIdMiddlewareTests()
    {
        _next = Substitute.For<RequestDelegate>();
        _logger = new ScopeCapturingLogger();
        _middleware = new RequestIdMiddleware(_next, _logger);
    }

    [Fact]
    public async Task Invoke_WithIncomingRequestId_UsesProvidedId()
    {
        const string requestId = "existing-request-id";
        var context = CreateContext();
        context.Request.Headers[RequestIdMiddleware.HeaderName] = requestId;

        await _middleware.Invoke(context);

        Assert.Equal(requestId, context.Items[RequestIdMiddleware.HttpContextItemKey]);
        await AssertResponseHeaderAsync(context, requestId);
        await _next.Received(1).Invoke(context);
    }

    [Fact]
    public async Task Invoke_WithoutRequestId_GeneratesUuid()
    {
        var context = CreateContext();

        await _middleware.Invoke(context);

        var requestId = Assert.IsType<string>(context.Items[RequestIdMiddleware.HttpContextItemKey]);
        Assert.True(Guid.TryParse(requestId, out _));
        await AssertResponseHeaderAsync(context, requestId);
        await _next.Received(1).Invoke(context);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Invoke_WithEmptyRequestId_GeneratesUuid(string requestIdHeader)
    {
        var context = CreateContext();
        context.Request.Headers[RequestIdMiddleware.HeaderName] = requestIdHeader;

        await _middleware.Invoke(context);

        var requestId = Assert.IsType<string>(context.Items[RequestIdMiddleware.HttpContextItemKey]);
        Assert.True(Guid.TryParse(requestId, out _));
        await AssertResponseHeaderAsync(context, requestId);
    }

    [Fact]
    public async Task Invoke_TrimsIncomingRequestId()
    {
        const string requestId = "trimmed-request-id";
        var context = CreateContext();
        context.Request.Headers[RequestIdMiddleware.HeaderName] = $"  {requestId}  ";

        await _middleware.Invoke(context);

        Assert.Equal(requestId, context.Items[RequestIdMiddleware.HttpContextItemKey]);
        await AssertResponseHeaderAsync(context, requestId);
    }

    [Fact]
    public async Task Invoke_AddsRequestIdToLoggingScope()
    {
        const string requestId = "scope-request-id";
        var context = CreateContext();
        context.Request.Headers[RequestIdMiddleware.HeaderName] = requestId;

        await _middleware.Invoke(context);

        var scope = Assert.Single(_logger.Scopes);
        var scopeDictionary = Assert.IsAssignableFrom<IReadOnlyDictionary<string, object>>(scope);
        Assert.Equal(requestId, scopeDictionary[RequestIdMiddleware.HttpContextItemKey]);
    }

    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task AssertResponseHeaderAsync(HttpContext context, string expectedRequestId)
    {
        await context.Response.StartAsync();
        Assert.True(context.Response.Headers.TryGetValue(RequestIdMiddleware.HeaderName, out var headerValue));
        Assert.Equal(expectedRequestId, headerValue.ToString());
    }

    private sealed class ScopeCapturingLogger : ILogger<RequestIdMiddleware>
    {
        public List<object> Scopes { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            Scopes.Add(state!);
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
