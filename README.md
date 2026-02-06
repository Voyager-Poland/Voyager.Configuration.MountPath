# Voyager.Configuration.MountPath

[![NuGet](https://img.shields.io/nuget/v/Voyager.Configuration.MountPath.svg)](https://www.nuget.org/packages/Voyager.Configuration.MountPath/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

An ASP.NET Core extension for organizing JSON configuration files with support for environment-specific settings, encrypted values, and Docker/Kubernetes mount paths.

## About

This library enables reading JSON configuration files from a configurable path, designed for containerized environments like Docker and Kubernetes. By mounting configuration files at runtime, you can:

- Keep sensitive data out of container images
- Update configuration without rebuilding images
- Support environment-specific settings (Development, Production, etc.)
- Encrypt sensitive configuration values

## Features

- 🐳 **Container-friendly**: Mount configuration from external volumes
- 🔒 **Encryption support**: Encrypt sensitive values in configuration files
- 🎯 **Environment-specific**: Automatic environment-based configuration loading
- 🔌 **Dependency Injection**: Full DI support with interfaces
- 🏗️ **SOLID architecture**: Interface-based design for testability
- 📦 **Multi-targeting**: Supports .NET 4.8, .NET Core 3.1, .NET 6.0, and .NET 8.0

## Installation

```bash
dotnet add package Voyager.Configuration.MountPath
```

## Quick Start

### Basic Configuration

```csharp
using Microsoft.Extensions.DependencyInjection;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureAppConfiguration((context, config) =>
{
    config.AddMountConfiguration(context.HostingEnvironment.GetSettingsProvider());
});
```

**File structure:**
```
YourApp/
├── bin/
│   └── config/
│       ├── appsettings.json
│       └── appsettings.Development.json
```

The library loads `appsettings.json` from the `config` folder, then applies environment-specific overrides from `appsettings.{EnvironmentName}.json` based on the `ASPNETCORE_ENVIRONMENT` variable.

### Custom Settings

Configure custom file names, paths, or environments:

```csharp
builder.ConfigureAppConfiguration((context, config) =>
{
    config.AddMountConfiguration(settings =>
    {
        settings.FileName = "myconfig";           // Default: "appsettings"
        settings.ConfigMountPath = "configuration"; // Default: "config"
        settings.HostingName = "Production";      // Default: from ASPNETCORE_ENVIRONMENT
        settings.Optional = false;                // Default: true
    });
});
```

### Require Environment File

Force the presence of an environment-specific configuration file:

```csharp
builder.ConfigureAppConfiguration((context, config) =>
{
    config.AddMountConfiguration(context.HostingEnvironment.GetSettingsProviderForce());
});
```

### Loading Multiple Configuration Files

```csharp
builder.ConfigureAppConfiguration((context, config) =>
{
    var provider = context.HostingEnvironment.GetSettingsProvider();

    config.AddMountConfiguration(provider, "appsettings", "database", "logging");
});
```

## Encrypted Configuration

### Basic Encryption

Encrypt sensitive values in your configuration files:

```csharp
builder.ConfigureAppConfiguration((context, config) =>
{
    var encryptionKey = Environment.GetEnvironmentVariable("ENCRYPTION_KEY");
    var provider = context.HostingEnvironment.GetSettingsProvider();

    config.AddEncryptedMountConfiguration(encryptionKey, provider, "secrets");
});
```

**secrets.json:**
```json
{
    "ConnectionStrings": {
        "Database": "U2VydmVyPWxvY2FsaG9zdDtEYXRhYmFzZT1teWRiO1VzZXI9c2E7UGFzc3dvcmQ9..."
    }
}
```

Values in the encrypted file are automatically decrypted when loaded.

### Dependency Injection for Encryption

Register encryption services in DI:

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register default encryption services
builder.Services.AddVoyagerEncryption();

// Or use a custom encryptor factory
builder.Services.AddVoyagerEncryption<MyCustomEncryptorFactory>();
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

## Docker Example

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
      - ENCRYPTION_KEY=${ENCRYPTION_KEY}
```

**config/appsettings.Production.json:**
```json
{
  "ConnectionStrings": {
    "Database": "Server=db;Database=prod;..."
  }
}
```

## Kubernetes Example

**ConfigMap:**
```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: app-config
data:
  appsettings.json: |
    {
      "Logging": {
        "LogLevel": {
          "Default": "Information"
        }
      }
    }
```

**Deployment:**
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
        - name: config
          mountPath: /app/config
          readOnly: true
      volumes:
      - name: config
        configMap:
          name: app-config
```

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

## Migration Guide

For migration from version 1.x to 2.x, see [Migration Guide](docs/MIGRATION.md).

For the full roadmap and planned improvements, see [ROADMAP.md](docs/ROADMAP.md).

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
