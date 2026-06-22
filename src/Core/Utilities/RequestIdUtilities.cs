using Microsoft.AspNetCore.Http;

namespace Bit.Core.Utilities;

public static class RequestIdUtilities
{
    public const string HeaderName = "X-Request-ID";
    public const string HttpContextItemKey = "RequestId";

    public static string GetOrCreateRequestId(HttpContext context)
    {
        if (context.Items.TryGetValue(HttpContextItemKey, out var existing)
            && existing is string existingId
            && !string.IsNullOrWhiteSpace(existingId))
        {
            RequestIdContext.Current = existingId;
            return existingId;
        }

        var requestId = GetIncomingRequestId(context) ?? Guid.NewGuid().ToString();
        context.Items[HttpContextItemKey] = requestId;
        context.TraceIdentifier = requestId;
        RequestIdContext.Current = requestId;
        return requestId;
    }

    public static string? GetRequestId(HttpContext? context)
    {
        if (context != null
            && context.Items.TryGetValue(HttpContextItemKey, out var item)
            && item is string requestId
            && !string.IsNullOrWhiteSpace(requestId))
        {
            return requestId;
        }

        return RequestIdContext.Current;
    }

    private static string? GetIncomingRequestId(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(HeaderName, out var headerValue))
        {
            return null;
        }

        var value = headerValue.ToString();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}

public static class RequestIdContext
{
    private static readonly AsyncLocal<string?> _current = new();

    public static string? Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }
}
