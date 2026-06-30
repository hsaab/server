using System.Collections;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

#nullable enable

namespace Bit.SharedWeb.Utilities;

/// <summary>
/// Reads or generates an <c>X-Request-ID</c> for distributed tracing and enriches structured logs
/// for the request lifecycle.
/// </summary>
public sealed class RequestIdMiddleware
{
    public const string HeaderName = "X-Request-ID";
    public const string HttpContextItemKey = "RequestId";
    public const string LogPropertyName = "requestId";

    private readonly RequestDelegate _next;
    private readonly ILogger<RequestIdMiddleware> _logger;

    public RequestIdMiddleware(RequestDelegate next, ILogger<RequestIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        var requestId = GetOrCreateRequestId(context);
        context.Items[HttpContextItemKey] = requestId;
        context.Response.Headers[HeaderName] = requestId;

        using (_logger.BeginScope(new RequestIdLogScope(requestId)))
        {
            await _next(context);
        }
    }

    internal static string GetOrCreateRequestId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(HeaderName, out var headerValue))
        {
            var requestId = headerValue.ToString();
            if (!string.IsNullOrWhiteSpace(requestId))
            {
                return requestId;
            }
        }

        return Guid.NewGuid().ToString();
    }

    private sealed class RequestIdLogScope : IReadOnlyList<KeyValuePair<string, object?>>
    {
        private string? _cachedToString;

        public RequestIdLogScope(string requestId)
        {
            RequestId = requestId;
        }

        public string RequestId { get; }

        public KeyValuePair<string, object?> this[int index]
        {
            get
            {
                if (index == 0)
                {
                    return new KeyValuePair<string, object?>(LogPropertyName, RequestId);
                }

                throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        public int Count => 1;

        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        {
            yield return this[0];
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public override string ToString()
        {
            _cachedToString ??= $"{LogPropertyName}:{RequestId}";
            return _cachedToString;
        }
    }
}
