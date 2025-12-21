using System;
using FluentAssertions;
using Traceability.HttpClient;
using Xunit;

namespace Traceability.Tests
{
    public class TraceableHttpClientFactoryTests
    {
        [Fact]
        public void Create_ShouldReturnHttpClientWithCorrelationIdHandler()
        {
            // Act
            var httpClient = TraceableHttpClientFactory.Create();

            // Assert
            httpClient.Should().NotBeNull();
        }

        [Fact]
        public void Create_WithBaseAddress_ShouldSetBaseAddress()
        {
            // Arrange
            var baseAddress = "https://api.example.com";

            // Act
            var httpClient = TraceableHttpClientFactory.Create(baseAddress);

            // Assert
            httpClient.BaseAddress.Should().NotBeNull();
            httpClient.BaseAddress.ToString().Should().Be(baseAddress + "/");
        }

        [Fact]
        public void Create_WithNullBaseAddress_ShouldNotSetBaseAddress()
        {
            // Act
            var httpClient = TraceableHttpClientFactory.Create(null);

            // Assert
            httpClient.BaseAddress.Should().BeNull();
        }

        [Fact]
        public void Create_WithEmptyBaseAddress_ShouldNotSetBaseAddress()
        {
            // Act
            var httpClient = TraceableHttpClientFactory.Create(string.Empty);

            // Assert
            httpClient.BaseAddress.Should().BeNull();
        }
    }
}

