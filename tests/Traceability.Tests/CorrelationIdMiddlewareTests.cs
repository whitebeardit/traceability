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
            string? correlationIdInNext = null;

            var nextCalled = false;
            RequestDelegate next = async (context) =>
            {
                nextCalled = true;
                // Verifica o correlation-id dentro do contexto do middleware
                correlationIdInNext = CorrelationContext.Current;
                await Task.CompletedTask;
            };

            var middleware = new CorrelationIdMiddleware(next);

            // Act
            await middleware.InvokeAsync(httpContext);

            // Assert
            // Verifica que o header da resposta contém o correlation-id esperado
            var responseHeader = httpContext.Response.Headers["X-Correlation-Id"].ToString();
            responseHeader.Should().Be(expectedCorrelationId);
            // Verifica que o correlation-id no contexto do middleware é o mesmo do header
            correlationIdInNext.Should().Be(expectedCorrelationId);
            nextCalled.Should().BeTrue();
        }

        [Fact]
        public async Task InvokeAsync_WhenHeaderDoesNotExist_ShouldGenerateNew()
        {
            // Arrange
            CorrelationContext.Clear();

            var httpContext = new DefaultHttpContext();
            string? correlationIdInNext = null;

            var nextCalled = false;
            RequestDelegate next = async (context) =>
            {
                nextCalled = true;
                // Verifica o correlation-id dentro do contexto do middleware
                correlationIdInNext = CorrelationContext.Current;
                await Task.CompletedTask;
            };

            var middleware = new CorrelationIdMiddleware(next);

            // Act
            await middleware.InvokeAsync(httpContext);

            // Assert
            // O header da resposta deve conter um correlation-id gerado
            var responseHeader = httpContext.Response.Headers["X-Correlation-Id"].ToString();
            responseHeader.Should().NotBeNullOrEmpty();
            responseHeader.Length.Should().Be(32); // GUID sem hífens tem 32 caracteres
            // O correlation-id no contexto do middleware deve ser o mesmo do header
            correlationIdInNext.Should().NotBeNullOrEmpty();
            correlationIdInNext.Should().Be(responseHeader);
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

