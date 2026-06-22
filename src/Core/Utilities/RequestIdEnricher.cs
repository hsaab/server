using Serilog.Core;
using Serilog.Events;

namespace Bit.Core.Utilities;

/// <summary>
/// Adds the current request identifier to every Serilog event for the active HTTP request.
/// </summary>
public sealed class RequestIdEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var requestId = RequestIdContext.Current;
        if (string.IsNullOrWhiteSpace(requestId))
        {
            return;
        }

        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("requestId", requestId));
    }
}
