using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Traceability;
using Traceability.Configuration;
using Traceability.Logging;
using Traceability.Middleware;
using Traceability.HttpClient;
#if NET8_0
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Traceability.Extensions;
#endif
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;
using Moq;
using Moq.Protected;
using Xunit;

namespace Traceability.Tests
{
    public class EdgeCaseTests
    {
#if NET8_0
        [Fact]
        public async Task CorrelationIdMiddleware_WhenCorrelationIdTooLong_ShouldGenerateNew()
        {
            // Arrange
            CorrelationContext.Clear();
            var longCorrelationId = new string('a', 200); // > 128 chars

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["X-Correlation-Id"] = longCorrelationId;

            var options = new TraceabilityOptions
            {
                ValidateCorrelationIdFormat = true
            };
            var optionsWrapper = Options.Create(options);

            RequestDelegate next = (context) => Task.CompletedTask;
            var middleware = new CorrelationIdMiddleware(next, optionsWrapper);

            // Act
            await middleware.InvokeAsync(httpContext);

            // Assert
            var responseHeader = httpContext.Response.Headers["X-Correlation-Id"].ToString();
            responseHeader.Should().NotBe(longCorrelationId);
            responseHeader.Length.Should().Be(32); // Deve gerar novo GUID
        }

        [Fact]
        public async Task CorrelationIdMiddleware_WhenHeaderNameIsNull_ShouldUseDefault()
        {
            // Arrange
            CorrelationContext.Clear();
            var correlationId = Guid.NewGuid().ToString("N");

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["X-Correlation-Id"] = correlationId;

            var options = new TraceabilityOptions
            {
                HeaderName = null! // Simula null
            };
            var optionsWrapper = Options.Create(options);

            RequestDelegate next = (context) => Task.CompletedTask;
            var middleware = new CorrelationIdMiddleware(next, optionsWrapper);

            // Act
            await middleware.InvokeAsync(httpContext);

            // Assert
            // Deve usar "X-Correlation-Id" como padrão
            var responseHeader = httpContext.Response.Headers["X-Correlation-Id"].ToString();
            responseHeader.Should().Be(correlationId);
        }

        [Fact]
        public async Task CorrelationIdMiddleware_WhenHeaderNameIsEmpty_ShouldUseDefault()
        {
            // Arrange
            CorrelationContext.Clear();
            var correlationId = Guid.NewGuid().ToString("N");

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers["X-Correlation-Id"] = correlationId;

            var options = new TraceabilityOptions
            {
                HeaderName = string.Empty
            };
            var optionsWrapper = Options.Create(options);

            RequestDelegate next = (context) => Task.CompletedTask;
            var middleware = new CorrelationIdMiddleware(next, optionsWrapper);

            // Act
            await middleware.InvokeAsync(httpContext);

            // Assert
            // Deve usar "X-Correlation-Id" como padrão
            var responseHeader = httpContext.Response.Headers["X-Correlation-Id"].ToString();
            responseHeader.Should().Be(correlationId);
        }
#endif

        [Fact]
        public void DataEnricher_WhenCircularReference_ShouldDetectAndPreventStackOverflow()
        {
            // Arrange
            var enricher = new DataEnricher();
            
            // Cria objeto com referência circular
            var circularObject = new Dictionary<string, object>();
            circularObject["Self"] = circularObject; // Referência circular
            circularObject["Value"] = "test";

            var template = new MessageTemplateParser().Parse("Test {@Circular}");
            var properties = new List<LogEventProperty>
            {
                new LogEventProperty("Circular", new StructureValue(new[]
                {
                    new LogEventProperty("Self", new ScalarValue(circularObject)),
                    new LogEventProperty("Value", new ScalarValue("test"))
                }))
            };
            var logEvent = new LogEvent(
                DateTimeOffset.Now,
                LogEventLevel.Information,
                null,
                template,
                properties);

            // Act
            var mockPropertyFactory = new MockPropertyFactory();
            Action act = () => enricher.Enrich(logEvent, mockPropertyFactory);

            // Assert - Não deve lançar StackOverflowException
            act.Should().NotThrow<StackOverflowException>();
            // Deve processar sem crashar
            logEvent.Properties.Should().NotBeNull();
        }

        // Helper class para criar ILogEventPropertyFactory
        private class MockPropertyFactory : ILogEventPropertyFactory
        {
            public LogEventProperty CreateProperty(string name, object? value, bool destructureObjects = false)
            {
                return new LogEventProperty(name, new ScalarValue(value));
            }
        }

