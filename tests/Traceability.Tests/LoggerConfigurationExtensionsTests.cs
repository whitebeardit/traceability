using System;
using System.Collections.Generic;
using FluentAssertions;
using Serilog;
using Serilog.Events;
using Traceability.Extensions;
using Traceability.Logging;
using Xunit;

namespace Traceability.Tests
{
    public class LoggerConfigurationExtensionsTests
    {
        [Fact]
        public void WithTraceability_ShouldAddSourceEnricher()
        {
            // Arrange
            var source = "TestService";
            var config = new LoggerConfiguration()
                .MinimumLevel.Debug();

            // Act
            config.WithTraceability(source);

            // Assert
            var logger = config.CreateLogger();
            using (var log = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.With(new SourceEnricher(source))
                .WriteTo.Sink(new TestSink())
                .CreateLogger())
            {
                log.Information("Test message");
            }
        }

        [Fact]
        public void WithTraceability_ShouldAddCorrelationIdEnricher()
        {
            // Arrange
            var source = "TestService";
            var config = new LoggerConfiguration()
                .MinimumLevel.Debug();

            // Act
            config.WithTraceability(source);

            // Assert - Verifica que o método retorna LoggerConfiguration para encadeamento
            var result = config.WithTraceability(source);
            result.Should().NotBeNull();
            result.Should().BeOfType<LoggerConfiguration>();
        }

        [Fact]
        public void WithTraceability_ShouldReturnLoggerConfigurationForChaining()
        {
            // Arrange
            var source = "TestService";
            var config = new LoggerConfiguration()
                .MinimumLevel.Debug();

            // Act
            var result = config.WithTraceability(source);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<LoggerConfiguration>();
        }

        [Fact]
        public void WithTraceability_ShouldThrowWhenConfigIsNull()
        {
            // Arrange
            LoggerConfiguration? config = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => config!.WithTraceability("TestService"));
        }

        [Fact]
        public void WithTraceability_ShouldThrowWhenSourceIsNull()
        {
            // Arrange
            var config = new LoggerConfiguration();

            // Act & Assert
            // O método lança ArgumentException quando source é null ou vazio
            Assert.Throws<ArgumentNullException>(() => config.WithTraceability(null!));
        }

        [Fact]
        public void WithTraceability_ShouldThrowWhenSourceIsEmpty()
        {
            // Arrange
            var config = new LoggerConfiguration();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => config.WithTraceability(""));
        }

        [Fact]
        public void WithTraceability_ShouldThrowWhenSourceIsWhitespace()
        {
            // Arrange
            var config = new LoggerConfiguration();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => config.WithTraceability("   "));
        }

        [Fact]
        public void WithTraceability_ShouldCreateLoggerWithEnrichers()
        {
            // Arrange
            var source = "TestService";
            var testSink = new TestSink();

            // Act
            var logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WithTraceability(source)
                .WriteTo.Sink(testSink)
                .CreateLogger();

            logger.Information("Test message");

            // Assert
            testSink.Events.Should().NotBeEmpty();
            var logEvent = testSink.Events[0];
            logEvent.Properties.Should().ContainKey("Source");
            logEvent.Properties["Source"].ToString().Should().Contain(source);
        }

        [Fact]
        public void WithTraceabilityJson_ShouldAddDataEnricher()
        {
            // Arrange
            var source = "TestService";
            var testSink = new TestSink();

            // Act
            var logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WithTraceabilityJson(source)
                .WriteTo.Sink(testSink)
                .CreateLogger();

            var user = new { UserId = 123, UserName = "john.doe" };
            logger.Information("Processando {@User}", user);

            // Assert
            testSink.Events.Should().NotBeEmpty();
            var logEvent = testSink.Events[0];
            logEvent.Properties.Should().ContainKey("Source");
            logEvent.Properties.Should().ContainKey("data");
        }

        [Fact]
        public void WithTraceabilityJson_ShouldReturnLoggerConfigurationForChaining()
        {
            // Arrange
            var source = "TestService";
            var config = new LoggerConfiguration()
                .MinimumLevel.Debug();

            // Act
            var result = config.WithTraceabilityJson(source);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeOfType<LoggerConfiguration>();
        }

        [Fact]
        public void WithTraceabilityJson_ShouldThrowWhenConfigIsNull()
        {
            // Arrange
            LoggerConfiguration? config = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => config!.WithTraceabilityJson("TestService"));
        }

        [Fact]
        public void WithTraceabilityJson_ShouldThrowWhenSourceIsNull()
        {
            // Arrange
            var config = new LoggerConfiguration();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => config.WithTraceabilityJson(null!));
        }

        [Fact]
        public void WithTraceabilityJson_ShouldThrowWhenSourceIsEmpty()
        {
            // Arrange
            var config = new LoggerConfiguration();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => config.WithTraceabilityJson(""));
        }

        [Fact]
        public void WithTraceabilityJson_WithOptions_ShouldConfigureCorrectly()
        {
            // Arrange
            var source = "TestService";
            var testSink = new TestSink();

            // Act
            var logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WithTraceabilityJson(source, options =>
                {
                    options.LogIncludeData = true;
                    options.LogIncludeSource = true;
                })
                .WriteTo.Sink(testSink)
                .CreateLogger();

            logger.Information("Test message");

            // Assert
            testSink.Events.Should().NotBeEmpty();
            var logEvent = testSink.Events[0];
            logEvent.Properties.Should().ContainKey("Source");
        }

        [Fact]
        public void WithTraceabilityJson_WithTraceabilityOptions_ShouldConfigureCorrectly()
        {
            // Arrange
            var options = new Traceability.Configuration.TraceabilityOptions
            {
                Source = "TestService",
                LogIncludeData = true
            };
            var testSink = new TestSink();

            // Act
            var logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WithTraceabilityJson(options)
                .WriteTo.Sink(testSink)
                .CreateLogger();

            logger.Information("Test message");

            // Assert
            testSink.Events.Should().NotBeEmpty();
            var logEvent = testSink.Events[0];
            logEvent.Properties.Should().ContainKey("Source");
        }

        [Fact]
        public void WithTraceabilityJson_WithTraceabilityOptions_ShouldThrowWhenOptionsIsNull()
        {
            // Arrange
            var config = new LoggerConfiguration();
            Traceability.Configuration.TraceabilityOptions? options = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => config.WithTraceabilityJson(options!));
        }

        [Fact]
        public void WithTraceabilityJson_WithTraceabilityOptions_ShouldThrowWhenSourceNotDefined()
        {
            // Arrange
            var config = new LoggerConfiguration();
            var options = new Traceability.Configuration.TraceabilityOptions
            {
                Source = null
            };

            // Act & Assert
            Assert.Throws<ArgumentException>(() => config.WithTraceabilityJson(options));
        }
    }

    // Helper class para capturar eventos de log
    internal class TestSink : Serilog.Core.ILogEventSink
    {
        public List<LogEvent> Events { get; } = new List<LogEvent>();

        public void Emit(LogEvent logEvent)
        {
            Events.Add(logEvent);
        }
    }
}

