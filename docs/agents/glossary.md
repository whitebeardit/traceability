# Glossário de Termos

- **Correlation-ID**: Identificador único usado para rastrear uma requisição através de múltiplos serviços
- **AsyncLocal**: Classe do .NET que armazena valores específicos de um contexto assíncrono
- **DelegatingHandler**: Classe base para handlers HTTP que podem ser encadeados
- **Enricher**: Componente do Serilog que adiciona propriedades aos eventos de log
- **ScopeProvider**: Componente do Microsoft.Extensions.Logging que gerencia scopes de logging
- **Conditional Compilation**: Compilação condicional usando diretivas `#if` para incluir código baseado em condições
- **Middleware**: Componente no pipeline HTTP que processa requisições e respostas
- **MessageHandler**: Handler no pipeline do ASP.NET Web API
- **HttpModule**: Módulo no pipeline do ASP.NET tradicional
- **Source**: Campo que identifica a origem/serviço que está gerando os logs
- **Traceability**: Capacidade de rastrear uma requisição através de múltiplos serviços usando correlation-id
- **Socket Exhaustion**: Problema que ocorre quando muitas conexões HTTP são criadas sem reutilização, esgotando os sockets disponíveis
- **IHttpClientFactory**: Factory do .NET que gerencia o pool de conexões HTTP, prevenindo socket exhaustion

