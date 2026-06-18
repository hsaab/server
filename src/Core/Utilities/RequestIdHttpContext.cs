using Microsoft.AspNetCore.Http;

namespace Bit.Core.Utilities;

public static class RequestIdHttpContext
{
    public const string ItemKey = "RequestId";

    public static void SetRequestId(HttpContext context, string requestId)
    {
        context.Items[ItemKey] = requestId;
    }

    public static string? GetRequestId(HttpContext context)
    {
        return context.Items[ItemKey] as string;
    }
}
