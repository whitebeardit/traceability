# Installation

![NuGet Version](https://img.shields.io/nuget/v/WhiteBeard.Traceability.svg?style=flat-square&label=version)

## Requirements

- .NET 8.0 or higher (for ASP.NET Core)
- .NET Framework 4.8 (for ASP.NET Web API and Traditional ASP.NET)

## Installation via NuGet

### .NET CLI

```bash
dotnet add package WhiteBeard.Traceability
```

### Package Manager Console

```powershell
Install-Package WhiteBeard.Traceability
```

### PackageReference (.csproj file)

```xml
<ItemGroup>
  <!-- Prefer omitting an explicit version and letting NuGet resolve the latest stable version -->
  <PackageReference Include="WhiteBeard.Traceability" />
</ItemGroup>
```

## Installation Verification

After installation, you should be able to import the namespace:

```csharp
using Traceability;
using Traceability.Extensions;
```

## Dependencies

The Traceability package includes the following dependencies:

### .NET 8.0
- `Microsoft.AspNetCore.Http.Abstractions` (2.2.0)
- `Microsoft.AspNetCore.Hosting.Abstractions` (2.2.0)
- `Microsoft.Extensions.Http` (8.0.0)
- `Microsoft.Extensions.Logging.Abstractions` (8.0.0)
- `Polly` (8.3.1)

### .NET Framework 4.8
- `Polly` (7.2.3)
- `Microsoft.AspNet.WebApi.Client` (5.2.9)
- `Microsoft.AspNet.WebApi.Core` (5.2.9)
- `Microsoft.AspNet.Mvc` (5.2.9)
- `Microsoft.Web.Infrastructure` (2.0.1)
- `Microsoft.Extensions.Logging.Abstractions` (2.1.1)
- `Microsoft.Extensions.DependencyInjection.Abstractions` (2.1.1)

### Common
- `Serilog` (3.1.1) - PrivateAssets: all

All dependencies are automatically installed when you install the Traceability package.

## Next Steps

After installation, see:
- [Quick Start](getting-started.md) to get started quickly
- [User Manual](user-guide/index.md) for a complete guide


