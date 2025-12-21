using System;
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
        public void Current_ShouldBeIsolatedBetweenAsyncContexts()
        {
            // Arrange
            CorrelationContext.Clear();
            var correlationId1 = Guid.NewGuid().ToString("N");
            var correlationId2 = Guid.NewGuid().ToString("N");
            string result1 = null;
            string result2 = null;

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

            Task.WaitAll(task1, task2);

            // Assert
            result1.Should().Be(correlationId1);
            result2.Should().Be(correlationId2);
            result1.Should().NotBe(result2);
        }

        [Fact]
        public void Current_ShouldMaintainValueAcrossAsyncOperations()
        {
            // Arrange
            CorrelationContext.Clear();
            var expectedId = Guid.NewGuid().ToString("N");
            CorrelationContext.Current = expectedId;
            string result = null;

            // Act
            var task = Task.Run(async () =>
            {
                await Task.Delay(10);
                result = CorrelationContext.Current;
            });

            task.Wait();

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
    }
}

