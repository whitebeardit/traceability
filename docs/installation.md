# Instalação

## Requisitos

- .NET 8.0 ou superior (para ASP.NET Core)
- .NET Framework 4.8 (para ASP.NET Web API e ASP.NET Tradicional)

## Instalação via NuGet

### .NET CLI

```bash
dotnet add package Traceability
```

### Package Manager Console

```powershell
Install-Package Traceability
```

### PackageReference (arquivo .csproj)

```xml
<ItemGroup>
  <PackageReference Include="Traceability" Version="1.0.0" />
</ItemGroup>
```

## Verificação da Instalação

Após a instalação, você deve conseguir importar o namespace:

```csharp
using Traceability;
using Traceability.Extensions;
```

## Dependências

O pacote Traceability inclui as seguintes dependências:

### .NET 8.0
- `Microsoft.AspNetCore.Http.Abstractions` (2.2.0)
- `Microsoft.Extensions.Http` (8.0.0)
- `Microsoft.Extensions.Logging.Abstractions` (8.0.0)
- `Polly` (8.3.1)

### .NET Framework 4.8
- `Polly` (7.2.3)
- `Microsoft.AspNet.WebApi.Client` (5.2.9)
- `Microsoft.Extensions.Logging.Abstractions` (2.1.1)
- `Microsoft.Extensions.DependencyInjection.Abstractions` (2.1.1)

### Comum
- `Serilog` (3.1.1) - PrivateAssets: all

Todas as dependências são instaladas automaticamente quando você instala o pacote Traceability.

## Próximos Passos

Após a instalação, consulte:
- [Quick Start](getting-started.md) para começar rapidamente
- [Manual do Usuário](user-guide/index.md) para um guia completo


