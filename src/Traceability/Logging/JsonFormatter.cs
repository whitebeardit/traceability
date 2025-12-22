using System;
using System.IO;
using System.Text;
using Serilog.Events;
using Serilog.Formatting;
using Traceability.Configuration;

namespace Traceability.Logging
{
    /// <summary>
    /// Formatter JSON customizado para Serilog que formata logs em JSON estruturado.
    /// Suporta configuração via TraceabilityOptions para incluir/excluir campos específicos.
    /// </summary>
    public class JsonFormatter : ITextFormatter
    {
        private readonly TraceabilityOptions _options;
        private readonly bool _indent;

        /// <summary>
        /// Cria uma nova instância do JsonFormatter.
        /// </summary>
        /// <param name="options">Opções de configuração para o formatter (opcional).</param>
        /// <param name="indent">Se true, formata JSON com indentação (padrão: false).</param>
        public JsonFormatter(TraceabilityOptions? options = null, bool indent = false)
        {
            _options = options ?? new TraceabilityOptions();
            _indent = indent;
        }

        /// <summary>
        /// Formata o log event em JSON.
        /// </summary>
        public void Format(LogEvent logEvent, TextWriter output)
        {
            if (logEvent == null || output == null)
                return;

            var sb = new StringBuilder();
            sb.Append('{');

            var first = true;

            // Timestamp
            if (_options.LogIncludeTimestamp)
            {
                AppendProperty(sb, ref first, "Timestamp", FormatTimestamp(logEvent.Timestamp), _indent);
            }

            // Level
            if (_options.LogIncludeLevel)
            {
                AppendProperty(sb, ref first, "Level", logEvent.Level.ToString(), _indent);
            }

            // Source
            if (_options.LogIncludeSource && logEvent.Properties.TryGetValue("Source", out var sourceValue))
            {
                AppendProperty(sb, ref first, "Source", sourceValue.ToString().Trim('"'), _indent);
            }

            // CorrelationId
            if (_options.LogIncludeCorrelationId && logEvent.Properties.TryGetValue("CorrelationId", out var correlationIdValue))
            {
                AppendProperty(sb, ref first, "CorrelationId", correlationIdValue.ToString().Trim('"'), _indent);
            }

            // Message
            if (_options.LogIncludeMessage)
            {
                var message = logEvent.RenderMessage();
                AppendProperty(sb, ref first, "Message", EscapeJson(message), _indent);
            }

            // Data (objetos serializados)
            if (_options.LogIncludeData && logEvent.Properties.TryGetValue("data", out var dataValue))
            {
                var dataJson = FormatDataValue(dataValue);
                AppendProperty(sb, ref first, "Data", dataJson, _indent);
            }

            // Exception
            if (_options.LogIncludeException && logEvent.Exception != null)
            {
                var exceptionJson = FormatException(logEvent.Exception);
                AppendProperty(sb, ref first, "Exception", exceptionJson, _indent);
            }

            // Outras propriedades (exceto as já incluídas)
            foreach (var property in logEvent.Properties)
            {
                if (IsKnownProperty(property.Key))
                    continue;

                var value = FormatPropertyValue(property.Value);
                AppendProperty(sb, ref first, property.Key, value, _indent);
            }

            sb.Append('}');
            if (_indent)
                sb.AppendLine();
            else
                sb.AppendLine();

            output.Write(sb.ToString());
        }

        private static string FormatTimestamp(DateTimeOffset timestamp)
        {
            return timestamp.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        }

        private static string FormatDataValue(LogEventPropertyValue value)
        {
            if (value == null)
                return "null";

            // Se já é uma string JSON, retorna diretamente (removendo aspas externas)
            var str = value.ToString();
            if (str.StartsWith("{") || str.StartsWith("["))
                return str;

            // Caso contrário, escapa como string
            return EscapeJson(str.Trim('"'));
        }

        private static string FormatPropertyValue(LogEventPropertyValue value)
        {
            if (value == null)
                return "null";

            var str = value.ToString();
            
            // Se parece com JSON (objeto ou array), retorna diretamente
            if ((str.StartsWith("{") && str.EndsWith("}")) || 
                (str.StartsWith("[") && str.EndsWith("]")))
                return str;

            // Caso contrário, escapa como string
            return EscapeJson(str.Trim('"'));
        }

        private static string FormatException(Exception exception)
        {
            if (exception == null)
                return "null";

            var sb = new StringBuilder();
            sb.Append('{');
            sb.Append($"\"Type\":\"{EscapeJson(exception.GetType().FullName ?? exception.GetType().Name)}\"");
            sb.Append($",\"Message\":\"{EscapeJson(exception.Message)}\"");
            
            if (!string.IsNullOrEmpty(exception.StackTrace))
            {
                sb.Append($",\"StackTrace\":\"{EscapeJson(exception.StackTrace)}\"");
            }

            if (exception.InnerException != null)
            {
                sb.Append($",\"InnerException\":{FormatException(exception.InnerException)}");
            }

            sb.Append('}');
            return sb.ToString();
        }

        private static bool IsKnownProperty(string propertyName)
        {
            return propertyName == "Source" ||
                   propertyName == "CorrelationId" ||
                   propertyName == "data" ||
                   propertyName.StartsWith("@") ||
                   propertyName == "Message" ||
                   propertyName == "MessageTemplate";
        }

        private static void AppendProperty(StringBuilder sb, ref bool first, string name, string value, bool indent)
        {
            if (!first)
            {
                sb.Append(',');
                if (indent)
                    sb.Append(' ');
            }

            first = false;

            if (indent)
            {
                sb.AppendLine();
                sb.Append("  ");
            }

            sb.Append('"');
            sb.Append(name);
            sb.Append('"');
            sb.Append(':');

            if (indent)
                sb.Append(' ');

            // Se value já é JSON válido (objeto ou array), não adiciona aspas
            if ((value.StartsWith("{") && value.EndsWith("}")) || 
                (value.StartsWith("[") && value.EndsWith("]")) ||
                value == "null" ||
                (value.Length > 0 && char.IsDigit(value[0]) && double.TryParse(value, out _)))
            {
                sb.Append(value);
            }
            else
            {
                sb.Append('"');
                sb.Append(value);
                sb.Append('"');
            }
        }

        private static string EscapeJson(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            var sb = new StringBuilder(value.Length);
            foreach (var c in value)
            {
                switch (c)
                {
                    case '"':
                        sb.Append("\\\"");
                        break;
                    case '\\':
                        sb.Append("\\\\");
                        break;
                    case '\b':
                        sb.Append("\\b");
                        break;
                    case '\f':
                        sb.Append("\\f");
                        break;
                    case '\n':
                        sb.Append("\\n");
                        break;
                    case '\r':
                        sb.Append("\\r");
                        break;
                    case '\t':
                        sb.Append("\\t");
                        break;
                    default:
                        if (c < 0x20)
                        {
                            sb.AppendFormat("\\u{0:X4}", (int)c);
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }
            return sb.ToString();
        }
    }
}

