# Regras e Constraints para LLMs

## Regras Obrigatórias

1. **Sempre usar `AsyncLocal` para contexto assíncrono**
   - ❌ Nunca usar `ThreadLocal`
   - ❌ Nunca usar variáveis estáticas simples
   - ✅ Sempre usar `AsyncLocal<string>`

2. **Sempre compilar condicionalmente código específico de framework**
   - ❌ Nunca misturar código .NET 8 e .NET Framework sem `#if`
   - ✅ Usar `#if NET8_0` para código ASP.NET Core
   - ✅ Usar `#if NET48` para código .NET Framework
   - ✅ Sempre fechar com `#endif`

3. **Header padrão: `X-Correlation-Id`**
   - ✅ Usar constante `"X-Correlation-Id"` (futuramente via `TraceabilityOptions`)
   - ❌ Nunca usar outros nomes de header sem configuração

4. **GUID formatado sem hífens (32 caracteres)**
   - ✅ Usar `Guid.NewGuid().ToString("N")`
   - ❌ Nunca usar `ToString()` sem parâmetro (36 caracteres)

5. **Nunca modificar correlation-id existente**
   - ✅ Se header existe na requisição, usar o valor
   - ✅ Se header não existe, gerar novo
   - ❌ Nunca sobrescrever correlation-id existente no contexto

6. **Isolamento assíncrono obrigatório**
   - ✅ Cada contexto assíncrono isolado mantém seu próprio correlation-id
   - ✅ `Task.Run()` cria novo contexto isolado
   - ✅ `await` preserva contexto

## Constraints de Design

1. **Sem dependências circulares**: Componentes não devem depender uns dos outros circularmente
2. **Thread-safe**: Todas as operações devem ser thread-safe
3. **Async-safe**: Todas as operações devem funcionar corretamente com async/await
4. **Zero configuração por padrão**: Funciona sem configuração, mas permite customização

## Validações Obrigatórias

Ao adicionar/modificar código, verificar:
- [ ] Compilação condicional correta (`#if NET8_0` / `#if NET48`)
- [ ] Uso de `AsyncLocal` para contexto assíncrono
- [ ] Header `X-Correlation-Id` usado consistentemente
- [ ] GUID gerado sem hífens (`ToString("N")`)
- [ ] Não modifica correlation-id existente
- [ ] Thread-safe e async-safe
- [ ] XML comments adicionados/atualizados

## Checklist de Validação para Modificações

### Código
- [ ] Compilação condicional correta (`#if NET8_0` / `#if NET48`)
- [ ] Uso de `AsyncLocal` para contexto assíncrono
- [ ] Header `X-Correlation-Id` usado consistentemente
- [ ] GUID gerado sem hífens (`ToString("N")`)
- [ ] Não modifica correlation-id existente
- [ ] Thread-safe e async-safe
- [ ] XML comments adicionados/atualizados

### Testes
- [ ] Testes unitários adicionados/atualizados
- [ ] Testes passam para ambos os frameworks
- [ ] Cobertura de casos edge

### Documentação
- [ ] README.md atualizado se necessário
- [ ] AGENTS.md atualizado se necessário
- [ ] Exemplos de uso atualizados

### Compatibilidade
- [ ] Funciona em .NET 8.0
- [ ] Funciona em .NET Framework 4.8
- [ ] Sem breaking changes (ou documentados)


