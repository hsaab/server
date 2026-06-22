using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Core.Utilities;

/// <summary>
/// Extension methods for propagating request identifiers on outbound HTTP clients.
/// </summary>
public static class HttpClientBuilderRequestIdExtensions
{
    /// <summary>
    /// Adds a delegating handler that forwards the current request's <c>X-Request-ID</c> header.
    /// </summary>
    public static IHttpClientBuilder AddRequestIdPropagation(this IHttpClientBuilder builder)
    {
        builder.AddHttpMessageHandler(sp =>
            new RequestIdDelegatingHandler(sp.GetRequiredService<IHttpContextAccessor>()));

        return builder;
    }
}
