using Microsoft.AspNetCore.Http;

namespace Bit.SharedWeb.Utilities;

public static class RequestIdContext
{
    public const string HeaderName = "X-Request-ID";
    public const string HttpContextItemKey = "RequestId";
    public const string LogPropertyName = "RequestId";

    public static string? GetRequestId(HttpContext context)
    {
        return context.Items[HttpContextItemKey] as string;
    }
}
