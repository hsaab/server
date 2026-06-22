using Microsoft.AspNetCore.Http;

namespace Bit.Core.Utilities;

public static class RequestIdUtilities
{
    public const string HeaderName = "X-Request-ID";
    public const string HttpContextItemKey = "RequestId";
    private const int MaxRequestIdLength = 128;

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

        return null;
    }

    private static string? GetIncomingRequestId(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(HeaderName, out var headerValue))
        {
            return null;
        }

        var value = headerValue.ToString();
        return IsValidRequestId(value) ? value : null;
    }

    internal static bool IsValidRequestId(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > MaxRequestIdLength)
        {
            return false;
        }

        foreach (var character in value)
        {
            if (character < 0x20 || character > 0x7E)
            {
                return false;
            }
        }

        return true;
    }

    internal static void TrySetResponseRequestId(HttpResponse response, string requestId)
    {
        try
        {
            response.Headers[HeaderName] = requestId;
        }
        catch
        {
            // Avoid masking upstream exceptions when the response header cannot be written.
        }
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
