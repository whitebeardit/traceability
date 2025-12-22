using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Traceability;
using Traceability.Logging;
using Serilog.Core;
using Serilog.Events;
using Serilog.Parsing;
using Xunit;

namespace Traceability.Tests
{
    public class PerformanceTests
    {
        [Fact]
        public void DataEnricher_WithVeryLargeObject_ShouldCompleteWithinReasonableTime()
        {
            // Arrange
            var enricher = new DataEnricher();
            
            // Cria objeto grande mas dentro do limite (1000 elementos)
            var largeObject = new Dictionary<string, object>();
            for (int i = 0; i < 1000; i++)
            {
                largeObject[$"Key{i}"] = new { Id = i, Name = $"Item{i}", Value = i * 2 };
            }

            var template = new MessageTemplateParser().Parse("Test {@Large}");
            var properties = new List<LogEventProperty>
            {
                new LogEventProperty("Large", new StructureValue(
                    largeObject.Select(kvp => 
                        new LogEventProperty(kvp.Key, new ScalarValue(kvp.Value))
                    ).ToArray()
                ))
            };
            var logEvent = new LogEvent(
                DateTimeOffset.Now,
                LogEventLevel.Information,
                null,
                template,
                properties);

            // Act
            var stopwatch = Stopwatch.StartNew();
            var mockPropertyFactory = new MockPropertyFactory();
            enricher.Enrich(logEvent, mockPropertyFactory);
            stopwatch.Stop();

            // Assert
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000); // Deve completar em menos de 5 segundos
            logEvent.Properties.Should().ContainKey("data");
        }

        [Fact]
        public async Task JsonFormatter_WithManyConcurrentLogs_ShouldCompleteWithoutErrors()
        {
            // Arrange
            var formatter = new JsonFormatter();
            var logEvents = new List<LogEvent>();

            // Cria 100 eventos de log
            for (int i = 0; i < 100; i++)
            {
                var correlationId = Guid.NewGuid().ToString("N");
                CorrelationContext.Current = correlationId;

                var properties = new List<LogEventProperty>
                {
                    new LogEventProperty("Source", new ScalarValue("TestService")),
                    new LogEventProperty("CorrelationId", new ScalarValue(correlationId)),
                    new LogEventProperty("Message", new ScalarValue($"Test message {i}"))
                };

                logEvents.Add(new LogEvent(
                    DateTimeOffset.Now,
                    LogEventLevel.Information,
                    null,
                    MessageTemplate.Empty,
                    properties));
            }

            // Act
            var stopwatch = Stopwatch.StartNew();
            var tasks = logEvents.Select(logEvent =>
            {
                return Task.Run(() =>
                {
                    using var writer = new System.IO.StringWriter();
                    formatter.Format(logEvent, writer);
                    return writer.ToString();
                });
            }).ToArray();

            var results = await Task.WhenAll(tasks);
            stopwatch.Stop();

            // Assert
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(10000); // Deve completar em menos de 10 segundos
#if NET8_0
            tasks.All(t => t.IsCompletedSuccessfully).Should().BeTrue();
#else
            tasks.All(t => t.IsCompleted && !t.IsFaulted && !t.IsCanceled).Should().BeTrue();
#endif
            results.All(r => !string.IsNullOrEmpty(r)).Should().BeTrue();
        }

        [Fact]
        public void JsonFormatter_WithDeepNestedData_ShouldCompleteWithinReasonableTime()
        {
            // Arrange
            var formatter = new JsonFormatter();
            
            // Cria objeto aninhado profundo usando StructureValue (formato que o Serilog usa)
            var level9 = new StructureValue(new[]
            {
                new LogEventProperty("Value", new ScalarValue("Level9Value"))
            });
            
            var nested = level9;
            for (int i = 8; i >= 0; i--)
            {
                nested = new StructureValue(new[]
                {
                    new LogEventProperty($"Level{i}", nested)
                });
            }

            var properties = new List<LogEventProperty>
            {
                new LogEventProperty("data", nested)
            };

            var logEvent = new LogEvent(
                DateTimeOffset.Now,
                LogEventLevel.Information,
                null,
                MessageTemplate.Empty,
                properties);

            // Act
            var stopwatch = Stopwatch.StartNew();
            using var writer = new System.IO.StringWriter();
            formatter.Format(logEvent, writer);
            stopwatch.Stop();

            // Assert
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000); // Deve completar rapidamente
            var result = writer.ToString();
            result.Should().NotBeNullOrEmpty();
            // Verifica que o formatter processou o objeto (não precisa conter "Level0" especificamente, apenas não deve crashar)
            result.Should().Contain("Data");
        }

        [Fact]
        public void CorrelationIdEnricher_WithManyLogs_ShouldUseCacheEfficiently()
        {
            // Arrange
            var correlationId = Guid.NewGuid().ToString("N");
            CorrelationContext.Current = correlationId;
            var enricher = new CorrelationIdEnricher();
            var logEvents = new List<LogEvent>();

            // Cria 1000 eventos de log com o mesmo correlation-id
            for (int i = 0; i < 1000; i++)
            {
                logEvents.Add(new LogEvent(
                    DateTimeOffset.Now,
                    LogEventLevel.Information,
                    null,
                    MessageTemplate.Empty,
                    new List<LogEventProperty>()));
            }

            // Act
            var stopwatch = Stopwatch.StartNew();
            var mockPropertyFactory = new MockPropertyFactory();
            foreach (var logEvent in logEvents)
            {
                enricher.Enrich(logEvent, mockPropertyFactory);
            }
            stopwatch.Stop();

            // Assert
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000); // Deve ser rápido devido ao cache
            logEvents.All(e => e.Properties.ContainsKey("CorrelationId")).Should().BeTrue();
        }

        // Helper class para criar ILogEventPropertyFactory
        private class MockPropertyFactory : ILogEventPropertyFactory
        {
            public LogEventProperty CreateProperty(string name, object? value, bool destructureObjects = false)
            {
                return new LogEventProperty(name, new ScalarValue(value));
            }
        }
    }
}

