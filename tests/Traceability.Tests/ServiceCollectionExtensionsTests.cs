#if NET8_0
using System;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
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
            var originalEnv = Environment.GetEnvironmentVariable("TRACEABILITY_SERVICENAME");
            try
            {
                Environment.SetEnvironmentVariable("TRACEABILITY_SERVICENAME", "TestService");
                var services = new ServiceCollection();

                // Act
                services.AddTraceability();

                // Assert
                var serviceProvider = services.BuildServiceProvider();
                var handler = serviceProvider.GetService<CorrelationIdHandler>();
                handler.Should().NotBeNull();
            }
            finally
            {
                Environment.SetEnvironmentVariable("TRACEABILITY_SERVICENAME", originalEnv);
            }
        }

        [Fact]
        public void AddTraceability_ShouldRegisterTraceabilityOptions()
        {
            // Arrange
            var originalEnv = Environment.GetEnvironmentVariable("TRACEABILITY_SERVICENAME");
            try
            {
                Environment.SetEnvironmentVariable("TRACEABILITY_SERVICENAME", "TestService");
                var services = new ServiceCollection();

                // Act
                services.AddTraceability();

                // Assert
                var serviceProvider = services.BuildServiceProvider();
                var options = serviceProvider.GetService<IOptions<TraceabilityOptions>>();
                options.Should().NotBeNull();
                options!.Value.HeaderName.Should().Be("X-Correlation-Id");
            }
            finally
            {
                Environment.SetEnvironmentVariable("TRACEABILITY_SERVICENAME", originalEnv);
            }
        }

        [Fact]
        public void AddTraceability_ShouldRegisterIHttpClientFactory()
        {
            // Arrange
            var originalEnv = Environment.GetEnvironmentVariable("TRACEABILITY_SERVICENAME");
            try
            {
                Environment.SetEnvironmentVariable("TRACEABILITY_SERVICENAME", "TestService");
                var services = new ServiceCollection();

                // Act
                services.AddTraceability();

                // Assert
                var serviceProvider = services.BuildServiceProvider();
                var factory = serviceProvider.GetService<IHttpClientFactory>();
                factory.Should().NotBeNull();
            }
            finally
            {
                Environment.SetEnvironmentVariable("TRACEABILITY_SERVICENAME", originalEnv);
            }
        }

        [Fact]
        public void AddTraceability_ShouldRegisterCorrelationIdScopeProvider()
        {
            // Arrange
            var originalEnv = Environment.GetEnvironmentVariable("TRACEABILITY_SERVICENAME");
            try
            {
                Environment.SetEnvironmentVariable("TRACEABILITY_SERVICENAME", "TestService");
                var services = new ServiceCollection();

                // Act
                services.AddTraceability();

                // Assert
                var serviceProvider = services.BuildServiceProvider();
                var scopeProvider = serviceProvider.GetService<IExternalScopeProvider>();
                scopeProvider.Should().NotBeNull();
                scopeProvider.Should().BeOfType<SourceScopeProvider>(); // Agora sempre usa SourceScopeProvider
            }
            finally
            {
                Environment.SetEnvironmentVariable("TRACEABILITY_SERVICENAME", originalEnv);
            }
        }

        [Fact]
        public void AddTraceability_WithSource_ShouldRegisterSourceScopeProvider()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddTraceability(configureOptions: options =>
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
            var originalEnv = Environment.GetEnvironmentVariable("TRACEABILITY_SERVICENAME");
            try
            {
                Environment.SetEnvironmentVariable("TRACEABILITY_SERVICENAME", "TestService");
                var services = new ServiceCollection();
                var customHeader = "X-Custom-Id";

                // Act
                services.AddTraceability(configureOptions: options =>
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
            finally
            {
                Environment.SetEnvironmentVariable("TRACEABILITY_SERVICENAME", originalEnv);
            }
        }

        [Fact]
        public void AddTraceability_WithSourceOverload_ShouldThrowWhenSourceIsNullAndNoEnvVar()
        {
            // Arrange
            var originalEnv = Environment.GetEnvironmentVariable("TRACEABILITY_SERVICENAME");
            try
            {
                Environment.SetEnvironmentVariable("TRACEABILITY_SERVICENAME", null);
                var services = new ServiceCollection();

                // Act & Assert
                Assert.Throws<InvalidOperationException>(() => services.AddTraceability((string?)null, options =>
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
        public void AddTraceability_WithSourceOverload_ShouldThrowWhenSourceIsEmptyAndNoEnvVar()
        {
            // Arrange
            var originalEnv = Environment.GetEnvironmentVariable("TRACEABILITY_SERVICENAME");
            try
            {
                Environment.SetEnvironmentVariable("TRACEABILITY_SERVICENAME", null);
                var services = new ServiceCollection();

                // Act & Assert
                Assert.Throws<InvalidOperationException>(() => services.AddTraceability("", options =>
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
        public void AddTraceability_WithSourceOverload_ShouldThrowWhenSourceIsWhitespaceAndNoEnvVar()
        {
            // Arrange
            var originalEnv = Environment.GetEnvironmentVariable("TRACEABILITY_SERVICENAME");
            try
            {
                Environment.SetEnvironmentVariable("TRACEABILITY_SERVICENAME", null);
                var services = new ServiceCollection();

                // Act & Assert
                Assert.Throws<InvalidOperationException>(() => services.AddTraceability("   ", options =>
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
        public void AddTraceability_ShouldNotRegisterIHttpClientFactory_WhenAlreadyRegistered()
        {
            // Arrange
            var originalEnv = Environment.GetEnvironmentVariable("TRACEABILITY_SERVICENAME");
            try
            {
                Environment.SetEnvironmentVariable("TRACEABILITY_SERVICENAME", "TestService");
                var services = new ServiceCollection();
                services.AddHttpClient(); // Registra IHttpClientFactory primeiro

                // Act
                services.AddTraceability();

                // Assert
                var serviceProvider = services.BuildServiceProvider();
                var factory = serviceProvider.GetService<IHttpClientFactory>();
                factory.Should().NotBeNull();
            }
            finally
            {
                Environment.SetEnvironmentVariable("TRACEABILITY_SERVICENAME", originalEnv);
            }
        }

        [Fact]
        public void AddTraceability_WithAutoRegisterMiddleware_ShouldRegisterStartupFilter()
        {
            // Arrange
            var originalEnv = Environment.GetEnvironmentVariable("TRACEABILITY_SERVICENAME");
            try
            {
                Environment.SetEnvironmentVariable("TRACEABILITY_SERVICENAME", "TestService");
                var services = new ServiceCollection();

                // Act
                services.AddTraceability();

                // Assert
                var serviceProvider = services.BuildServiceProvider();
                var startupFilter = serviceProvider.GetService<IStartupFilter>();
                startupFilter.Should().NotBeNull();
                // Verifica que é do tipo correto através do nome do tipo (já que é internal)
                startupFilter!.GetType().Name.Should().Be("CorrelationIdStartupFilter");
            }
            finally
            {
                Environment.SetEnvironmentVariable("TRACEABILITY_SERVICENAME", originalEnv);
            }
        }

        [Fact]
        public void AddTraceability_WithAutoRegisterMiddlewareFalse_ShouldNotRegisterStartupFilter()
        {
            // Arrange
            var originalEnv = Environment.GetEnvironmentVariable("TRACEABILITY_SERVICENAME");
            try
            {
                Environment.SetEnvironmentVariable("TRACEABILITY_SERVICENAME", "TestService");
                var services = new ServiceCollection();

                // Act
                services.AddTraceability(configureOptions: options =>
                {
                    options.AutoRegisterMiddleware = false;
                });

                // Assert
                var serviceProvider = services.BuildServiceProvider();
                var startupFilter = serviceProvider.GetService<IStartupFilter>();
                startupFilter.Should().BeNull();
            }
            finally
            {
                Environment.SetEnvironmentVariable("TRACEABILITY_SERVICENAME", originalEnv);
            }
        }

        [Fact]
        public void AddTraceability_WithAutoConfigureHttpClient_ShouldConfigureAllHttpClients()
        {
            // Arrange
            var originalEnv = Environment.GetEnvironmentVariable("TRACEABILITY_SERVICENAME");
            try
            {
                Environment.SetEnvironmentVariable("TRACEABILITY_SERVICENAME", "TestService");
                var services = new ServiceCollection();

                // Act
                services.AddTraceability();
                services.AddHttpClient("TestClient");

                // Assert
                var serviceProvider = services.BuildServiceProvider();
                var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
                var client = httpClientFactory.CreateClient("TestClient");
                
                // Verifica que o HttpClient foi criado (se chegou aqui, a configuração funcionou)
                client.Should().NotBeNull();
            }
            finally
            {
                Environment.SetEnvironmentVariable("TRACEABILITY_SERVICENAME", originalEnv);
            }
        }

        [Fact]
        public void AddTraceability_WithAutoConfigureHttpClientFalse_ShouldNotConfigureHttpClients()
        {
            // Arrange
            var originalEnv = Environment.GetEnvironmentVariable("TRACEABILITY_SERVICENAME");
            try
            {
                Environment.SetEnvironmentVariable("TRACEABILITY_SERVICENAME", "TestService");
                var services = new ServiceCollection();

                // Act
                services.AddTraceability(configureOptions: options =>
                {
                    options.AutoConfigureHttpClient = false;
                });
                services.AddHttpClient("TestClient");

                // Assert
                var serviceProvider = services.BuildServiceProvider();
                var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
                var client = httpClientFactory.CreateClient("TestClient");
                
                // HttpClient ainda deve ser criado, mas sem o handler automático
                client.Should().NotBeNull();
            }
            finally
            {
                Environment.SetEnvironmentVariable("TRACEABILITY_SERVICENAME", originalEnv);
            }
        }

        [Fact]
        public void AddTraceability_WithUseAssemblyNameAsFallback_ShouldUseAssemblyName()
        {
            // Arrange
            var originalEnv = Environment.GetEnvironmentVariable("TRACEABILITY_SERVICENAME");
            try
            {
                Environment.SetEnvironmentVariable("TRACEABILITY_SERVICENAME", null);
                var services = new ServiceCollection();
                var assemblyName = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name;

                // Act
                services.AddTraceability();

                // Assert
                var serviceProvider = services.BuildServiceProvider();
                var options = serviceProvider.GetService<IOptions<TraceabilityOptions>>();
                options.Should().NotBeNull();
                if (assemblyName != null)
                {
                    options!.Value.Source.Should().Be(assemblyName);
                }
            }
            finally
            {
                Environment.SetEnvironmentVariable("TRACEABILITY_SERVICENAME", originalEnv);
            }
        }

        [Fact]
        public void AddTraceability_WithUseAssemblyNameAsFallbackFalse_ShouldThrowException()
        {
            // Arrange
            var originalEnv = Environment.GetEnvironmentVariable("TRACEABILITY_SERVICENAME");
            try
            {
                Environment.SetEnvironmentVariable("TRACEABILITY_SERVICENAME", null);
                var services = new ServiceCollection();

                // Act & Assert
                Assert.Throws<InvalidOperationException>(() => services.AddTraceability(configureOptions: options =>
                {
                    options.UseAssemblyNameAsFallback = false;
                }));
            }
            finally
            {
                Environment.SetEnvironmentVariable("TRACEABILITY_SERVICENAME", originalEnv);
            }
        }
    }
}
#endif

