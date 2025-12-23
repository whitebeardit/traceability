# Documentação do Traceability

Bem-vindo à documentação completa do pacote Traceability para gerenciamento automático de correlation-id em aplicações .NET.

## Início Rápido

- [Quick Start](getting-started.md) - Comece a usar em minutos
- [Instalação](installation.md) - Como instalar o pacote

## Guias

- [Manual do Usuário](user-guide/index.md) - Guia progressivo para iniciantes
- [Configuração](configuration.md) - Opções de configuração detalhadas
- [Referência da API](api-reference.md) - Documentação completa da API
- [Tópicos Avançados](advanced.md) - Recursos avançados e casos de uso

## Exemplos

- [ASP.NET Core](examples/aspnet-core.md) - Exemplos para .NET 8
- [ASP.NET Framework](examples/aspnet-framework.md) - Exemplos para .NET Framework 4.8
- [Console Application](examples/console.md) - Exemplos para aplicações console
- [Requisições HTTP](examples/http-requests.md) - Exemplos de requisições HTTP

## Suporte

- [Troubleshooting](troubleshooting.md) - Solução de problemas comuns
- [FAQ](troubleshooting.md#faq) - Perguntas frequentes

## Documentação Técnica

Para desenvolvedores que desejam contribuir ou entender a arquitetura interna:

- [CI/CD e Releases](development/ci-cd.md) - Pipeline de CI/CD e processo de release
- [Documentação para LLMs](../AGENTS.md) - Arquitetura e guia técnico completo

## O que é Traceability?

Traceability é um pacote NuGet que gerencia automaticamente correlation-id em aplicações .NET, permitindo rastrear requisições através de múltiplos serviços em arquiteturas distribuídas.

### Características Principais

- ✅ Gerenciamento automático de correlation-id usando `AsyncLocal`
- ✅ Suporte para .NET 8.0 e .NET Framework 4.8
- ✅ Middleware para ASP.NET Core (.NET 8)
- ✅ HttpModule e MessageHandler para ASP.NET (.NET Framework 4.8)
- ✅ Integração automática com HttpClient
- ✅ Suporte para Serilog e Microsoft.Extensions.Logging
- ✅ Integração com Polly para políticas de resiliência
- ✅ Propagação automática em chamadas HTTP encadeadas

### Quando Usar?

Use o Traceability quando você precisa:

1. **Rastreabilidade em Microserviços**: Rastrear uma requisição através de múltiplos serviços
2. **Debugging Simplificado**: Identificar rapidamente todos os logs relacionados a uma requisição
3. **Análise de Performance**: Medir o tempo total de processamento através de múltiplos serviços
4. **Monitoramento e Observabilidade**: Correlacionar métricas, traces e logs de diferentes serviços

## Frameworks Suportados

- **.NET 8.0**: Suporte completo para ASP.NET Core
- **.NET Framework 4.8**: Suporte para ASP.NET Web API e ASP.NET Tradicional

## Instalação Rápida

```bash
dotnet add package WhiteBeard.Traceability
```

Para mais detalhes, consulte [Instalação](installation.md).

## Exemplo Rápido

```csharp
using Traceability.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Zero configuração - tudo é automático!
builder.Services.AddTraceability("MyService");
builder.Services.AddControllers();

var app = builder.Build();
app.MapControllers();
app.Run();
```

Para mais exemplos, consulte [Quick Start](getting-started.md) ou o [Manual do Usuário](user-guide/index.md).

