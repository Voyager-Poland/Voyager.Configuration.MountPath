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

> **⚠️ DEPRECATION NOTICE: Built-in Encryption**
> The built-in encryption feature is **deprecated** and will be removed in **version 3.0**.
> We recommend migrating to external secret management tools like **[Mozilla SOPS](https://github.com/mozilla/sops)**, Kubernetes Secrets, or cloud providers (Azure Key Vault, AWS Secrets Manager).
> See [ADR-003: Encryption Delegation](docs/adr/ADR-003-encryption-delegation-to-external-tools.md) for migration guidance and rationale.

## Features

- 📁 **File-based organization**: Pass file names (without extensions) as parameters
- 🎯 **Conditional loading**: Automatic environment-based file selection
- 🐳 **Container-friendly**: Mount configuration from external volumes
- 🔄 **Hot reload**: Automatically reload when configuration files change
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

### ⚠️ DEPRECATED: Encrypted Configuration Files

> **This feature is deprecated and will be removed in version 3.0.**
> Please migrate to external secret management tools.

**Built-in encryption (deprecated):**

```csharp
builder.ConfigureAppConfiguration((context, config) =>
{
    var provider = context.HostingEnvironment.GetSettingsProvider();
    var encryptionKey = Environment.GetEnvironmentVariable("ENCRYPTION_KEY");

    // Regular files (plain text)
    config.AddMountConfiguration(provider, "logging");
    config.AddMountConfiguration(provider, "appsettings");

    // ⚠️ DEPRECATED - Use SOPS instead
    config.AddEncryptedMountConfiguration(encryptionKey, provider, "secrets");
    config.AddEncryptedMountConfiguration(encryptionKey, provider, "connectionstrings");
});
```

**Recommended alternative: Mozilla SOPS**

Encrypt configuration files using [SOPS](https://github.com/mozilla/sops) before loading:

```bash
# Encrypt files with SOPS (one-time setup)
sops -e config/secrets.json > config/secrets.json

# Decrypt in your deployment script or init container
sops -d /config-encrypted/secrets.json > /config/secrets.json
```

```csharp
// Load decrypted files normally - no code changes needed!
builder.ConfigureAppConfiguration((context, config) =>
{
    var provider = context.HostingEnvironment.GetSettingsProvider();

    config.AddMountConfiguration(provider, "logging");
    config.AddMountConfiguration(provider, "secrets");     // Already decrypted by SOPS
    config.AddMountConfiguration(provider, "appsettings");
});
```

**Why migrate to SOPS?**
- ✅ **More secure**: AES-256-GCM instead of legacy DES
- ✅ **GitOps-friendly**: Encrypted files can be committed to Git
- ✅ **Key management**: Integrates with AWS KMS, Azure Key Vault, GCP KMS, Age
- ✅ **No code changes**: Decrypt before loading, library code stays the same
- ✅ **Better tooling**: Edit encrypted files without manual decrypt/encrypt cycle

See [ADR-003: Encryption Delegation to External Tools](docs/adr/ADR-003-encryption-delegation-to-external-tools.md) for:
- Detailed SOPS setup guide
- Migration examples for Kubernetes and Supervisor
- Comparison of secret management solutions
- Step-by-step migration path

## Security Considerations

### ⚠️ Encryption Feature Deprecated

**Built-in encryption is deprecated** and will be removed in version 3.0.

- **Current implementation**: Legacy DES encryption (56-bit, insecure)
- **Status**: Deprecated for backward compatibility only
- **Recommendation**: Migrate to external secret management tools

### Recommended Secret Management Solutions

Instead of built-in encryption, use industry-standard tools:

1. **[Mozilla SOPS](https://github.com/mozilla/sops)** - File encryption for GitOps workflows
   - Encrypts JSON/YAML values while keeping structure readable
   - Supports AWS KMS, Azure Key Vault, GCP KMS, Age
   - Git-friendly diffs
   - **Best for**: GitOps, encrypted configs in Git

2. **Kubernetes Secrets + Sealed Secrets**
   - Native Kubernetes secret management
   - Sealed Secrets for GitOps-safe encrypted secrets
   - **Best for**: Kubernetes deployments

3. **Cloud Secret Managers**
   - Azure Key Vault, AWS Secrets Manager, GCP Secret Manager
   - Enterprise-grade key rotation and audit logging
   - **Best for**: Cloud-native applications

4. **dotnet user-secrets**
   - For development environments only
   - **Best for**: Local development

See [ADR-003](docs/adr/ADR-003-encryption-delegation-to-external-tools.md) for detailed migration guide.

### Best Practices

1. **External Secret Management**: Use SOPS, Kubernetes Secrets, or cloud providers
2. **File Permissions**: Ensure configuration files have appropriate read permissions
3. **Container Security**: Mount configuration volumes as read-only (`:ro`)
4. **Key Management**: Never hardcode secrets in source code or images
5. **Separation of Concerns**: Configuration loading ≠ secret management

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
  - [ADR-002: Settings Builder Pattern](docs/adr/ADR-002-settings-builder-pattern.md) - Why Action<Settings> over Builder Pattern
  - [ADR-003: Encryption Delegation to External Tools](docs/adr/ADR-003-encryption-delegation-to-external-tools.md) - **Migration guide from built-in encryption to SOPS**
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
