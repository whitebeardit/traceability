using System;
using System.Collections.Generic;
using System.Linq;
using Serilog.Core;
using Serilog.Events;

namespace Traceability.Logging
{
    /// <summary>
    /// Enricher do Serilog que detecta objetos complexos nas propriedades do log e os serializa em um campo "data".
    /// Identifica objetos não primitivos e os agrupa em um único campo "data" no JSON de saída.
    /// </summary>
    public class DataEnricher : ILogEventEnricher
    {
        private const string DataPropertyName = "data";
        private const int MaxDepth = 10;
        private const int MaxDictionarySize = 1000;
        private static readonly HashSet<Type> PrimitiveTypes = new HashSet<Type>
        {
            typeof(string),
            typeof(int),
            typeof(long),
            typeof(short),
            typeof(byte),
            typeof(uint),
            typeof(ulong),
            typeof(ushort),
            typeof(sbyte),
            typeof(float),
            typeof(double),
            typeof(decimal),
            typeof(bool),
            typeof(char),
            typeof(DateTime),
            typeof(DateTimeOffset),
            typeof(TimeSpan),
            typeof(Guid),
            typeof(Uri)
        };

        /// <summary>
        /// Enriquece o log event detectando objetos complexos e os serializando no campo "data".
        /// </summary>
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            if (logEvent == null || propertyFactory == null)
                return;

            var dataObjects = new Dictionary<string, object?>();

            // Itera sobre todas as propriedades do log event
            foreach (var property in logEvent.Properties)
            {
                // Ignora propriedades especiais do Serilog e propriedades já conhecidas
                if (IsSpecialProperty(property.Key) || IsKnownTraceabilityProperty(property.Key))
                    continue;

                // Verifica se a propriedade contém um objeto complexo
                if (IsComplexObject(property.Value))
                {
                    var value = ExtractValue(property.Value, visited: new HashSet<object>(), depth: 0);
                    if (value != null)
                    {
                        dataObjects[property.Key] = value;
                    }
                }
            }

            // Se encontrou objetos complexos, adiciona ao campo "data"
            if (dataObjects.Count > 0)
            {
                // Se houver apenas um objeto, usa diretamente; caso contrário, agrupa em um objeto
                object dataValue = dataObjects.Count == 1 
                    ? dataObjects.Values.First()! 
                    : dataObjects;

                var dataProperty = propertyFactory.CreateProperty(DataPropertyName, dataValue);
                logEvent.AddPropertyIfAbsent(dataProperty);
            }
        }

        private static bool IsSpecialProperty(string propertyName)
        {
            // Propriedades especiais do Serilog que não devem ser movidas para "data"
            return propertyName.StartsWith("@") || 
                   propertyName == "Message" || 
                   propertyName == "MessageTemplate" ||
                   propertyName == "Exception";
        }

        private static bool IsKnownTraceabilityProperty(string propertyName)
        {
            // Propriedades de traceability que não devem ser movidas para "data"
            return propertyName == "Source" || 
                   propertyName == "CorrelationId" ||
                   propertyName == DataPropertyName;
        }

        private static bool IsComplexObject(LogEventPropertyValue value)
        {
            if (value == null)
                return false;

            // Verifica se é um objeto estruturado (DictionaryValue, StructureValue, SequenceValue)
            if (value is DictionaryValue || value is StructureValue || value is SequenceValue)
                return true;

            // Verifica se é um ScalarValue com tipo complexo
            if (value is ScalarValue scalarValue)
            {
                var type = scalarValue.Value?.GetType();
                if (type == null)
                    return false;

                // Não é primitivo e não é null
                return !PrimitiveTypes.Contains(type) && 
                       !type.IsPrimitive && 
                       type != typeof(object);
            }

            return false;
        }

        private static object? ExtractValue(LogEventPropertyValue value, HashSet<object> visited, int depth)
        {
            if (value == null)
                return null;

            // Previne stack overflow limitando a profundidade
            if (depth >= MaxDepth)
            {
                return "[Maximum depth reached]";
            }

            // Para DictionaryValue, converte para Dictionary
            if (value is DictionaryValue dictValue)
            {
                // Previne OutOfMemoryException limitando o tamanho do dicionário
                if (dictValue.Elements.Count > MaxDictionarySize)
                {
                    return $"[Dictionary too large: {dictValue.Elements.Count} elements, max: {MaxDictionarySize}]";
                }

                var result = new Dictionary<string, object?>();
                foreach (var element in dictValue.Elements)
                {
                    var key = element.Key.ToString().Trim('"');
                    result[key] = ExtractValue(element.Value, visited, depth + 1);
                }
                return result;
            }

            // Para StructureValue, converte para Dictionary
            if (value is StructureValue structValue)
            {
                // Previne OutOfMemoryException limitando o tamanho
                if (structValue.Properties.Count > MaxDictionarySize)
                {
                    return $"[Structure too large: {structValue.Properties.Count} properties, max: {MaxDictionarySize}]";
                }

                var result = new Dictionary<string, object?>();
                foreach (var property in structValue.Properties)
                {
                    result[property.Name] = ExtractValue(property.Value, visited, depth + 1);
                }
                return result;
            }

            // Para SequenceValue, converte para List
            if (value is SequenceValue seqValue)
            {
                // Previne OutOfMemoryException limitando o tamanho
                if (seqValue.Elements.Count > MaxDictionarySize)
                {
                    return $"[Sequence too large: {seqValue.Elements.Count} elements, max: {MaxDictionarySize}]";
                }

                var result = new List<object?>();
                foreach (var element in seqValue.Elements)
                {
                    result.Add(ExtractValue(element, visited, depth + 1));
                }
                return result;
            }

            // Para ScalarValue, verifica referências circulares
            if (value is ScalarValue scalarValue)
            {
                var scalarObj = scalarValue.Value;
                
                // Detecta referências circulares em objetos complexos
                if (scalarObj != null && !IsPrimitiveOrSimpleType(scalarObj.GetType()))
                {
                    if (visited.Contains(scalarObj))
                    {
                        return "[Circular reference detected]";
                    }
                    
                    visited.Add(scalarObj);
                    try
                    {
                        // Para objetos complexos, tenta extrair propriedades recursivamente
                        // Mas limita profundidade para prevenir stack overflow
                        if (depth < MaxDepth - 1)
                        {
                            return ExtractComplexObject(scalarObj, visited, depth + 1);
                        }
                        return scalarObj.ToString();
                    }
                    finally
                    {
                        visited.Remove(scalarObj);
                    }
                }
                
                return scalarObj;
            }

            // Fallback: converte para string
            return value.ToString();
        }

        private static bool IsPrimitiveOrSimpleType(Type type)
        {
            return PrimitiveTypes.Contains(type) || 
                   type.IsPrimitive || 
                   type == typeof(object) ||
                   type == typeof(void);
        }

        private static object? ExtractComplexObject(object obj, HashSet<object> visited, int depth)
        {
            if (obj == null || depth >= MaxDepth)
                return obj?.ToString() ?? "null";

            // Para objetos complexos, retorna representação string simples
            // Evita recursão profunda que pode causar stack overflow
            try
            {
                return obj.ToString();
            }
            catch
            {
                return $"[{obj.GetType().Name}]";
            }
        }
    }
}

