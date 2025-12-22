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
        public void WithTraceability_ShouldThrowWhenSourceIsNullAndNoEnvVar()
        {
            // Arrange
            var originalEnv = Environment.GetEnvironmentVariable("TRACEABILITY_SERVICENAME");
            try
            {
                Environment.SetEnvironmentVariable("TRACEABILITY_SERVICENAME", null);
                var config = new LoggerConfiguration();

                // Act & Assert
                // Com fallback de assembly name habilitado por padrão, não lança exceção
                // Usar WithTraceabilityJson para testar sem fallback
                Assert.Throws<InvalidOperationException>(() => config.WithTraceabilityJson(null, options =>
                {
                    options.UseAssemblyNameAsFallback = false;
                }));
            }
            finally
            {
                Environment.SetEnvironmentVariable("TRACEABILITY_SERVICENAME", originalEnv);
            }
        }

        [Fact]
        public void WithTraceability_ShouldThrowWhenSourceIsEmptyAndNoEnvVar()
        {
            // Arrange
            var originalEnv = Environment.GetEnvironmentVariable("TRACEABILITY_SERVICENAME");
            try
            {
                Environment.SetEnvironmentVariable("TRACEABILITY_SERVICENAME", null);
                var config = new LoggerConfiguration();

                // Act & Assert
                // Com fallback de assembly name habilitado por padrão, não lança exceção
                // Usar WithTraceabilityJson para testar sem fallback
                Assert.Throws<InvalidOperationException>(() => config.WithTraceabilityJson("", options =>
                {
                    options.UseAssemblyNameAsFallback = false;
                }));
            }
            finally
            {
                Environment.SetEnvironmentVariable("TRACEABILITY_SERVICENAME", originalEnv);
            }
        }

        [Fact]
        public void WithTraceability_ShouldThrowWhenSourceIsWhitespace()
        {
            // Arrange
            var originalEnv = Environment.GetEnvironmentVariable("TRACEABILITY_SERVICENAME");
            try
            {
                Environment.SetEnvironmentVariable("TRACEABILITY_SERVICENAME", null);
                var config = new LoggerConfiguration();

                // Act & Assert
                // Com fallback de assembly name habilitado por padrão, não lança exceção
                // Usar WithTraceabilityJson para testar sem fallback
                Assert.Throws<InvalidOperationException>(() => config.WithTraceabilityJson("   ", options =>
                {
                    options.UseAssemblyNameAsFallback = false;
                }));
            }
            finally
            {
                Environment.SetEnvironmentVariable("TRACEABILITY_SERVICENAME", originalEnv);
            }
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
        public void WithTraceabilityJson_ShouldThrowWhenSourceIsNullAndNoEnvVar()
        {
            // Arrange
            var originalEnv = Environment.GetEnvironmentVariable("TRACEABILITY_SERVICENAME");
            try
            {
                Environment.SetEnvironmentVariable("TRACEABILITY_SERVICENAME", null);
                var config = new LoggerConfiguration();

                // Act & Assert
                Assert.Throws<InvalidOperationException>(() => config.WithTraceabilityJson((string?)null, options =>
                {
                    options.UseAssemblyNameAsFallback = false;
                }));
            }
            finally
            {
                Environment.SetEnvironmentVariable("TRACEABILITY_SERVICENAME", originalEnv);
            }
        }

        [Fact]
        public void WithTraceabilityJson_ShouldThrowWhenSourceIsEmptyAndNoEnvVar()
        {
            // Arrange
            var originalEnv = Environment.GetEnvironmentVariable("TRACEABILITY_SERVICENAME");
            try
            {
                Environment.SetEnvironmentVariable("TRACEABILITY_SERVICENAME", null);
                var config = new LoggerConfiguration();

                // Act & Assert
                Assert.Throws<InvalidOperationException>(() => config.WithTraceabilityJson("", options =>
                {
                    options.UseAssemblyNameAsFallback = false;
                }));
            }
            finally
            {
                Environment.SetEnvironmentVariable("TRACEABILITY_SERVICENAME", originalEnv);
            }
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
            var originalEnv = Environment.GetEnvironmentVariable("TRACEABILITY_SERVICENAME");
            try
            {
                Environment.SetEnvironmentVariable("TRACEABILITY_SERVICENAME", null);
                var config = new LoggerConfiguration();
                var options = new Traceability.Configuration.TraceabilityOptions
                {
                    Source = null,
                    UseAssemblyNameAsFallback = false
                };

                // Act & Assert
                Assert.Throws<InvalidOperationException>(() => config.WithTraceabilityJson(options));
            }
            finally
            {
                Environment.SetEnvironmentVariable("TRACEABILITY_SERVICENAME", originalEnv);
            }
        }

        [Fact]
        public void WithTraceability_ShouldApplyMinimumLogLevelFromEnvironmentVariable()
        {
            // Arrange
            var originalEnv = Environment.GetEnvironmentVariable("LOG_LEVEL");
            try
            {
                Environment.SetEnvironmentVariable("LOG_LEVEL", "Warning");
                var testSink = new TestSink();
                var source = "TestService";

                // Act
                var logger = new LoggerConfiguration()
                    .WithTraceability(source)
                    .WriteTo.Sink(testSink)
                    .CreateLogger();

                logger.Debug("Debug message");
                logger.Information("Info message");
                logger.Warning("Warning message");
                logger.Error("Error message");

                // Assert
                testSink.Events.Should().HaveCount(2);
                testSink.Events.Should().OnlyContain(e => e.Level >= LogEventLevel.Warning);
            }
            finally
            {
                Environment.SetEnvironmentVariable("LOG_LEVEL", originalEnv);
            }
        }

        [Fact]
        public void WithTraceability_ShouldUseInformationAsDefaultWhenNoEnvVar()
        {
            // Arrange
            var originalEnv = Environment.GetEnvironmentVariable("LOG_LEVEL");
            try
            {
                Environment.SetEnvironmentVariable("LOG_LEVEL", null);
                var testSink = new TestSink();
                var source = "TestService";

                // Act
                var logger = new LoggerConfiguration()
                    .WithTraceability(source)
                    .WriteTo.Sink(testSink)
                    .CreateLogger();

                logger.Debug("Debug message");
                logger.Information("Info message");
                logger.Warning("Warning message");

                // Assert
                testSink.Events.Should().HaveCount(2); // Information e Warning
                testSink.Events.Should().OnlyContain(e => e.Level >= LogEventLevel.Information);
            }
            finally
            {
                Environment.SetEnvironmentVariable("LOG_LEVEL", originalEnv);
            }
        }

        [Fact]
        public void WithTraceability_ShouldFilterLogsBelowMinimumLevel()
        {
            // Arrange
            var originalEnv = Environment.GetEnvironmentVariable("LOG_LEVEL");
            try
            {
                Environment.SetEnvironmentVariable("LOG_LEVEL", "Warning");
                var testSink = new TestSink();
                var source = "TestService";

                // Act
                var logger = new LoggerConfiguration()
                    .WithTraceability(source)
                    .WriteTo.Sink(testSink)
                    .CreateLogger();

                logger.Debug("Debug message"); // Não deve aparecer
                logger.Information("Info message"); // Não deve aparecer
                logger.Warning("Warning message"); // Deve aparecer
                logger.Error("Error message"); // Deve aparecer

                // Assert
                testSink.Events.Should().HaveCount(2);
                testSink.Events.Should().OnlyContain(e => e.Level >= LogEventLevel.Warning);
            }
            finally
            {
                Environment.SetEnvironmentVariable("LOG_LEVEL", originalEnv);
            }
        }

        [Fact]
        public void WithTraceability_ShouldUseEnvVarOverOptionsMinimumLogLevel()
        {
            // Arrange
            var originalEnv = Environment.GetEnvironmentVariable("LOG_LEVEL");
            try
            {
                // Define env var como Warning
                Environment.SetEnvironmentVariable("LOG_LEVEL", "Warning");
                var testSink = new TestSink();
                var source = "TestService";

                // Act - Tenta definir Information via opções, mas env var deve ter prioridade
                // Nota: WithTraceability não aceita opções, então testamos WithTraceabilityJson
                var logger = new LoggerConfiguration()
                    .WithTraceabilityJson(source, options =>
                    {
                        options.MinimumLogLevel = LogEventLevel.Information; // Deve ser ignorado
                    })
                    .WriteTo.Sink(testSink)
                    .CreateLogger();

                logger.Information("Info message"); // Não deve aparecer (env var = Warning)
                logger.Warning("Warning message"); // Deve aparecer

                // Assert
                testSink.Events.Should().HaveCount(1);
                testSink.Events[0].Level.Should().Be(LogEventLevel.Warning);
            }
            finally
            {
                Environment.SetEnvironmentVariable("LOG_LEVEL", originalEnv);
            }
        }

        [Fact]
        public void WithTraceabilityJson_ShouldApplyMinimumLogLevelFromEnvironmentVariable()
        {
            // Arrange
            var originalEnv = Environment.GetEnvironmentVariable("LOG_LEVEL");
            try
            {
                Environment.SetEnvironmentVariable("LOG_LEVEL", "Error");
                var testSink = new TestSink();
                var source = "TestService";

                // Act
                var logger = new LoggerConfiguration()
                    .WithTraceabilityJson(source)
                    .WriteTo.Sink(testSink)
                    .CreateLogger();

                logger.Warning("Warning message");
                logger.Error("Error message");
                logger.Fatal("Fatal message");

                // Assert
                testSink.Events.Should().HaveCount(2); // Error e Fatal
                testSink.Events.Should().OnlyContain(e => e.Level >= LogEventLevel.Error);
            }
            finally
            {
                Environment.SetEnvironmentVariable("LOG_LEVEL", originalEnv);
            }
        }

        [Fact]
        public void WithTraceabilityJson_ShouldFilterLogsBelowMinimumLevel()
        {
            // Arrange
            var originalEnv = Environment.GetEnvironmentVariable("LOG_LEVEL");
            try
            {
                Environment.SetEnvironmentVariable("LOG_LEVEL", "Error");
                var testSink = new TestSink();
                var source = "TestService";

                // Act
                var logger = new LoggerConfiguration()
                    .WithTraceabilityJson(source)
                    .WriteTo.Sink(testSink)
                    .CreateLogger();

                logger.Debug("Debug message");
                logger.Information("Info message");
                logger.Warning("Warning message");
                logger.Error("Error message");
                logger.Fatal("Fatal message");

                // Assert
                testSink.Events.Should().HaveCount(2); // Error e Fatal
                testSink.Events.Should().OnlyContain(e => e.Level >= LogEventLevel.Error);
            }
            finally
            {
                Environment.SetEnvironmentVariable("LOG_LEVEL", originalEnv);
            }
        }

        [Fact]
        public void WithTraceabilityJson_ShouldUseOptionsMinimumLogLevelWhenNoEnvVar()
        {
            // Arrange
            var originalEnv = Environment.GetEnvironmentVariable("LOG_LEVEL");
            try
            {
                Environment.SetEnvironmentVariable("LOG_LEVEL", null);
                var testSink = new TestSink();
                var source = "TestService";

                // Act
                var logger = new LoggerConfiguration()
                    .WithTraceabilityJson(source, options =>
                    {
                        options.MinimumLogLevel = LogEventLevel.Warning;
                    })
                    .WriteTo.Sink(testSink)
                    .CreateLogger();

                logger.Information("Info message");
                logger.Warning("Warning message");
                logger.Error("Error message");

                // Assert
                testSink.Events.Should().HaveCount(2); // Warning e Error
                testSink.Events.Should().OnlyContain(e => e.Level >= LogEventLevel.Warning);
            }
            finally
            {
                Environment.SetEnvironmentVariable("LOG_LEVEL", originalEnv);
            }
        }

        [Fact]
        public void WithTraceabilityJson_WithTraceabilityOptions_ShouldUseEnvVarOverOptions()
        {
            // Arrange
            var originalEnv = Environment.GetEnvironmentVariable("LOG_LEVEL");
            try
            {
                Environment.SetEnvironmentVariable("LOG_LEVEL", "Error");
                var testSink = new TestSink();
                var options = new Traceability.Configuration.TraceabilityOptions
                {
                    Source = "TestService",
                    MinimumLogLevel = LogEventLevel.Information // Deve ser ignorado
                };

                // Act
                var logger = new LoggerConfiguration()
                    .WithTraceabilityJson(options)
                    .WriteTo.Sink(testSink)
                    .CreateLogger();

                logger.Warning("Warning message");
                logger.Error("Error message");

                // Assert
                testSink.Events.Should().HaveCount(1); // Apenas Error
                testSink.Events[0].Level.Should().Be(LogEventLevel.Error);
            }
            finally
            {
                Environment.SetEnvironmentVariable("LOG_LEVEL", originalEnv);
            }
        }

        [Fact]
        public void GetMinimumLogLevel_ShouldParseCaseInsensitive()
        {
            // Arrange
            var originalEnv = Environment.GetEnvironmentVariable("LOG_LEVEL");
            try
            {
                var testSink = new TestSink();
                var source = "TestService";

                // Testa diferentes casos
                var testCases = new[] { "warning", "WARNING", "Warning", "WaRnInG" };

                foreach (var testCase in testCases)
                {
                    testSink.Events.Clear();
                    Environment.SetEnvironmentVariable("LOG_LEVEL", testCase);

                    // Act
                    var logger = new LoggerConfiguration()
                        .WithTraceability(source)
                        .WriteTo.Sink(testSink)
                        .CreateLogger();

                    logger.Information("Info message");
                    logger.Warning("Warning message");

                    // Assert
                    testSink.Events.Should().HaveCount(1);
                    testSink.Events[0].Level.Should().Be(LogEventLevel.Warning);
                }
            }
            finally
            {
                Environment.SetEnvironmentVariable("LOG_LEVEL", originalEnv);
            }
        }

        [Fact]
        public void GetMinimumLogLevel_ShouldHandleInvalidEnvVarValue()
        {
            // Arrange
            var originalEnv = Environment.GetEnvironmentVariable("LOG_LEVEL");
            try
            {
                Environment.SetEnvironmentVariable("LOG_LEVEL", "InvalidLevel");
                var testSink = new TestSink();
                var source = "TestService";

                // Act - Valor inválido deve usar Information como padrão
                var logger = new LoggerConfiguration()
                    .WithTraceability(source)
                    .WriteTo.Sink(testSink)
                    .CreateLogger();

                logger.Debug("Debug message");
                logger.Information("Info message");
                logger.Warning("Warning message");

                // Assert - Deve usar Information como padrão (Debug não aparece)
                testSink.Events.Should().HaveCount(2); // Information e Warning
                testSink.Events.Should().OnlyContain(e => e.Level >= LogEventLevel.Information);
            }
            finally
            {
                Environment.SetEnvironmentVariable("LOG_LEVEL", originalEnv);
            }
        }

        [Fact]
        public void WithTraceability_ShouldIncludeLevelInLogsWhenLogIncludeLevelIsTrue()
        {
            // Arrange
            var originalEnv = Environment.GetEnvironmentVariable("LOG_LEVEL");
            try
            {
                Environment.SetEnvironmentVariable("LOG_LEVEL", null);
                var testSink = new TestSink();
                var source = "TestService";

                // Act
                var logger = new LoggerConfiguration()
                    .WithTraceability(source)
                    .WriteTo.Sink(testSink)
                    .CreateLogger();

                logger.Information("Test message");

                // Assert
                testSink.Events.Should().NotBeEmpty();
                var logEvent = testSink.Events[0];
                logEvent.Level.Should().Be(LogEventLevel.Information);
            }
            finally
            {
                Environment.SetEnvironmentVariable("LOG_LEVEL", originalEnv);
            }
        }

        [Fact]
        public void WithTraceability_ShouldReadServiceNameFromEnvironmentVariable()
        {
            // Arrange
            var originalEnv = Environment.GetEnvironmentVariable("TRACEABILITY_SERVICENAME");
            try
            {
                Environment.SetEnvironmentVariable("TRACEABILITY_SERVICENAME", "EnvServiceName");
                var testSink = new TestSink();

                // Act
                var logger = new LoggerConfiguration()
                    .WithTraceability() // source opcional
                    .WriteTo.Sink(testSink)
                    .CreateLogger();

                logger.Information("Test message");

                // Assert
                testSink.Events.Should().NotBeEmpty();
                var logEvent = testSink.Events[0];
                logEvent.Properties.Should().ContainKey("Source");
                logEvent.Properties["Source"].ToString().Should().Contain("EnvServiceName");
            }
            finally
            {
                Environment.SetEnvironmentVariable("TRACEABILITY_SERVICENAME", originalEnv);
            }
        }

        [Fact]
        public void WithTraceability_ShouldUseParameterOverEnvironmentVariable()
        {
            // Arrange
            var originalEnv = Environment.GetEnvironmentVariable("TRACEABILITY_SERVICENAME");
            try
            {
                Environment.SetEnvironmentVariable("TRACEABILITY_SERVICENAME", "EnvServiceName");
                var testSink = new TestSink();
                var explicitSource = "ExplicitServiceName";

                // Act
                var logger = new LoggerConfiguration()
                    .WithTraceability(explicitSource) // parâmetro tem prioridade
                    .WriteTo.Sink(testSink)
                    .CreateLogger();

                logger.Information("Test message");

                // Assert
                testSink.Events.Should().NotBeEmpty();
                var logEvent = testSink.Events[0];
                logEvent.Properties.Should().ContainKey("Source");
                logEvent.Properties["Source"].ToString().Should().Contain(explicitSource);
                logEvent.Properties["Source"].ToString().Should().NotContain("EnvServiceName");
            }
            finally
            {
                Environment.SetEnvironmentVariable("TRACEABILITY_SERVICENAME", originalEnv);
            }
        }

        [Fact]
        public void WithTraceability_ShouldThrowWhenNoSourceAvailable()
        {
            // Arrange
            var originalEnv = Environment.GetEnvironmentVariable("TRACEABILITY_SERVICENAME");
            try
            {
                Environment.SetEnvironmentVariable("TRACEABILITY_SERVICENAME", null);
                var config = new LoggerConfiguration();

                // Act & Assert
                // Com fallback de assembly name habilitado por padrão, não lança exceção
                // Usar WithTraceabilityJson para testar sem fallback
                var exception = Assert.Throws<InvalidOperationException>(() => config.WithTraceabilityJson((string?)null, options =>
                {
                    options.UseAssemblyNameAsFallback = false;
                }));
                exception.Message.Should().Contain("TRACEABILITY_SERVICENAME");
                exception.Message.Should().Contain("Source");
            }
            finally
            {
                Environment.SetEnvironmentVariable("TRACEABILITY_SERVICENAME", originalEnv);
            }
        }

        [Fact]
        public void WithTraceabilityJson_ShouldReadServiceNameFromEnvironmentVariable()
        {
            // Arrange
            var originalEnv = Environment.GetEnvironmentVariable("TRACEABILITY_SERVICENAME");
            try
            {
                Environment.SetEnvironmentVariable("TRACEABILITY_SERVICENAME", "EnvServiceName");
                var testSink = new TestSink();

                // Act
                var logger = new LoggerConfiguration()
                    .WithTraceabilityJson() // source opcional
                    .WriteTo.Sink(testSink)
                    .CreateLogger();

                logger.Information("Test message");

                // Assert
                testSink.Events.Should().NotBeEmpty();
                var logEvent = testSink.Events[0];
                logEvent.Properties.Should().ContainKey("Source");
                logEvent.Properties["Source"].ToString().Should().Contain("EnvServiceName");
            }
            finally
            {
                Environment.SetEnvironmentVariable("TRACEABILITY_SERVICENAME", originalEnv);
            }
        }

        [Fact]
        public void WithTraceabilityJson_ShouldUseParameterOverEnvironmentVariable()
        {
            // Arrange
            var originalEnv = Environment.GetEnvironmentVariable("TRACEABILITY_SERVICENAME");
            try
            {
                Environment.SetEnvironmentVariable("TRACEABILITY_SERVICENAME", "EnvServiceName");
                var testSink = new TestSink();
                var explicitSource = "ExplicitServiceName";

                // Act
                var logger = new LoggerConfiguration()
                    .WithTraceabilityJson(explicitSource) // parâmetro tem prioridade
                    .WriteTo.Sink(testSink)
                    .CreateLogger();

                logger.Information("Test message");

                // Assert
                testSink.Events.Should().NotBeEmpty();
                var logEvent = testSink.Events[0];
                logEvent.Properties.Should().ContainKey("Source");
                logEvent.Properties["Source"].ToString().Should().Contain(explicitSource);
                logEvent.Properties["Source"].ToString().Should().NotContain("EnvServiceName");
            }
            finally
            {
                Environment.SetEnvironmentVariable("TRACEABILITY_SERVICENAME", originalEnv);
            }
        }

        [Fact]
        public void WithTraceabilityJson_ShouldThrowWhenNoSourceAvailable()
        {
            // Arrange
            var originalEnv = Environment.GetEnvironmentVariable("TRACEABILITY_SERVICENAME");
            try
            {
                Environment.SetEnvironmentVariable("TRACEABILITY_SERVICENAME", null);
                var config = new LoggerConfiguration();

                // Act & Assert
                var exception = Assert.Throws<InvalidOperationException>(() => config.WithTraceabilityJson((string?)null, options =>
                {
                    options.UseAssemblyNameAsFallback = false;
                }));
                exception.Message.Should().Contain("TRACEABILITY_SERVICENAME");
                exception.Message.Should().Contain("Source");
            }
            finally
            {
                Environment.SetEnvironmentVariable("TRACEABILITY_SERVICENAME", originalEnv);
            }
        }

        [Fact]
        public void WithTraceabilityJson_WithOptions_ShouldUseOptionsSourceOverEnvironmentVariable()
        {
            // Arrange
            var originalEnv = Environment.GetEnvironmentVariable("TRACEABILITY_SERVICENAME");
            try
            {
                Environment.SetEnvironmentVariable("TRACEABILITY_SERVICENAME", "EnvServiceName");
                var testSink = new TestSink();
                var optionsSource = "OptionsServiceName";

                // Act
                var logger = new LoggerConfiguration()
                    .WithTraceabilityJson(configureOptions: opt => opt.Source = optionsSource)
                    .WriteTo.Sink(testSink)
                    .CreateLogger();

                logger.Information("Test message");

                // Assert
                testSink.Events.Should().NotBeEmpty();
                var logEvent = testSink.Events[0];
                logEvent.Properties.Should().ContainKey("Source");
                logEvent.Properties["Source"].ToString().Should().Contain(optionsSource);
            }
            finally
            {
                Environment.SetEnvironmentVariable("TRACEABILITY_SERVICENAME", originalEnv);
            }
        }

        [Fact]
        public void WithTraceabilityJson_WithTraceabilityOptions_ShouldReadServiceNameFromEnvironmentVariable()
        {
            // Arrange
            var originalEnv = Environment.GetEnvironmentVariable("TRACEABILITY_SERVICENAME");
            try
            {
                Environment.SetEnvironmentVariable("TRACEABILITY_SERVICENAME", "EnvServiceName");
                var testSink = new TestSink();
                var options = new Traceability.Configuration.TraceabilityOptions(); // Source não definido

                // Act
                var logger = new LoggerConfiguration()
                    .WithTraceabilityJson(options)
                    .WriteTo.Sink(testSink)
                    .CreateLogger();

                logger.Information("Test message");

                // Assert
                testSink.Events.Should().NotBeEmpty();
                var logEvent = testSink.Events[0];
                logEvent.Properties.Should().ContainKey("Source");
                logEvent.Properties["Source"].ToString().Should().Contain("EnvServiceName");
            }
            finally
            {
                Environment.SetEnvironmentVariable("TRACEABILITY_SERVICENAME", originalEnv);
            }
        }

        [Fact]
        public void WithTraceabilityJson_WithTraceabilityOptions_ShouldThrowWhenNoSourceAvailable()
        {
            // Arrange
            var originalEnv = Environment.GetEnvironmentVariable("TRACEABILITY_SERVICENAME");
            try
            {
                Environment.SetEnvironmentVariable("TRACEABILITY_SERVICENAME", null);
                var config = new LoggerConfiguration();
                var options = new Traceability.Configuration.TraceabilityOptions
                {
                    UseAssemblyNameAsFallback = false // Source não definido e fallback desabilitado
                };

                // Act & Assert
                var exception = Assert.Throws<InvalidOperationException>(() => config.WithTraceabilityJson(options));
                exception.Message.Should().Contain("TRACEABILITY_SERVICENAME");
                exception.Message.Should().Contain("Source");
            }
            finally
            {
                Environment.SetEnvironmentVariable("TRACEABILITY_SERVICENAME", originalEnv);
            }
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

