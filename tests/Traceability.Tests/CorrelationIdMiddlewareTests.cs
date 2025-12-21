#if NET8_0
using System;
using System.Threading.Tasks;
using FluentAssertions;
using Traceability;
using Traceability.Middleware;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Traceability.Tests
{
    public class CorrelationIdMiddlewareTests
    {
        [Fact]
        public async Task InvokeAsync_WhenHeaderExists_ShouldUseHeaderValue()
        {
            // Arrange
            CorrelationContext.Clear();
            var expectedCorrelationId = Guid.NewGuid().ToString("N");

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["X-Correlation-Id"] = expectedCorrelationId;

            var nextCalled = false;
            RequestDelegate next = (context) =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            };

            var middleware = new CorrelationIdMiddleware(next);

            // Act
            await middleware.InvokeAsync(httpContext);

            // Assert
            CorrelationContext.Current.Should().Be(expectedCorrelationId);
            httpContext.Response.Headers["X-Correlation-Id"].ToString().Should().Be(expectedCorrelationId);
            nextCalled.Should().BeTrue();
        }

        [Fact]
        public async Task InvokeAsync_WhenHeaderDoesNotExist_ShouldGenerateNew()
        {
            // Arrange
            CorrelationContext.Clear();

            var httpContext = new DefaultHttpContext();

            var nextCalled = false;
            RequestDelegate next = (context) =>
            {
                nextCalled = true;
                return Task.CompletedTask;
            };

            var middleware = new CorrelationIdMiddleware(next);

            // Act
            await middleware.InvokeAsync(httpContext);

            // Assert
            var correlationId = CorrelationContext.Current;
            correlationId.Should().NotBeNullOrEmpty();
            httpContext.Response.Headers["X-Correlation-Id"].ToString().Should().Be(correlationId);
            nextCalled.Should().BeTrue();
        }

        [Fact]
        public async Task InvokeAsync_ShouldAddCorrelationIdToResponseHeader()
        {
            // Arrange
            CorrelationContext.Clear();
            var correlationId = Guid.NewGuid().ToString("N");
            CorrelationContext.Current = correlationId;

            var httpContext = new DefaultHttpContext();

            RequestDelegate next = (context) => Task.CompletedTask;
            var middleware = new CorrelationIdMiddleware(next);

            // Act
            await middleware.InvokeAsync(httpContext);

            // Assert
            httpContext.Response.Headers["X-Correlation-Id"].ToString().Should().Be(correlationId);
        }
    }
}
#endif

