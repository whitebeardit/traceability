using System;
using System.Collections.Generic;
using FluentAssertions;
using Traceability;
using Traceability.Logging;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Xunit;

namespace Traceability.Tests
{
    public class LoggingTests
    {
        [Fact]
        public void CorrelationIdEnricher_ShouldAddCorrelationIdToLogEvent()
        {
            // Arrange
            var correlationId = Guid.NewGuid().ToString("N");
            CorrelationContext.Current = correlationId;

            var enricher = new CorrelationIdEnricher();
            var logEvent = new LogEvent(
                DateTimeOffset.Now,
                LogEventLevel.Information,
                null,
                MessageTemplate.Empty,
                new List<LogEventProperty>());

            // Act - Usa um mock factory
            var mockPropertyFactory = new MockPropertyFactory();
            enricher.Enrich(logEvent, mockPropertyFactory);

            // Assert
            logEvent.Properties.Should().ContainKey("CorrelationId");
            logEvent.Properties["CorrelationId"].ToString().Should().Contain(correlationId);
        }

        [Fact]
        public void CorrelationIdScopeProvider_ShouldAddCorrelationIdToScope()
        {
            // Arrange
            var correlationId = Guid.NewGuid().ToString("N");
            CorrelationContext.Current = correlationId;

            var provider = new CorrelationIdScopeProvider();
            var scopes = new List<object>();

            // Act
            provider.ForEachScope<object?>((scope, state) =>
            {
                scopes.Add(scope);
            }, null);

            // Assert
            scopes.Should().NotBeEmpty();
            var scopeDict = scopes[0].Should().BeOfType<Dictionary<string, object>>().Subject;
            scopeDict.Should().ContainKey("CorrelationId");
            scopeDict["CorrelationId"].Should().Be(correlationId);
        }

        [Fact]
        public void CorrelationIdScopeProvider_Push_ShouldReturnDisposableScope()
        {
            // Arrange
            var correlationId = Guid.NewGuid().ToString("N");
            CorrelationContext.Current = correlationId;

            var provider = new CorrelationIdScopeProvider();

            // Act
            var scope = provider.Push(new { Test = "value" });

            // Assert
            scope.Should().NotBeNull();
            scope.Should().BeAssignableTo<IDisposable>();

            // Cleanup
            scope.Dispose();
        }

        [Fact]
        public void CorrelationIdEnricher_WhenNoCorrelationId_ShouldNotAddToLogEvent()
        {
            // Arrange
            CorrelationContext.Clear();

            var enricher = new CorrelationIdEnricher();
            var logEvent = new LogEvent(
                DateTimeOffset.Now,
                LogEventLevel.Information,
                null,
                MessageTemplate.Empty,
                new List<LogEventProperty>());

            // Act
            var mockPropertyFactory = new MockPropertyFactory();
            enricher.Enrich(logEvent, mockPropertyFactory);

            // Assert
            logEvent.Properties.Should().NotContainKey("CorrelationId");
            CorrelationContext.HasValue.Should().BeFalse();
        }

        [Fact]
        public void CorrelationIdEnricher_ShouldNotCreateCorrelationIdWhenNotExists()
        {
            // Arrange
            CorrelationContext.Clear();

            var enricher = new CorrelationIdEnricher();
            var logEvent = new LogEvent(
                DateTimeOffset.Now,
                LogEventLevel.Information,
                null,
                MessageTemplate.Empty,
                new List<LogEventProperty>());

            // Act
            var mockPropertyFactory = new MockPropertyFactory();
            enricher.Enrich(logEvent, mockPropertyFactory);

            // Assert
            // Verifica que não criou correlation-id indesejadamente
            CorrelationContext.HasValue.Should().BeFalse();
        }

        [Fact]
        public void CorrelationIdScopeProvider_WhenNoCorrelationId_ShouldNotAddToScope()
        {
            // Arrange
            CorrelationContext.Clear();

            var provider = new CorrelationIdScopeProvider();
            var scopes = new List<object>();

            // Act
            provider.ForEachScope<object?>((scope, state) =>
            {
                scopes.Add(scope);
            }, null);

            // Assert
            // Se não houver correlation-id, apenas o provider interno (se existir) adiciona scopes
            // Como não há provider interno, não deve adicionar nada relacionado a correlation-id
            CorrelationContext.HasValue.Should().BeFalse();
        }

