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
        public void Current_WhenActivityAvailable_ShouldBeIndependentFromTraceId()
        {
            // Arrange
            CorrelationContext.Clear();
            using var activity = new Activity("Test");
            activity.SetIdFormat(ActivityIdFormat.W3C);
            activity.Start();
            var traceId = activity.TraceId.ToString();

            // Act
            var correlationId = CorrelationContext.Current;

            // Assert
            // Correlation-ID deve ser independente do trace ID
            correlationId.Should().NotBeNullOrEmpty();
            correlationId.Should().NotBe(traceId); // Devem ser diferentes
            correlationId.Length.Should().Be(32); // GUID sem hífens
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
        public void HasValue_WhenActivityAvailable_ShouldReturnFalseIfNoCorrelationIdSet()
        {
            // Arrange
            CorrelationContext.Clear();
            using var activity = new Activity("Test");
            activity.SetIdFormat(ActivityIdFormat.W3C);
            activity.Start();

            // Act
            var hasValue = CorrelationContext.HasValue;

            // Assert
            // Correlation-ID é independente do Activity, então se não foi setado, deve ser false
            hasValue.Should().BeFalse();
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
        public void TryGetValue_WhenActivityAvailable_ShouldReturnFalseIfNoCorrelationIdSet()
        {
            // Arrange
            CorrelationContext.Clear();
            using var activity = new Activity("Test");
            activity.SetIdFormat(ActivityIdFormat.W3C);
            activity.Start();

            // Act
            var result = CorrelationContext.TryGetValue(out var value);

            // Assert
            // Correlation-ID é independente do Activity, então se não foi setado, deve retornar false
            result.Should().BeFalse();
            value.Should().BeNull();
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
        public void Current_ShouldPrioritizeExplicitCorrelationIdOverActivity()
        {
            // Arrange
            CorrelationContext.Clear();
            var explicitCorrelationId = Guid.NewGuid().ToString("N");
            CorrelationContext.Current = explicitCorrelationId;

            // Criar Activity (não deve afetar correlation-ID)
            using var activity = new Activity("Test");
            activity.SetIdFormat(ActivityIdFormat.W3C);
            activity.Start();

            // Act
            var correlationId = CorrelationContext.Current;

            // Assert
            // Correlation-ID explícito deve ter prioridade sobre qualquer Activity
            correlationId.Should().Be(explicitCorrelationId);
            correlationId.Should().NotBe(activity.TraceId.ToString());
        }

#if NET48 || NET8_0
        [Fact]
        public void Current_WhenActivityWithHierarchicalFormat_ShouldBeIndependent()
        {
            // Arrange
            CorrelationContext.Clear();
            // Criar Activity com formato hierárquico (comum no .NET Framework 4.8)
            using var activity = new Activity("Test");
            // Não definir W3C format - usa formato hierárquico por padrão
            activity.Start();

            // Act
            var correlationId = CorrelationContext.Current;

            // Assert
            // Correlation-ID deve ser independente do Activity, sempre gerado como GUID
            correlationId.Should().NotBeNullOrEmpty();
            correlationId.Length.Should().Be(32); // GUID sem hífens
            correlationId.Should().NotBe(activity.TraceId.ToString()); // Devem ser diferentes
        }

        [Fact]
        public void TryGetValue_WhenActivityWithHierarchicalFormat_ShouldReturnFalseIfNoCorrelationIdSet()
        {
            // Arrange
            CorrelationContext.Clear();
            using var activity = new Activity("Test");
            activity.Start();

            // Act
            var result = CorrelationContext.TryGetValue(out var value);

            // Assert
            // Correlation-ID é independente do Activity, então se não foi setado, deve retornar false
            result.Should().BeFalse();
            value.Should().BeNull();
        }

        [Fact]
        public void Current_ShouldBeIndependentFromActivity()
        {
            // Arrange
            CorrelationContext.Clear();
            
            // Teste 1: Activity com W3C format
            using var activity1 = new Activity("Test1");
            activity1.SetIdFormat(ActivityIdFormat.W3C);
            activity1.Start();
            
            var correlationId1 = CorrelationContext.Current;
            var traceId1 = activity1.TraceId.ToString();
            correlationId1.Should().NotBeNullOrEmpty();
            correlationId1.Should().NotBe(traceId1); // Devem ser diferentes
            correlationId1.Length.Should().Be(32); // GUID sem hífens
            
            activity1.Stop();
            
            // Teste 2: Sem Activity (mas correlation-ID já foi gerado no teste 1)
            var previousActivity = Activity.Current;
            if (previousActivity != null)
            {
                previousActivity.Stop();
            }
            
            var correlationId2 = CorrelationContext.Current;
            correlationId2.Should().NotBeNullOrEmpty();
            correlationId2.Length.Should().Be(32); // GUID sem hífens
            correlationId2.Should().Be(correlationId1); // Retorna o mesmo valor já gerado (armazenado no AsyncLocal)
            
            // Teste 3: Novo Activity com W3C (mas correlation-ID continua o mesmo)
            using var activity3 = new Activity("Test3");
            activity3.SetIdFormat(ActivityIdFormat.W3C);
            activity3.Start();
            
            var correlationId3 = CorrelationContext.Current;
            var traceId3 = activity3.TraceId.ToString();
            correlationId3.Should().NotBeNullOrEmpty();
            correlationId3.Should().NotBe(traceId3); // Devem ser diferentes
            correlationId3.Length.Should().Be(32); // GUID sem hífens
            correlationId3.Should().Be(correlationId1); // Continua o mesmo correlation-ID (independente do Activity)
        }
#endif
    }
}

