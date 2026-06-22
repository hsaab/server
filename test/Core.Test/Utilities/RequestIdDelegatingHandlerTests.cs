using Bit.Core.Utilities;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Xunit;

namespace Bit.Core.Test.Utilities;

public class RequestIdDelegatingHandlerTests
{
    [Fact]
    public async Task SendAsync_WithRequestContext_PropagatesRequestIdHeader()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Items[RequestIdUtilities.HttpContextItemKey] = "downstream-request-id";

        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);

        HttpRequestMessage? capturedRequest = null;
        var innerHandler = new CapturingHandler(request => capturedRequest = request);
        var handler = new RequestIdDelegatingHandler(httpContextAccessor)
        {
            InnerHandler = innerHandler
        };

        var invoker = new HttpMessageInvoker(handler);
        await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://example.com"), CancellationToken.None);

        Assert.NotNull(capturedRequest);
        Assert.True(capturedRequest.Headers.TryGetValues(RequestIdUtilities.HeaderName, out var values));
        Assert.Equal("downstream-request-id", Assert.Single(values));
    }

    [Fact]
    public async Task SendAsync_WithoutRequestContext_DoesNotAddRequestIdHeader()
    {
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns((HttpContext?)null);

        HttpRequestMessage? capturedRequest = null;
        var innerHandler = new CapturingHandler(request => capturedRequest = request);
        var handler = new RequestIdDelegatingHandler(httpContextAccessor)
        {
            InnerHandler = innerHandler
        };

        var invoker = new HttpMessageInvoker(handler);
        await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://example.com"), CancellationToken.None);

        Assert.NotNull(capturedRequest);
        Assert.False(capturedRequest.Headers.Contains(RequestIdUtilities.HeaderName));
    }

    [Fact]
    public async Task SendAsync_WithStaleAsyncLocalContext_DoesNotAddRequestIdHeader()
    {
        RequestIdContext.Current = "stale-request-id";
        try
        {
            var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
            httpContextAccessor.HttpContext.Returns((HttpContext?)null);

            HttpRequestMessage? capturedRequest = null;
            var innerHandler = new CapturingHandler(request => capturedRequest = request);
            var handler = new RequestIdDelegatingHandler(httpContextAccessor)
            {
                InnerHandler = innerHandler
            };

            var invoker = new HttpMessageInvoker(handler);
            await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://example.com"), CancellationToken.None);

            Assert.NotNull(capturedRequest);
            Assert.False(capturedRequest.Headers.Contains(RequestIdUtilities.HeaderName));
        }
        finally
        {
            RequestIdContext.Current = null;
        }
    }

    private sealed class CapturingHandler(Action<HttpRequestMessage> capture) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            capture(request);
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }
}