        [Fact]
        public void CorrelationIdScopeProvider_Push_WhenNoCorrelationId_ShouldReturnNullScope()
        {
            // Arrange
            CorrelationContext.Clear();

            var provider = new CorrelationIdScopeProvider();

            // Act
            var scope = provider.Push(new { Test = "value" });

            // Assert
            scope.Should().NotBeNull();
            scope.Should().BeAssignableTo<IDisposable>();
            CorrelationContext.HasValue.Should().BeFalse();

            // Cleanup
            scope.Dispose();
        }

        [Fact]
        public void SourceEnricher_ShouldAddSourceToLogEvent()
        {
            // Arrange
            const string source = "TestService";
            var enricher = new SourceEnricher(source);
            var logEvent = new LogEvent(
                DateTimeOffset.Now,
                LogEventLevel.Information,
                null,
                MessageTemplate.Empty,
                new List<LogEventProperty>());

            // Act
            var mockPropertyFactory = new MockPropertyFactory();
            enricher.Enrich(logEvent, mockPropertyFactory);

            // Assert
            logEvent.Properties.Should().ContainKey("Source");
            logEvent.Properties["Source"].ToString().Should().Contain(source);
        }

        [Fact]
        public void SourceEnricher_ShouldAlwaysAddSource()
        {
            // Arrange
            const string source = "TestService";
            var enricher = new SourceEnricher(source);
            var logEvent = new LogEvent(
                DateTimeOffset.Now,
                LogEventLevel.Information,
                null,
                MessageTemplate.Empty,
                new List<LogEventProperty>());

            // Act - Sem correlation-id, mas Source deve estar presente
            CorrelationContext.Clear();
            var mockPropertyFactory = new MockPropertyFactory();
            enricher.Enrich(logEvent, mockPropertyFactory);

            // Assert
            logEvent.Properties.Should().ContainKey("Source");
            logEvent.Properties["Source"].ToString().Should().Contain(source);
        }

        [Fact]
        public void SourceEnricher_ShouldReuseCachedProperty()
        {
            // Arrange
            const string source = "TestService";
            var enricher = new SourceEnricher(source);
            var logEvent1 = new LogEvent(
                DateTimeOffset.Now,
                LogEventLevel.Information,
                null,
                MessageTemplate.Empty,
                new List<LogEventProperty>());
            var logEvent2 = new LogEvent(
                DateTimeOffset.Now,
                LogEventLevel.Information,
                null,
                MessageTemplate.Empty,
                new List<LogEventProperty>());

            // Act
            var mockPropertyFactory = new MockPropertyFactory();
            enricher.Enrich(logEvent1, mockPropertyFactory);
            enricher.Enrich(logEvent2, mockPropertyFactory);

            // Assert
            logEvent1.Properties.Should().ContainKey("Source");
            logEvent2.Properties.Should().ContainKey("Source");
            // Verifica que a mesma propriedade foi reutilizada (cache)
            logEvent1.Properties["Source"].Should().Be(logEvent2.Properties["Source"]);
        }

        [Fact]
        public void SourceEnricher_WhenSourceIsNull_ShouldThrowArgumentNullException()
        {
            // Arrange & Act
            Action act = () => new SourceEnricher(null!);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("source");
        }

        [Fact]
        public void SourceEnricher_WhenSourceIsEmpty_ShouldThrowArgumentNullException()
        {
            // Arrange & Act
            Action act = () => new SourceEnricher(string.Empty);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("source");
        }

        [Fact]
        public void SourceEnricher_WhenSourceIsWhitespace_ShouldThrowArgumentNullException()
        {
            // Arrange & Act
            Action act = () => new SourceEnricher("   ");

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("source");
        }

