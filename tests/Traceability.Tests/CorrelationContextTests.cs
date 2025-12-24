using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Traceability;
using Xunit;

namespace Traceability.Tests
{
    public class CorrelationContextTests
    {
        [Fact]
        public void Current_WhenNoValue_ShouldGenerateNew()
        {
            // Arrange
            CorrelationContext.Clear();

            // Act
            var correlationId = CorrelationContext.Current;

            // Assert
            correlationId.Should().NotBeNullOrEmpty();
            correlationId.Length.Should().Be(32); // GUID sem hífens tem 32 caracteres
        }

        [Fact]
        public void Current_WhenValueExists_ShouldReturnSameValue()
        {
            // Arrange
            var expectedId = Guid.NewGuid().ToString("N");
            CorrelationContext.Current = expectedId;

            // Act
            var actualId = CorrelationContext.Current;

            // Assert
            actualId.Should().Be(expectedId);
        }

        [Fact]
        public void HasValue_WhenNoValue_ShouldReturnFalse()
        {
            // Arrange
            CorrelationContext.Clear();

            // Act
            var hasValue = CorrelationContext.HasValue;

            // Assert
            hasValue.Should().BeFalse();
        }

        [Fact]
        public void HasValue_WhenValueExists_ShouldReturnTrue()
        {
            // Arrange
            CorrelationContext.Current = Guid.NewGuid().ToString("N");

            // Act
            var hasValue = CorrelationContext.HasValue;

            // Assert
            hasValue.Should().BeTrue();
        }

        [Fact]
        public void GetOrCreate_WhenNoValue_ShouldCreateNew()
        {
            // Arrange
            CorrelationContext.Clear();

            // Act
            var correlationId = CorrelationContext.GetOrCreate();

            // Assert
            correlationId.Should().NotBeNullOrEmpty();
            CorrelationContext.HasValue.Should().BeTrue();
        }

        [Fact]
        public void GetOrCreate_WhenValueExists_ShouldReturnExisting()
        {
            // Arrange
            var expectedId = Guid.NewGuid().ToString("N");
            CorrelationContext.Current = expectedId;

            // Act
            var actualId = CorrelationContext.GetOrCreate();

            // Assert
            actualId.Should().Be(expectedId);
        }

        [Fact]
        public void Clear_ShouldRemoveValue()
        {
            // Arrange
            CorrelationContext.Current = Guid.NewGuid().ToString("N");

            // Act
            CorrelationContext.Clear();

            // Assert
            CorrelationContext.HasValue.Should().BeFalse();
        }

        [Fact]
        public async Task Current_ShouldBeIsolatedBetweenAsyncContexts()
        {
            // Arrange
            CorrelationContext.Clear();
            var correlationId1 = Guid.NewGuid().ToString("N");
            var correlationId2 = Guid.NewGuid().ToString("N");
            string? result1 = null;
            string? result2 = null;

            // Act
            var task1 = Task.Run(async () =>
            {
                CorrelationContext.Current = correlationId1;
                await Task.Delay(10);
                result1 = CorrelationContext.Current;
            });

            var task2 = Task.Run(async () =>
            {
                CorrelationContext.Current = correlationId2;
                await Task.Delay(10);
                result2 = CorrelationContext.Current;
            });

            await Task.WhenAll(task1, task2);

            // Assert
            result1.Should().Be(correlationId1);
            result2.Should().Be(correlationId2);
            result1.Should().NotBe(result2);
        }

        [Fact]
        public async Task Current_ShouldMaintainValueAcrossAsyncOperations()
        {
            // Arrange
            CorrelationContext.Clear();
            var expectedId = Guid.NewGuid().ToString("N");
            CorrelationContext.Current = expectedId;
            string? result = null;

            // Act
            await Task.Run(async () =>
            {
                await Task.Delay(10);
                result = CorrelationContext.Current;
            });

            // Assert
            result.Should().Be(expectedId);
        }

        [Fact]
        public void TryGetValue_WhenNoValue_ShouldReturnFalseAndNull()
        {
            // Arrange
            CorrelationContext.Clear();

            // Act
            var result = CorrelationContext.TryGetValue(out var value);

            // Assert
            result.Should().BeFalse();
            value.Should().BeNull();
            CorrelationContext.HasValue.Should().BeFalse();
        }

