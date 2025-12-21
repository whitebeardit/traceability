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
    }

    // Helper class para criar ILogEventPropertyFactory
    internal class MockPropertyFactory : Serilog.Core.ILogEventPropertyFactory
    {
        public LogEventProperty CreateProperty(string name, object? value, bool destructureObjects = false)
        {
            return new LogEventProperty(name, new ScalarValue(value));
        }
    }
}