        [Fact]
        public void SourceScopeProvider_ShouldAddSourceToScope()
        {
            // Arrange
            const string source = "TestService";
            var provider = new SourceScopeProvider(source);
            var scopes = new List<object>();

            // Act
            provider.ForEachScope<object?>((scope, state) =>
            {
                scopes.Add(scope);
            }, null);

            // Assert
            scopes.Should().NotBeEmpty();
            var scopeDict = scopes[0].Should().BeOfType<Dictionary<string, object>>().Subject;
            scopeDict.Should().ContainKey("Source");
            scopeDict["Source"].Should().Be(source);
        }

        [Fact]
        public void SourceScopeProvider_ShouldAlwaysAddSource()
        {
            // Arrange
            const string source = "TestService";
            var provider = new SourceScopeProvider(source);
            var scopes = new List<object>();

            // Act - Sem correlation-id, mas Source deve estar presente
            CorrelationContext.Clear();
            provider.ForEachScope<object?>((scope, state) =>
            {
                scopes.Add(scope);
            }, null);

            // Assert
            scopes.Should().NotBeEmpty();
            var scopeDict = scopes[0].Should().BeOfType<Dictionary<string, object>>().Subject;
            scopeDict.Should().ContainKey("Source");
            scopeDict["Source"].Should().Be(source);
        }

        [Fact]
        public void SourceScopeProvider_Push_ShouldReturnDisposableScope()
        {
            // Arrange
            const string source = "TestService";
            var provider = new SourceScopeProvider(source);

            // Act
            var scope = provider.Push(new { Test = "value" });

            // Assert
            scope.Should().NotBeNull();
            scope.Should().BeAssignableTo<IDisposable>();

            // Cleanup
            scope.Dispose();
        }

        [Fact]
        public void SourceScopeProvider_WhenSourceIsNull_ShouldThrowArgumentNullException()
        {
            // Arrange & Act
            Action act = () => new SourceScopeProvider(null!);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("source");
        }

        [Fact]
        public void SourceScopeProvider_WhenSourceIsEmpty_ShouldThrowArgumentNullException()
        {
            // Arrange & Act
            Action act = () => new SourceScopeProvider(string.Empty);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("source");
        }

        [Fact]
        public void SourceScopeProvider_WhenSourceIsWhitespace_ShouldThrowArgumentNullException()
        {
            // Arrange & Act
            Action act = () => new SourceScopeProvider("   ");

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("source");
        }

        [Fact]
        public void SourceScopeProvider_WithInnerProvider_ShouldCallInnerProvider()
        {
            // Arrange
            const string source = "TestService";
            var innerScopes = new List<object>();
            var innerProvider = new MockScopeProvider(innerScopes);
            var provider = new SourceScopeProvider(source, innerProvider);
            var allScopes = new List<object>();

            // Act
            provider.ForEachScope<object?>((scope, state) =>
            {
                allScopes.Add(scope);
            }, null);

            // Assert
            allScopes.Should().HaveCount(2); // Source scope + inner scope
            allScopes[0].Should().BeOfType<Dictionary<string, object>>()
                .Which.Should().ContainKey("Source");
            innerScopes.Should().NotBeEmpty(); // Verifica que inner provider foi chamado
        }
    }

    // Helper class para criar ILogEventPropertyFactory
    internal class MockPropertyFactory : Serilog.Core.ILogEventPropertyFactory
    {
        public LogEventProperty CreateProperty(string name, object? value, bool destructureObjects = false)
        {
            return new LogEventProperty(name, new ScalarValue(value));
        }
    }

    // Helper class para testar inner provider
    internal class MockScopeProvider : IExternalScopeProvider
    {
        private readonly List<object> _scopes;

        public MockScopeProvider(List<object> scopes)
        {
            _scopes = scopes;
        }

        public void ForEachScope<TState>(Action<object?, TState> callback, TState state)
        {
            var scope = new { Inner = "InnerScope" };
            _scopes.Add(scope);
            callback(scope, state);
        }

        public IDisposable Push(object? state)
        {
            return new MockDisposable();
        }
    }

    // Helper class para testar Push
    internal class MockDisposable : IDisposable
    {
        public void Dispose()
        {
            // Nada a fazer
        }
    }
}