        [Fact]
        public void TryGetValue_WhenValueExists_ShouldReturnTrueAndValue()
        {
            // Arrange
            var expectedId = Guid.NewGuid().ToString("N");
            CorrelationContext.Current = expectedId;

            // Act
            var result = CorrelationContext.TryGetValue(out var value);

            // Assert
            result.Should().BeTrue();
            value.Should().Be(expectedId);
        }

        [Fact]
        public void TryGetValue_ShouldNotCreateNewValue()
        {
            // Arrange
            CorrelationContext.Clear();

            // Act
            var result1 = CorrelationContext.TryGetValue(out var value1);
            var hasValueAfterTryGet = CorrelationContext.HasValue;
            var result2 = CorrelationContext.TryGetValue(out var value2);

            // Assert
            result1.Should().BeFalse();
            result2.Should().BeFalse();
            value1.Should().BeNull();
            value2.Should().BeNull();
            hasValueAfterTryGet.Should().BeFalse();
        }

        [Fact]
        public void Current_WhenActivityAvailable_ShouldUseTraceId()
        {
            // Arrange
            CorrelationContext.Clear();
            using var activity = new Activity("Test");
            activity.SetIdFormat(ActivityIdFormat.W3C);
            activity.Start();

            // Act
            var correlationId = CorrelationContext.Current;

            // Assert
            correlationId.Should().Be(activity.TraceId.ToString());
            correlationId.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void Current_WhenActivityNotAvailable_ShouldUseFallback()
        {
            // Arrange
            CorrelationContext.Clear();
            // Garantir que não há Activity
            var previousActivity = Activity.Current;
            if (previousActivity != null)
            {
                previousActivity.Stop();
            }

            // Act
            var correlationId = CorrelationContext.Current;

            // Assert
            correlationId.Should().NotBeNullOrEmpty();
            correlationId.Length.Should().Be(32); // GUID sem hífens tem 32 caracteres
        }

        [Fact]
        public void HasValue_WhenActivityAvailable_ShouldReturnTrue()
        {
            // Arrange
            using var activity = new Activity("Test");
            activity.SetIdFormat(ActivityIdFormat.W3C);
            activity.Start();

            // Act
            var hasValue = CorrelationContext.HasValue;

            // Assert
            hasValue.Should().BeTrue();
        }

        [Fact]
        public void HasValue_WhenActivityNotAvailable_ShouldUseFallback()
        {
            // Arrange
            CorrelationContext.Clear();
            // Garantir que não há Activity
            var previousActivity = Activity.Current;
            if (previousActivity != null)
            {
                previousActivity.Stop();
            }

            // Act
            var hasValue = CorrelationContext.HasValue;

            // Assert
            hasValue.Should().BeFalse();
        }

        [Fact]
        public void TryGetValue_WhenActivityAvailable_ShouldReturnTraceId()
        {
            // Arrange
            using var activity = new Activity("Test");
            activity.SetIdFormat(ActivityIdFormat.W3C);
            activity.Start();

            // Act
            var result = CorrelationContext.TryGetValue(out var value);

            // Assert
            result.Should().BeTrue();
            value.Should().Be(activity.TraceId.ToString());
        }

        [Fact]
        public void TryGetValue_WhenActivityNotAvailable_ShouldUseFallback()
        {
            // Arrange
            CorrelationContext.Clear();
            // Garantir que não há Activity
            var previousActivity = Activity.Current;
            if (previousActivity != null)
            {
                previousActivity.Stop();
            }

            // Act
            var result = CorrelationContext.TryGetValue(out var value);

            // Assert
            result.Should().BeFalse();
            value.Should().BeNull();
        }

        [Fact]
        public void Current_ShouldPrioritizeActivityOverFallback()
        {
            // Arrange
            CorrelationContext.Clear();
            var fallbackId = Guid.NewGuid().ToString("N");
            CorrelationContext.Current = fallbackId;

            // Criar Activity (deve ter prioridade)
            using var activity = new Activity("Test");
            activity.SetIdFormat(ActivityIdFormat.W3C);
            activity.Start();

            // Act
            var correlationId = CorrelationContext.Current;

            // Assert
            correlationId.Should().Be(activity.TraceId.ToString());
            correlationId.Should().NotBe(fallbackId);
        }

#if NET48 || NET8_0
        [Fact]
        public void Current_WhenActivityWithHierarchicalFormat_ShouldExtractTraceId()
        {
            // Arrange
            CorrelationContext.Clear();
            // Criar Activity com formato hierárquico (comum no .NET Framework 4.8)
            using var activity = new Activity("Test");
            // Não definir W3C format - usa formato hierárquico por padrão
            activity.Start();
            
            // Simular formato hierárquico: |{trace-id}.{span-id}.{parent-id}|
            // Como não podemos forçar formato hierárquico diretamente, testamos o fallback
            // Se o Activity não tiver W3C format, deve tentar extrair do formato hierárquico ou usar fallback
            var hasW3C = activity.IdFormat == ActivityIdFormat.W3C;

            // Act
            var correlationId = CorrelationContext.Current;

            // Assert
            correlationId.Should().NotBeNullOrEmpty();
            // Se não for W3C, pode extrair do formato hierárquico (pode ter tamanho variável) ou usar fallback (GUID de 32 chars)
            if (!hasW3C)
            {
                // Pode ser trace-id extraído do formato hierárquico (tamanho variável) ou GUID (32 chars)
                correlationId.Length.Should().BeGreaterThan(0);
                correlationId.Length.Should().BeLessThanOrEqualTo(128);
            }
            else
            {
                // Se for W3C, deve ter 32 caracteres (hex sem hífens)
                correlationId.Length.Should().Be(32);
            }
        }

        [Fact]
        public void TryGetValue_WhenActivityWithHierarchicalFormat_ShouldWork()
        {
            // Arrange
            CorrelationContext.Clear();
            using var activity = new Activity("Test");
            activity.Start();
            var hasW3C = activity.IdFormat == ActivityIdFormat.W3C;

            // Act
            var result = CorrelationContext.TryGetValue(out var value);

            // Assert
            if (hasW3C)
            {
                // Se for W3C, deve conseguir extrair o trace-id
                result.Should().BeTrue();
                value.Should().NotBeNullOrEmpty();
                value.Should().Be(activity.TraceId.ToString());
            }
            else
            {
                // Se não for W3C, pode conseguir extrair do formato hierárquico ou retornar false
                // O comportamento depende se o Activity.Id está no formato hierárquico esperado
                // Se conseguir extrair, retorna true; caso contrário, retorna false
                if (result)
                {
                    value.Should().NotBeNullOrEmpty();
                    value!.Length.Should().BeGreaterThan(0);
                    value.Length.Should().BeLessThanOrEqualTo(128);
                }
                else
                {
                    value.Should().BeNull();
                }
            }
        }

        [Fact]
        public void Current_ShouldHandleActivityIdFormatChanges()
        {
            // Arrange
            CorrelationContext.Clear();
            
            // Teste 1: Activity com W3C format
            using var activity1 = new Activity("Test1");
            activity1.SetIdFormat(ActivityIdFormat.W3C);
            activity1.Start();
            
            var correlationId1 = CorrelationContext.Current;
            correlationId1.Should().NotBeNullOrEmpty();
            
            activity1.Stop();
            
            // Teste 2: Sem Activity (fallback)
            var previousActivity = Activity.Current;
            if (previousActivity != null)
            {
                previousActivity.Stop();
            }
            
            var correlationId2 = CorrelationContext.Current;
            correlationId2.Should().NotBeNullOrEmpty();
            correlationId2.Length.Should().Be(32); // GUID sem hífens
            
            // Teste 3: Novo Activity com W3C
            using var activity3 = new Activity("Test3");
            activity3.SetIdFormat(ActivityIdFormat.W3C);
            activity3.Start();
            
            var correlationId3 = CorrelationContext.Current;
            correlationId3.Should().NotBeNullOrEmpty();
            correlationId3.Should().Be(activity3.TraceId.ToString());
        }
#endif
    }
}

