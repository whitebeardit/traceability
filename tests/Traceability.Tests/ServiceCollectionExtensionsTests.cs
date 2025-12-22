#if NET8_0
using System;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Traceability.Configuration;
using Traceability.Extensions;
using Traceability.HttpClient;
using Traceability.Logging;
using Serilog;
using Xunit;

namespace Traceability.Tests
{
    public class ServiceCollectionExtensionsTests
    {
        [Fact]
        public void AddTraceability_ShouldRegisterCorrelationIdHandler()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddTraceability();

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var handler = serviceProvider.GetService<CorrelationIdHandler>();
            handler.Should().NotBeNull();
        }

        [Fact]
        public void AddTraceability_ShouldRegisterTraceabilityOptions()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddTraceability();

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var options = serviceProvider.GetService<IOptions<TraceabilityOptions>>();
            options.Should().NotBeNull();
            options!.Value.HeaderName.Should().Be("X-Correlation-Id");
        }

        [Fact]
        public void AddTraceability_ShouldRegisterIHttpClientFactory()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddTraceability();

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var factory = serviceProvider.GetService<IHttpClientFactory>();
            factory.Should().NotBeNull();
        }

        [Fact]
        public void AddTraceability_ShouldRegisterCorrelationIdScopeProvider()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddTraceability();

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var scopeProvider = serviceProvider.GetService<IExternalScopeProvider>();
            scopeProvider.Should().NotBeNull();
            scopeProvider.Should().BeOfType<CorrelationIdScopeProvider>();
        }

        [Fact]
        public void AddTraceability_WithSource_ShouldRegisterSourceScopeProvider()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddTraceability(options =>
            {
                options.Source = "TestService";
            });

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var scopeProvider = serviceProvider.GetService<IExternalScopeProvider>();
            scopeProvider.Should().NotBeNull();
            scopeProvider.Should().BeOfType<SourceScopeProvider>();
        }

        [Fact]
        public void AddTraceability_WithSourceOverload_ShouldRegisterSourceScopeProvider()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddTraceability("TestService");

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var scopeProvider = serviceProvider.GetService<IExternalScopeProvider>();
            scopeProvider.Should().NotBeNull();
            scopeProvider.Should().BeOfType<SourceScopeProvider>();

            var options = serviceProvider.GetService<IOptions<TraceabilityOptions>>();
            options!.Value.Source.Should().Be("TestService");
        }

        [Fact]
        public void AddTraceability_WithSourceOverloadAndOptions_ShouldConfigureBoth()
        {
            // Arrange
            var services = new ServiceCollection();
            var customHeader = "X-Custom-Id";

            // Act
            services.AddTraceability("TestService", options =>
            {
                options.HeaderName = customHeader;
            });

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var options = serviceProvider.GetService<IOptions<TraceabilityOptions>>();
            options!.Value.Source.Should().Be("TestService");
            options.Value.HeaderName.Should().Be(customHeader);
        }

        [Fact]
        public void AddTraceability_WithCustomOptions_ShouldApplyConfiguration()
        {
            // Arrange
            var services = new ServiceCollection();
            var customHeader = "X-Custom-Id";

            // Act
            services.AddTraceability(options =>
            {
                options.HeaderName = customHeader;
                options.ValidateCorrelationIdFormat = true;
                options.AlwaysGenerateNew = true;
            });

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var options = serviceProvider.GetService<IOptions<TraceabilityOptions>>();
            options!.Value.HeaderName.Should().Be(customHeader);
            options.Value.ValidateCorrelationIdFormat.Should().BeTrue();
            options.Value.AlwaysGenerateNew.Should().BeTrue();
        }

        [Fact]
        public void AddTraceability_WithSourceOverload_ShouldThrowWhenSourceIsNull()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => services.AddTraceability((string)null!));
        }

        [Fact]
        public void AddTraceability_WithSourceOverload_ShouldThrowWhenSourceIsEmpty()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => services.AddTraceability(""));
        }

        [Fact]
        public void AddTraceability_WithSourceOverload_ShouldThrowWhenSourceIsWhitespace()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => services.AddTraceability("   "));
        }

        [Fact]
        public void AddTraceability_ShouldNotRegisterIHttpClientFactory_WhenAlreadyRegistered()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddHttpClient(); // Registra IHttpClientFactory primeiro

            // Act
            services.AddTraceability();

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var factory = serviceProvider.GetService<IHttpClientFactory>();
            factory.Should().NotBeNull();
        }
    }
}
#endif

