using Bit.Core.Services;
using Bit.Core.Settings;
using Bit.SharedWeb.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace SharedWeb.Test;

public class RequestLoggingMiddlewareTests
{
    private readonly RequestDelegate _next;
    private readonly ScopeCapturingLogger _logger;
    private readonly RequestLoggingMiddleware _middleware;

    public RequestLoggingMiddlewareTests()
    {
        _next = Substitute.For<RequestDelegate>();
        _logger = new ScopeCapturingLogger();
        _middleware = new RequestLoggingMiddleware(
            _next,
            _logger,
            new GlobalSettings());
    }

    [Fact]
    public async Task Invoke_WithRequestIdInContext_IncludesRequestIdInLogScope()
    {
        const string requestId = "550e8400-e29b-41d4-a716-446655440000";
        var context = new DefaultHttpContext();
        context.Items[RequestIdConstants.HttpContextItemKey] = requestId;

        await _middleware.Invoke(context, Substitute.For<IFeatureService>());

        var scopeState = Assert.Single(_logger.Scopes);
        var scope = Assert.IsAssignableFrom<IReadOnlyList<KeyValuePair<string, object?>>>(scopeState);
        Assert.Equal(requestId, scope.First(pair => pair.Key == "RequestId").Value);
        await _next.Received(1).Invoke(context);
    }

    private sealed class ScopeCapturingLogger : ILogger<RequestLoggingMiddleware>
    {
        public List<object?> Scopes { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull
        {
            Scopes.Add(state);
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
