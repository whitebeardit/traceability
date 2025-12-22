# Lição 3: Uso Básico

Nesta lição, você aprenderá a usar o `CorrelationContext` diretamente para gerenciar correlation-id.

## CorrelationContext

O `CorrelationContext` é uma classe estática que gerencia o correlation-id no contexto assíncrono da thread atual.

## Propriedades e Métodos

### Current

Obtém ou define o correlation-id atual. Se não existir, cria um novo automaticamente.

```csharp
using Traceability;

// Obter correlation-id (cria se não existir)
var correlationId = CorrelationContext.Current;
Console.WriteLine($"Correlation ID: {correlationId}");
```

**Output esperado:**
```
Correlation ID: a1b2c3d4e5f6789012345678901234ab
```

### HasValue

Verifica se existe um correlation-id no contexto.

```csharp
if (CorrelationContext.HasValue)
{
    var id = CorrelationContext.Current;
    Console.WriteLine($"Correlation ID existe: {id}");
}
else
{
    Console.WriteLine("Nenhum correlation-id no contexto");
}
```

### GetOrCreate()

Obtém o correlation-id existente ou cria um novo explicitamente.

```csharp
var correlationId = CorrelationContext.GetOrCreate();
Console.WriteLine($"Correlation ID: {correlationId}");
```

### TryGetValue()

Tenta obter o correlation-id existente **sem criar um novo** se não existir. Retorna `true` se um correlation-id existe, `false` caso contrário.

```csharp
if (CorrelationContext.TryGetValue(out var correlationId))
{
    Console.WriteLine($"Correlation ID encontrado: {correlationId}");
}
else
{
    Console.WriteLine("Nenhum correlation-id no contexto");
}
```

**Por que usar `TryGetValue()`?**
- Evita criar correlation-id indesejadamente
- Útil quando você só quer ler o valor se ele já existir

### Clear()

Limpa o correlation-id do contexto.

```csharp
CorrelationContext.Clear();
```

## Exemplo Completo

```csharp
using Traceability;

// Exemplo 1: Obter ou criar
var correlationId1 = CorrelationContext.Current;
Console.WriteLine($"Correlation ID 1: {correlationId1}");

// Exemplo 2: Verificar se existe
if (CorrelationContext.HasValue)
{
    var correlationId2 = CorrelationContext.Current;
    Console.WriteLine($"Correlation ID 2: {correlationId2}");
    // correlationId1 e correlationId2 são iguais
}

// Exemplo 3: Tentar obter sem criar
if (CorrelationContext.TryGetValue(out var correlationId3))
{
    Console.WriteLine($"Correlation ID 3: {correlationId3}");
}

// Exemplo 4: Limpar contexto
CorrelationContext.Clear();
Console.WriteLine($"Após Clear, HasValue: {CorrelationContext.HasValue}"); // False
```

**Output esperado:**
```
Correlation ID 1: a1b2c3d4e5f6789012345678901234ab
Correlation ID 2: a1b2c3d4e5f6789012345678901234ab
Correlation ID 3: a1b2c3d4e5f6789012345678901234ab
Após Clear, HasValue: False
```

## Preservação em Operações Assíncronas

O correlation-id é preservado através de operações assíncronas:

```csharp
var correlationIdBefore = CorrelationContext.GetOrCreate();
Console.WriteLine($"Antes do await: {correlationIdBefore}");

await Task.Delay(100);

var correlationIdAfter = CorrelationContext.Current;
Console.WriteLine($"Após o await: {correlationIdAfter}");
Console.WriteLine($"Preservado: {correlationIdBefore == correlationIdAfter}"); // True
```

**Output esperado:**
```
Antes do await: a1b2c3d4e5f6789012345678901234ab
Após o await: a1b2c3d4e5f6789012345678901234ab
Preservado: True
```

## Formato do Correlation-ID

O correlation-id é um GUID formatado **sem hífens** (32 caracteres):

- ✅ Formato correto: `a1b2c3d4e5f6789012345678901234ab` (32 caracteres)
- ❌ Formato incorreto: `a1b2c3d4-e5f6-7890-1234-5678901234ab` (36 caracteres com hífens)

## Próximos Passos

Agora que você sabe usar o `CorrelationContext`, vamos ver como integrá-lo com ASP.NET Core na [Lição 4: ASP.NET Core](04-aspnet-core.md).

