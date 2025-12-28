using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Serilog.Events;
using Serilog.Parsing;
using Traceability.Logging;
using Xunit;

namespace Traceability.Tests
{
    public class JsonFormatterTests
    {
        [Fact]
        public void JsonFormatter_ShouldWriteTraceContextFieldsNearTop_WhenPresent()
        {
            // Arrange
            var formatter = new JsonFormatter();
            var template = new MessageTemplateParser().Parse("Hello {Name}");

            var properties = new List<LogEventProperty>
            {
                new LogEventProperty("Source", new ScalarValue("TestService")),
                new LogEventProperty("CorrelationId", new ScalarValue("corr-123")),
                new LogEventProperty("TraceId", new ScalarValue("trace-abc")),
                new LogEventProperty("SpanId", new ScalarValue("span-def")),
                new LogEventProperty("ParentSpanId", new ScalarValue("parent-ghi")),
                new LogEventProperty("RouteName", new ScalarValue("GET Home/About")),
                new LogEventProperty("Name", new ScalarValue("World")),
            };

            var logEvent = new LogEvent(
                DateTimeOffset.UtcNow,
                LogEventLevel.Information,
                exception: null,
                template,
                properties);

            using var writer = new StringWriter();

            // Act
            formatter.Format(logEvent, writer);
            var json = writer.ToString();

            // Assert (presence)
            json.Should().Contain("\"TraceId\"");
            json.Should().Contain("\"SpanId\"");
            json.Should().Contain("\"ParentSpanId\"");
            json.Should().Contain("\"RouteName\"");

            // Assert (ordering relative to Message)
            json.IndexOf("\"TraceId\"", StringComparison.Ordinal).Should().BeLessThan(json.IndexOf("\"Message\"", StringComparison.Ordinal));
            json.IndexOf("\"SpanId\"", StringComparison.Ordinal).Should().BeLessThan(json.IndexOf("\"Message\"", StringComparison.Ordinal));
            json.IndexOf("\"ParentSpanId\"", StringComparison.Ordinal).Should().BeLessThan(json.IndexOf("\"Message\"", StringComparison.Ordinal));
            json.IndexOf("\"RouteName\"", StringComparison.Ordinal).Should().BeLessThan(json.IndexOf("\"Message\"", StringComparison.Ordinal));
        }
    }
}


