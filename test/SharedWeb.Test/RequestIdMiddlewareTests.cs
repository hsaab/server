using Bit.SharedWeb.Utilities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace SharedWeb.Test;

public class RequestIdMiddlewareTests
{
    private readonly RequestDelegate _next;
    private readonly RequestIdMiddleware _middleware;
    private readonly ScopeCapturingLoggerProvider _loggerProvider;

    public RequestIdMiddlewareTests()
    {
        _loggerProvider = new ScopeCapturingLoggerProvider();
        var loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(_loggerProvider));
        var logger = loggerFactory.CreateLogger<RequestIdMiddleware>();

        _next = Substitute.For<RequestDelegate>();
        _middleware = new RequestIdMiddleware(_next, logger);
    }

    [Fact]
    public async Task Invoke_WithoutIncomingHeader_GeneratesUuidInResponseAndHttpContext()
    {
        var context = new DefaultHttpContext();

        await _middleware.Invoke(context);

        var requestId = RequestIdContext.GetRequestId(context);
        Assert.NotNull(requestId);
        Assert.True(Guid.TryParse(requestId, out _));
        Assert.Equal(requestId, context.Response.Headers[RequestIdContext.HeaderName].ToString());
        await _next.Received(1).Invoke(context);
    }

    [Fact]
    public async Task Invoke_WithIncomingHeader_EchoesSameIdInResponseAndHttpContext()
    {
        const string incomingRequestId = "incoming-request-id-12345";
        var context = new DefaultHttpContext();
        context.Request.Headers[RequestIdContext.HeaderName] = incomingRequestId;

        await _middleware.Invoke(context);

        Assert.Equal(incomingRequestId, RequestIdContext.GetRequestId(context));
        Assert.Equal(incomingRequestId, context.Response.Headers[RequestIdContext.HeaderName].ToString());
        await _next.Received(1).Invoke(context);
    }

    [Fact]
    public async Task Invoke_WithoutIncomingHeader_IncludesRequestIdInLogScope()
    {
        var context = new DefaultHttpContext();
        string? scopedRequestId = null;

        _next.When(next => next.Invoke(context)).Do(_ =>
        {
            if (_loggerProvider.CurrentScopeState is Dictionary<string, object?> scope)
            {
                scopedRequestId = scope[RequestIdContext.LogPropertyName] as string;
            }
        });

        await _middleware.Invoke(context);

        var requestId = RequestIdContext.GetRequestId(context);
        Assert.NotNull(requestId);
        Assert.Equal(requestId, scopedRequestId);
    }

    [Fact]
    public async Task Invoke_WithIncomingHeader_IncludesRequestIdInLogScope()
    {
        const string incomingRequestId = "incoming-request-id-67890";
        var context = new DefaultHttpContext();
        context.Request.Headers[RequestIdContext.HeaderName] = incomingRequestId;
        string? scopedRequestId = null;

        _next.When(next => next.Invoke(context)).Do(_ =>
        {
            if (_loggerProvider.CurrentScopeState is Dictionary<string, object?> scope)
            {
                scopedRequestId = scope[RequestIdContext.LogPropertyName] as string;
            }
        });

        await _middleware.Invoke(context);

        Assert.Equal(incomingRequestId, scopedRequestId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Invoke_WithEmptyIncomingHeader_GeneratesUuid(string headerValue)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers[RequestIdContext.HeaderName] = headerValue;

        await _middleware.Invoke(context);

        var requestId = RequestIdContext.GetRequestId(context);
        Assert.NotNull(requestId);
        Assert.True(Guid.TryParse(requestId, out _));
    }

    private sealed class ScopeCapturingLoggerProvider : ILoggerProvider
    {
        public object? CurrentScopeState { get; private set; }

        public ILogger CreateLogger(string categoryName) => new ScopeCapturingLogger(this);

        public void Dispose()
        {
        }

        private sealed class ScopeCapturingLogger(ScopeCapturingLoggerProvider provider) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull
            {
                provider.CurrentScopeState = state;
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
        }

        private sealed class NullScope : IDisposable
        {
            public static NullScope Instance { get; } = new();

            public void Dispose()
            {
            }
        }
    }
}
