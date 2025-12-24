# Installation

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
  <PackageReference Include="WhiteBeard.Traceability" Version="1.1.0" />
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
- `Microsoft.Extensions.Http` (8.0.0)
- `Microsoft.Extensions.Logging.Abstractions` (8.0.0)
- `Polly` (8.3.1)

### .NET Framework 4.8
- `Polly` (7.2.3)
- `Microsoft.AspNet.WebApi.Client` (5.2.9)
- `Microsoft.Extensions.Logging.Abstractions` (2.1.1)
- `Microsoft.Extensions.DependencyInjection.Abstractions` (2.1.1)

### Common
- `Serilog` (3.1.1) - PrivateAssets: all

All dependencies are automatically installed when you install the Traceability package.

## Next Steps

After installation, see:
- [Quick Start](getting-started.md) to get started quickly
- [User Manual](user-guide/index.md) for a complete guide


