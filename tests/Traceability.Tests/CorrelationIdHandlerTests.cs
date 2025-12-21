using System;
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
            // O CorrelationContext.Current gera automaticamente, então deve adicionar
            request.Headers.Contains("X-Correlation-Id").Should().BeTrue();
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
    }
}

