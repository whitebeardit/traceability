#if NET48 || NET8_0
using System;
using System.Diagnostics;
using System.Net.Http;
using FluentAssertions;
using Traceability;
using Traceability.Core.Interfaces;
using Traceability.Core.Services;
using Xunit;
#if NET8_0
using Microsoft.AspNetCore.Http;
#endif

namespace Traceability.Tests
{
    public class HttpActivityTagProviderTests
    {
        private readonly IActivityTagProvider _tagProvider;

        public HttpActivityTagProviderTests()
        {
            _tagProvider = new HttpActivityTagProvider();
        }

        [Fact]
        public void AddRequestTags_ShouldAddCorrelationId_WhenCorrelationIdExists()
        {
            // Arrange
            CorrelationContext.Clear();
            var correlationId = Guid.NewGuid().ToString("N");
            CorrelationContext.Current = correlationId;

            using var activity = new Activity("Test");
            activity.Start();

#if NET8_0
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Method = "GET";
            httpContext.Request.Path = "/api/test";
            httpContext.Request.Scheme = "https";
            httpContext.Request.Host = new HostString("example.com");

            // Act
            _tagProvider.AddRequestTags(activity, httpContext);
#else
            var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/api/test");

            // Act
            _tagProvider.AddRequestTags(activity, request);
#endif

            // Assert
            var tagValue = activity.GetTagItem("correlation.id");
            tagValue.Should().NotBeNull();
            tagValue.Should().Be(correlationId);
        }

        [Fact]
        public void AddRequestTags_ShouldNotAddCorrelationId_WhenCorrelationIdDoesNotExist()
        {
            // Arrange
            CorrelationContext.Clear();

            using var activity = new Activity("Test");
            activity.Start();

#if NET8_0
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Method = "GET";
            httpContext.Request.Path = "/api/test";

            // Act
            _tagProvider.AddRequestTags(activity, httpContext);
#else
            var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com/api/test");

            // Act
            _tagProvider.AddRequestTags(activity, request);
#endif

            // Assert
            var tagValue = activity.GetTagItem("correlation.id");
            tagValue.Should().BeNull();
        }

        [Fact]
        public void AddRequestTags_ShouldAddCorrelationId_ForHttpContext()
        {
#if NET8_0
            // Arrange
            CorrelationContext.Clear();
            var correlationId = Guid.NewGuid().ToString("N");
            CorrelationContext.Current = correlationId;

            using var activity = new Activity("Test");
            activity.Start();

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Method = "POST";
            httpContext.Request.Path = "/api/users";
            httpContext.Request.Scheme = "https";
            httpContext.Request.Host = new HostString("api.example.com");

            // Act
            _tagProvider.AddRequestTags(activity, httpContext);

            // Assert
            activity.GetTagItem("correlation.id").Should().Be(correlationId);
            activity.GetTagItem("http.method").Should().Be("POST");
            activity.GetTagItem("http.url").Should().Be("/api/users");
            activity.GetTagItem("http.scheme").Should().Be("https");
            activity.GetTagItem("http.host").Should().Be("api.example.com");
#endif
        }

        [Fact]
        public void AddRequestTags_ShouldAddCorrelationId_ForHttpRequestMessage()
        {
            // Arrange
            CorrelationContext.Clear();
            var correlationId = Guid.NewGuid().ToString("N");
            CorrelationContext.Current = correlationId;

            using var activity = new Activity("Test");
            activity.Start();

            var request = new HttpRequestMessage(HttpMethod.Put, "https://api.example.com/users/123");

            // Act
            _tagProvider.AddRequestTags(activity, request);

            // Assert
            activity.GetTagItem("correlation.id").Should().Be(correlationId);
            activity.GetTagItem("http.method").Should().Be("PUT");
            activity.GetTagItem("http.url").Should().Be("https://api.example.com/users/123");
            activity.GetTagItem("http.scheme").Should().Be("https");
            activity.GetTagItem("http.host").Should().Be("api.example.com");
        }

        [Fact]
        public void AddRequestTags_ShouldNotAddCorrelationId_WhenCorrelationIdIsEmpty()
        {
            // Arrange
            CorrelationContext.Clear();
            CorrelationContext.Current = string.Empty;

            using var activity = new Activity("Test");
            activity.Start();

#if NET8_0
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Method = "GET";

            // Act
            _tagProvider.AddRequestTags(activity, httpContext);
#else
            var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");

            // Act
            _tagProvider.AddRequestTags(activity, request);
#endif

            // Assert
            var tagValue = activity.GetTagItem("correlation.id");
            tagValue.Should().BeNull();
        }

        [Fact]
        public void AddRequestTags_ShouldNotAddCorrelationId_WhenActivityIsNull()
        {
            // Arrange
            CorrelationContext.Clear();
            var correlationId = Guid.NewGuid().ToString("N");
            CorrelationContext.Current = correlationId;

#if NET8_0
            var httpContext = new DefaultHttpContext();

            // Act & Assert - não deve lançar exceção
            _tagProvider.AddRequestTags(null!, httpContext);
#else
            var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com");

            // Act & Assert - não deve lançar exceção
            _tagProvider.AddRequestTags(null!, request);
#endif
        }

        [Fact]
        public void AddRequestTags_ShouldNotAddCorrelationId_WhenRequestIsNull()
        {
            // Arrange
            CorrelationContext.Clear();
            var correlationId = Guid.NewGuid().ToString("N");
            CorrelationContext.Current = correlationId;

            using var activity = new Activity("Test");
            activity.Start();

            // Act & Assert - não deve lançar exceção
            _tagProvider.AddRequestTags(activity, null!);
        }
    }
}
#endif