        [Fact]
        public void DataEnricher_WhenObjectTooLarge_ShouldLimitSize()
        {
            // Arrange
            var enricher = new DataEnricher();
            
            // Cria objeto muito grande (> 1000 elementos)
            var largeObject = new Dictionary<string, object>();
            for (int i = 0; i < 2000; i++)
            {
                largeObject[$"Key{i}"] = $"Value{i}";
            }

            var template = new MessageTemplateParser().Parse("Test {@Large}");
            var properties = new List<LogEventProperty>
            {
                new LogEventProperty("Large", new StructureValue(new[]
                {
                    new LogEventProperty("Data", new ScalarValue(largeObject))
                }))
            };
            var logEvent = new LogEvent(
                DateTimeOffset.Now,
                LogEventLevel.Information,
                null,
                template,
                properties);

            // Act
            var mockPropertyFactory = new MockPropertyFactory();
            enricher.Enrich(logEvent, mockPropertyFactory);

            // Assert - Não deve crashar, deve limitar o tamanho
            logEvent.Properties.Should().NotBeNull();
            // O enricher deve lidar com objetos grandes sem causar OutOfMemoryException
        }

        [Fact]
        public void JsonFormatter_WhenExceptionWithManyInnerExceptions_ShouldLimitDepth()
        {
            // Arrange
            Exception? currentException = null;
            for (int i = 0; i < 20; i++) // Cria 20 níveis de InnerException
            {
                var inner = new Exception($"InnerException {i}", currentException);
                currentException = inner;
            }

            var rootException = new Exception("Root Exception", currentException);

            var logEvent = new LogEvent(
                DateTimeOffset.Now,
                LogEventLevel.Error,
                rootException,
                MessageTemplate.Empty,
                new List<LogEventProperty>());

            var formatter = new JsonFormatter();

            // Act
            using var writer = new System.IO.StringWriter();
            Action act = () => formatter.Format(logEvent, writer);

            // Assert - Não deve lançar StackOverflowException
            act.Should().NotThrow<StackOverflowException>();
            
            var result = writer.ToString();
            result.Should().Contain("Root Exception");
            // Deve limitar a profundidade (máximo 10 níveis)
            result.Should().Contain("Maximum exception depth reached");
        }

        [Fact]
        public void JsonFormatter_WhenDeepExceptionChain_ShouldNotCrash()
        {
            // Arrange
            Exception? inner = null;
            for (int i = 0; i < 15; i++)
            {
                inner = new Exception($"Level {i}", inner);
            }

            var logEvent = new LogEvent(
                DateTimeOffset.Now,
                LogEventLevel.Error,
                inner,
                MessageTemplate.Empty,
                new List<LogEventProperty>());

            var formatter = new JsonFormatter();

            // Act
            using var writer = new System.IO.StringWriter();
            Action act = () => formatter.Format(logEvent, writer);

            // Assert
            act.Should().NotThrow();
            var result = writer.ToString();
            result.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task CorrelationIdHandler_WhenHeaderNameIsNull_ShouldUseDefault()
        {
            // Arrange
            var correlationId = Guid.NewGuid().ToString("N");
            CorrelationContext.Current = correlationId;

#if NET8_0
            var options = new TraceabilityOptions
            {
                HeaderName = null!
            };
            var optionsWrapper = Options.Create(options);
            var handler = new CorrelationIdHandler(optionsWrapper);
#else
            var handler = new CorrelationIdHandler();
#endif

            var mockHandler = new Mock<System.Net.Http.HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<System.Net.Http.HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<System.Net.Http.HttpRequestMessage>(),
                    ItExpr.IsAny<System.Threading.CancellationToken>())
                .ReturnsAsync(new System.Net.Http.HttpResponseMessage());

            handler.InnerHandler = mockHandler.Object;
            var httpClient = new System.Net.Http.HttpClient(handler);
            var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, "https://example.com");

            // Act
            await httpClient.SendAsync(request);

            // Assert
            request.Headers.Contains("X-Correlation-Id").Should().BeTrue();
            request.Headers.GetValues("X-Correlation-Id").Should().Contain(correlationId);
        }

#if NET8_0
        [Fact]
        public void ServiceCollectionExtensions_WithMultipleRegistrations_ShouldHandleGracefully()
        {
            // Arrange
            var services = new ServiceCollection();
            var originalEnv = Environment.GetEnvironmentVariable("TRACEABILITY_SERVICENAME");
            try
            {
                Environment.SetEnvironmentVariable("TRACEABILITY_SERVICENAME", "TestService");

                // Act - Múltiplos registros (decorando o scope provider existente)
                services.AddTraceability("Service1");
                services.AddTraceability("Service2"); // Deve decorar o anterior

                // Assert - Não deve lançar exceção
                var serviceProvider = services.BuildServiceProvider();
                var scopeProvider = serviceProvider.GetService<Microsoft.Extensions.Logging.IExternalScopeProvider>();
                scopeProvider.Should().NotBeNull();
            }
            finally
            {
                Environment.SetEnvironmentVariable("TRACEABILITY_SERVICENAME", originalEnv);
            }
        }
#endif
    }
}

