using Microsoft.AspNetCore.Http;
using Serilog.Context;

#nullable enable

namespace Bit.SharedWeb.Utilities;

/// <summary>
/// Propagates a shared <c>X-Request-ID</c> for distributed tracing across HTTP hops.
/// </summary>
public sealed class RequestIdMiddleware(RequestDelegate next)
{
    private const int MaxRequestIdLength = 256;

    public const string RequestIdHeaderName = "X-Request-ID";

    public async Task Invoke(HttpContext context)
    {
        if (!TryGetOrCreateRequestId(context, out var requestId))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { Error = $"{RequestIdHeaderName} header cannot exceed {MaxRequestIdLength} characters or contain control characters" });
            return;
        }

        context.Response.Headers[RequestIdHeaderName] = requestId;

        using (LogContext.PushProperty("RequestId", requestId))
        {
            await next(context);
        }
    }

    private static bool TryGetOrCreateRequestId(HttpContext context, out string requestId)
    {
        if (context.Request.Headers.TryGetValue(RequestIdHeaderName, out var value))
        {
            requestId = value.ToString();
            if (!string.IsNullOrWhiteSpace(requestId))
            {
                return requestId.Length <= MaxRequestIdLength && !ContainsControlCharacter(requestId);
            }
        }

        requestId = Guid.NewGuid().ToString();
        return true;
    }

    private static bool ContainsControlCharacter(string value)
    {
        foreach (var character in value)
        {
            if (char.IsControl(character))
            {
                return true;
            }
        }

        return false;
    }
}
