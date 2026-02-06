# Voyager.Configuration.MountPath

[![NuGet](https://img.shields.io/nuget/v/Voyager.Configuration.MountPath.svg)](https://www.nuget.org/packages/Voyager.Configuration.MountPath/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

An ASP.NET Core extension for organizing JSON configuration files with support for environment-specific settings, encrypted values, and Docker/Kubernetes mount paths.

## About

This library provides a simple way to organize JSON configuration files by **file name** (without extensions) and load them conditionally based on **environment variables**. Designed for containerized environments like Docker and Kubernetes, it allows you to:

- **Organize configuration by concern**: Separate files for database, logging, services, etc.
- **Load files conditionally**: Automatically loads environment-specific variants (e.g., `database.json` + `database.Production.json`)
- **Mount at runtime**: Keep configuration outside container images using volume mounts
- **Update without rebuilding**: Change configuration without rebuilding or redeploying
- **Optional encryption**: Encrypt sensitive values when needed (extension feature)

## Features

- 📁 **File-based organization**: Pass file names (without extensions) as parameters
- 🎯 **Conditional loading**: Automatic environment-based file selection
- 🐳 **Container-friendly**: Mount configuration from external volumes
- 🔄 **Hot reload**: Automatically reload when configuration files change
- 🔒 **Optional encryption**: Encrypt sensitive values (extension feature)
- 🔌 **Dependency Injection**: Full DI support with interfaces
- 🏗️ **SOLID architecture**: Interface-based design for testability
- 📦 **Multi-targeting**: Supports .NET 4.8, .NET Core 3.1, .NET 6.0, and .NET 8.0

## Installation

```bash
dotnet add package Voyager.Configuration.MountPath
```

## Quick Start

### Basic Usage: Single Configuration File

Pass file names (without `.json` extension) as parameters:

```csharp
using Microsoft.Extensions.DependencyInjection;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureAppConfiguration((context, config) =>
{
    var provider = context.HostingEnvironment.GetSettingsProvider();

    // Loads: appsettings.json + appsettings.{Environment}.json
    config.AddMountConfiguration(provider, "appsettings");
});
```

**File structure:**
```
YourApp/
├── bin/
│   └── config/
│       ├── appsettings.json           # Base configuration
│       └── appsettings.Production.json # Environment-specific overrides
```

**How it works:**
1. Loads base file: `appsettings.json` (required)
2. Loads environment file: `appsettings.{ASPNETCORE_ENVIRONMENT}.json` (optional by default)
3. Environment-specific values override base values

### Customizing Settings

Configure custom mount paths or require environment-specific files:

```csharp
builder.ConfigureAppConfiguration((context, config) =>
{
    config.AddMountConfiguration(settings =>
    {
        settings.FileName = "myconfig";           // File name without extension
        settings.ConfigMountPath = "configuration"; // Default: "config"
        settings.HostingName = "Production";      // Override environment detection
        settings.Optional = false;                // Require environment file
    });
});
```

**Require environment-specific file:**
```csharp
builder.ConfigureAppConfiguration((context, config) =>
{
    // Throws exception if appsettings.Production.json doesn't exist
    config.AddMountConfiguration(context.HostingEnvironment.GetSettingsProviderForce());
});
```

### Organizing Configuration by Concern

The key feature: organize configuration into **separate files by concern**, each loaded conditionally based on environment:

```csharp
builder.ConfigureAppConfiguration((context, config) =>
{
    var provider = context.HostingEnvironment.GetSettingsProvider();

    // Each file name is loaded as: {name}.json + {name}.{Environment}.json
    config.AddMountConfiguration(provider, "appsettings");  // App settings
    config.AddMountConfiguration(provider, "database");     // Database config
    config.AddMountConfiguration(provider, "logging");      // Logging config
    config.AddMountConfiguration(provider, "services");     // External services
});
```

**File structure:**
```
config/
├── appsettings.json
├── appsettings.Production.json
├── database.json
├── database.Production.json
├── logging.json
├── logging.Production.json
├── services.json
└── services.Production.json
```

**Benefits:**
- **Separation of concerns**: Each file handles one aspect of configuration
- **Conditional loading**: Different values for Development, Production, Staging, etc.
- **Easier management**: Update only relevant files without touching others

## Docker and Kubernetes Examples

### Docker Example

Mount configuration files at runtime to separate concerns:

**Dockerfile:**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY bin/Release/net8.0/publish .

# Configuration will be mounted at runtime
ENTRYPOINT ["dotnet", "YourApp.dll"]
```

**docker-compose.yml:**
```yaml
services:
  myapp:
    image: myapp:latest
    volumes:
      - ./config:/app/config:ro  # Mount config folder
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
```

**config/ directory:**
```
config/
├── database.json              # Base database config
├── database.Production.json   # Production overrides
├── logging.json               # Base logging config
└── logging.Production.json    # Production logging settings
```

### Kubernetes Example

Use ConfigMaps for different configuration concerns:

**database-config.yaml:**
```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: database-config
data:
  database.json: |
    {
      "ConnectionStrings": {
        "Default": "Server=db;Database=myapp;..."
      }
    }
```

**logging-config.yaml:**
```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: logging-config
data:
  logging.json: |
    {
      "Logging": {
        "LogLevel": {
          "Default": "Information"
        }
      }
    }
```

**deployment.yaml:**
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: myapp
spec:
  template:
    spec:
      containers:
      - name: myapp
        image: myapp:latest
        volumeMounts:
        - name: database-config
          mountPath: /app/config/database.json
          subPath: database.json
        - name: logging-config
          mountPath: /app/config/logging.json
          subPath: logging.json
      volumes:
      - name: database-config
        configMap:
          name: database-config
      - name: logging-config
        configMap:
          name: logging-config
```

## Dependency Injection

Register configuration services:

```csharp
// Register settings provider
builder.Services.AddVoyagerConfiguration();

// Register custom settings provider
builder.Services.AddVoyagerConfiguration<MyCustomSettingsProvider>();

// Use in your services
public class MyService
{
    private readonly ISettingsProvider _settingsProvider;

    public MyService(ISettingsProvider settingsProvider)
    {
        _settingsProvider = settingsProvider;
    }
}
```

## Advanced Usage

### Custom Settings Provider

Implement `ISettingsProvider` for custom behavior:

```csharp
public class MySettingsProvider : ISettingsProvider
{
    public Settings GetSettings(string filename = "appsettings")
    {
        return new Settings
        {
            FileName = filename,
            ConfigMountPath = "/custom/path",
            HostingName = "Production"
        };
    }
}

// Register in DI
builder.Services.AddVoyagerConfiguration<MySettingsProvider>();
```

### Optional: Encrypted Configuration Files

**Extension feature** for encrypting sensitive configuration values:

```csharp
builder.ConfigureAppConfiguration((context, config) =>
{
    var provider = context.HostingEnvironment.GetSettingsProvider();
    var encryptionKey = Environment.GetEnvironmentVariable("ENCRYPTION_KEY");

    // Regular files (plain text)
    config.AddMountConfiguration(provider, "logging");
    config.AddMountConfiguration(provider, "appsettings");

    // Encrypted files (extension feature)
    config.AddEncryptedMountConfiguration(encryptionKey, provider, "secrets");
    config.AddEncryptedMountConfiguration(encryptionKey, provider, "connectionstrings");
});
```

**Register encryption services:**
```csharp
builder.Services.AddVoyagerEncryption();
```

**Low-level API** for individual encrypted files:
```csharp
// Fine-grained control over encrypted files
config.AddEncryptedJsonFile("secrets.json", encryptionKey, optional: false, reloadOnChange: true);
config.AddEncryptedJsonFile("secrets.Production.json", encryptionKey, optional: true, reloadOnChange: true);
```

> **Note**: Encryption is an optional extension feature. The core library is about organizing and conditionally loading configuration files.

## Security Considerations

### Encryption

- **Current**: Uses legacy DES encryption (deprecated, for backward compatibility only)
- **Recommended**: Migrate to AES-256-GCM in future versions
- **Key Management**: Never hardcode encryption keys in source code
  - Use environment variables
  - Use secret management systems (Azure Key Vault, AWS Secrets Manager, etc.)
  - Use Kubernetes secrets

### Best Practices

1. **Minimum Key Length**: Use keys with at least 8 characters (longer for production)
2. **Secret Storage**: Store encryption keys securely outside the application
3. **File Permissions**: Ensure configuration files have appropriate read permissions
4. **Container Security**: Mount configuration volumes as read-only (`:ro`)

## Architecture

This library follows SOLID principles and modern .NET design patterns:

- **Single Responsibility Principle**: Each class has one well-defined responsibility
- **Interface-based design**: `IEncryptor`, `ISettingsProvider`, `ICipherProvider`, `IEncryptorFactory`
- **Dependency Injection**: Full support for DI container registration
- **Extension methods**: Organized by responsibility for better maintainability

### Extension Methods Organization

The library provides extension methods organized by their specific concerns:

- **`ConfigurationExtension`**: Non-encrypted mount configuration
- **`EncryptedMountConfigurationExtensions`**: Encrypted mount configuration (high-level API)
- **`EncryptedJsonFileExtensions`**: Encrypted JSON file operations (low-level API)
- **`ServiceCollectionExtensions`**: Dependency injection registration

All extension methods are placed in the `Microsoft.Extensions.DependencyInjection` namespace following .NET conventions.

> **Note**: The `ConfigurationEncryptedExtension` class is deprecated and will be removed in version 3.0. Use `EncryptedMountConfigurationExtensions` and `EncryptedJsonFileExtensions` instead.

### Documentation

- **[Architecture Decision Records](docs/adr/)** - Architectural decisions and their rationale
  - [ADR-001: Extension Methods Organization](docs/adr/ADR-001-extension-methods-organization.md) - How extension methods are organized
- **[ROADMAP](docs/ROADMAP.md)** - Planned improvements and feature roadmap
- **[Documentation Index](docs/README.md)** - Complete documentation overview

## Migration Guide

For migration from version 1.x to 2.x, see [Migration Guide](docs/MIGRATION.md).

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Authors

- [@andrzejswistowski](https://github.com/AndrzejSwistowski) - Original author and maintainer

See also the list of [contributors](https://github.com/Voyager-Poland) who participated in this project.

## Acknowledgements

- Przemysław Wróbel - Icon design
- All contributors who have helped improve this library

## Support

If you encounter any issues or have suggestions:
- Open an issue on [GitHub Issues](https://github.com/Voyager-Poland/Voyager.Configuration.MountPath/issues)
- Check the [documentation](docs/)
- Review the [ROADMAP](docs/ROADMAP.md) for planned features
- Read [Architecture Decision Records](docs/adr/) for design rationale
