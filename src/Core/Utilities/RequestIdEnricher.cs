using Microsoft.AspNetCore.Http;
using Serilog.Core;
using Serilog.Events;

namespace Bit.Core.Utilities;

public sealed class RequestIdEnricher : ILogEventEnricher
{
    private static IHttpContextAccessor? _httpContextAccessor;

    public static void SetHttpContextAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (logEvent.Properties.ContainsKey(RequestIdConstants.LogPropertyName))
        {
            return;
        }

        var httpContext = _httpContextAccessor?.HttpContext;
        if (httpContext?.Items.TryGetValue(RequestIdConstants.ItemKey, out var requestId) == true
            && requestId is string requestIdValue
            && !string.IsNullOrEmpty(requestIdValue))
        {
            logEvent.AddPropertyIfAbsent(
                propertyFactory.CreateProperty(RequestIdConstants.LogPropertyName, requestIdValue));
        }
    }
}
