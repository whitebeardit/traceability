using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Traceability;
using Traceability.HttpClient;
using Moq;
using Moq.Protected;
using Xunit;

namespace Traceability.Tests
{
    public class CorrelationIdHandlerTests
    {
        [Fact]
        public async Task SendAsync_WhenCorrelationIdExists_ShouldAddToRequestHeaders()
        {
            // Arrange
            var correlationId = Guid.NewGuid().ToString("N");
            CorrelationContext.Current = correlationId;

            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage());

            var handler = new CorrelationIdHandler
            {
                InnerHandler = mockHandler.Object
            };

            var httpClient = new System.Net.Http.HttpClient(handler);
            var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");

            // Act
            await httpClient.SendAsync(request);

            // Assert
            request.Headers.Contains("X-Correlation-Id").Should().BeTrue();
            request.Headers.GetValues("X-Correlation-Id").Should().Contain(correlationId);
        }

        [Fact]
        public async Task SendAsync_WhenCorrelationIdDoesNotExist_ShouldNotAddToRequestHeaders()
        {
            // Arrange
            CorrelationContext.Clear();

            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage());

            var handler = new CorrelationIdHandler
            {
                InnerHandler = mockHandler.Object
            };

            var httpClient = new System.Net.Http.HttpClient(handler);
            var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");

            // Act
            await httpClient.SendAsync(request);

            // Assert
            // O handler não deve adicionar header quando não há correlation-id (não cria GUID indesejadamente)
            request.Headers.Contains("X-Correlation-Id").Should().BeFalse();
            CorrelationContext.HasValue.Should().BeFalse();
        }

        [Fact]
        public async Task SendAsync_ShouldNotCreateCorrelationIdWhenNotExists()
        {
            // Arrange
            CorrelationContext.Clear();

            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage());

            var handler = new CorrelationIdHandler
            {
                InnerHandler = mockHandler.Object
            };

            var httpClient = new System.Net.Http.HttpClient(handler);
            var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");

            // Act
            await httpClient.SendAsync(request);

            // Assert
            // Verifica que não criou correlation-id indesejadamente
            CorrelationContext.HasValue.Should().BeFalse();
        }

        [Fact]
        public async Task SendAsync_ShouldCallInnerHandler()
        {
            // Arrange
            var correlationId = Guid.NewGuid().ToString("N");
            CorrelationContext.Current = correlationId;

            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage());

            var handler = new CorrelationIdHandler
            {
                InnerHandler = mockHandler.Object
            };

            var httpClient = new System.Net.Http.HttpClient(handler);
            var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");

            // Act
            await httpClient.SendAsync(request);

            // Assert
            mockHandler.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
        }

#if NET8_0
        [Fact]
        public async Task SendAsync_Net8_Default_ShouldNotCreateHttpClientSpan_ButShouldPropagateHeaders()
        {
            // Arrange
            Environment.SetEnvironmentVariable("TRACEABILITY_NET8_HTTPCLIENT_SPANS_ENABLED", null);

            using var parent = new Activity("parent");
            parent.SetIdFormat(ActivityIdFormat.W3C);
            parent.Start();
            // CorrelationContext prioritizes Activity.TraceId; keep expectations aligned with that contract.
            var correlationId = parent.TraceId.ToString();

            HttpRequestMessage? capturedRequest = null;

            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
                .ReturnsAsync(new HttpResponseMessage());

            // No IOptions passed => Net8HttpClientSpansEnabled defaults false
            var handler = new CorrelationIdHandler
            {
                InnerHandler = mockHandler.Object
            };

            var httpClient = new System.Net.Http.HttpClient(handler);

            // Act
            await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://example.com"));

            // Assert
            capturedRequest.Should().NotBeNull();
            // capturedRequest já foi verificado como não-null acima
            capturedRequest!.Headers.Contains("X-Correlation-Id").Should().BeTrue();
            capturedRequest.Headers.GetValues("X-Correlation-Id").Should().Contain(correlationId);

            // traceparent should be propagated from parent activity (no new span created)
            capturedRequest!.Headers.Contains("traceparent").Should().BeTrue();
            capturedRequest.Headers.GetValues("traceparent").Should().Contain(parent.Id);

            Activity.Current.Should().Be(parent);
        }

        [Fact]
        public void Send_Net8_Default_ShouldNotCreateHttpClientSpan_ButShouldPropagateHeaders()
        {
            // Arrange
            Environment.SetEnvironmentVariable("TRACEABILITY_NET8_HTTPCLIENT_SPANS_ENABLED", null);

            using var parent = new Activity("parent");
            parent.SetIdFormat(ActivityIdFormat.W3C);
            parent.Start();

            // CorrelationContext prioritizes Activity.TraceId; keep expectations aligned with that contract.
            var correlationId = parent.TraceId.ToString();

            HttpRequestMessage? capturedRequest = null;

            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<HttpResponseMessage>(
                    "Send",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
                .Returns(new HttpResponseMessage());

            var handler = new CorrelationIdHandler
            {
                InnerHandler = mockHandler.Object
            };

            var httpClient = new System.Net.Http.HttpClient(handler);

            // Act
            httpClient.Send(new HttpRequestMessage(HttpMethod.Get, "https://example.com"));

            // Assert
            capturedRequest.Should().NotBeNull();
            capturedRequest!.Headers.Contains("X-Correlation-Id").Should().BeTrue();
            capturedRequest.Headers.GetValues("X-Correlation-Id").Should().Contain(correlationId);

            capturedRequest!.Headers.Contains("traceparent").Should().BeTrue();
            capturedRequest.Headers.GetValues("traceparent").Should().Contain(parent.Id);

            Activity.Current.Should().Be(parent);
        }
#endif
    }
}

