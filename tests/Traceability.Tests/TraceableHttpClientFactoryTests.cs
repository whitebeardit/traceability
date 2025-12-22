#if NET8_0
using System;
using FluentAssertions;
using Traceability.HttpClient;
using Microsoft.Extensions.Http;
using Moq;
using Xunit;

namespace Traceability.Tests
{
    public class TraceableHttpClientFactoryTests
    {
        [Fact]
        public void CreateFromFactory_ShouldReturnHttpClientFromFactory()
        {
            // Arrange
            var mockFactory = new Mock<IHttpClientFactory>();
            var expectedClient = new System.Net.Http.HttpClient();
            mockFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(expectedClient);

            // Act
            var httpClient = TraceableHttpClientFactory.CreateFromFactory(mockFactory.Object);

            // Assert
            httpClient.Should().NotBeNull();
            httpClient.Should().Be(expectedClient);
            mockFactory.Verify(f => f.CreateClient("DefaultTraceableClient"), Times.Once);
        }

        [Fact]
        public void CreateFromFactory_WithClientName_ShouldUseClientName()
        {
            // Arrange
            var clientName = "MyClient";
            var mockFactory = new Mock<IHttpClientFactory>();
            var expectedClient = new System.Net.Http.HttpClient();
            mockFactory.Setup(f => f.CreateClient(clientName)).Returns(expectedClient);

            // Act
            var httpClient = TraceableHttpClientFactory.CreateFromFactory(mockFactory.Object, clientName);

            // Assert
            httpClient.Should().NotBeNull();
            httpClient.Should().Be(expectedClient);
            mockFactory.Verify(f => f.CreateClient(clientName), Times.Once);
        }

        [Fact]
        public void CreateFromFactory_WithBaseAddress_ShouldSetBaseAddress()
        {
            // Arrange
            var baseAddress = "https://api.example.com";
            var mockFactory = new Mock<IHttpClientFactory>();
            var httpClient = new System.Net.Http.HttpClient();
            mockFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            // Act
            var result = TraceableHttpClientFactory.CreateFromFactory(mockFactory.Object, null, baseAddress);

            // Assert
            result.Should().NotBeNull();
            result.BaseAddress.Should().NotBeNull();
            result.BaseAddress!.ToString().Should().Be(baseAddress + "/");
        }

        [Fact]
        public void CreateFromFactory_WithNullFactory_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Action act = () => TraceableHttpClientFactory.CreateFromFactory(null!);
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("factory");
        }
    }
}
#endif

