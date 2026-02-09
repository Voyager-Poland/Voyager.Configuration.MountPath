# Voyager.Configuration.MountPath Documentation

Welcome to the Voyager.Configuration.MountPath documentation!

## 📚 Documentation Index

### Getting Started
- [Main README](../README.md) - Installation, quick start, and examples
- [ROADMAP](ROADMAP.md) - Planned improvements and feature roadmap

### Architecture
- [Architecture Decision Records (ADRs)](adr/) - Important architectural decisions
  - [ADR-001: Extension Methods Organization](adr/ADR-001-extension-methods-organization.md)

### Migration Guides
- [Migration Guide](MIGRATION.md) - Upgrading from version 1.x to 2.x (coming soon)

### API Reference
- Coming soon

## 🏗️ Architecture Overview

### Key Components

```
Voyager.Configuration.MountPath/
├── Settings                    - Configuration settings container
├── SettingsDefaults           - Default configuration values
├── SettingsProvider           - ISettingsProvider implementation
├── Encryption/
│   ├── IEncryptor            - Encryption abstraction
│   ├── Encryptor             - Main encryptor implementation
│   ├── ICipherProvider       - Low-level cipher abstraction
│   ├── LegacyDesCipherProvider - DES encryption (deprecated)
│   └── IEncryptorFactory     - Factory for creating encryptors
└── Microsoft.Extensions.DependencyInjection/
    ├── ConfigurationExtension           - Mount configuration extensions
    ├── ConfigurationEncryptedExtension  - Encrypted configuration extensions
    └── ServiceCollectionExtensions      - DI registration
```

### Design Principles

The library follows SOLID principles:

1. **Single Responsibility Principle**: Each class has one well-defined responsibility
2. **Open/Closed Principle**: Extensible through interfaces without modifying existing code
3. **Liskov Substitution Principle**: Interfaces can be substituted with custom implementations
4. **Interface Segregation Principle**: Focused interfaces (IEncryptor, ISettingsProvider, ICipherProvider)
5. **Dependency Inversion Principle**: Depends on abstractions (interfaces), not concrete implementations

## 🔒 Security

### Current Encryption

The library currently uses **DES encryption** for backward compatibility. This is deprecated and should only be used for:
- Migrating existing encrypted configuration
- Maintaining compatibility with legacy systems

### Future Encryption (Planned)

Version 2.0+ will introduce **AES-256-GCM** encryption:
- Modern, secure encryption algorithm
- Built-in authentication (prevents tampering)
- Random IV per value
- PBKDF2 key derivation

See [ROADMAP Phase 1](ROADMAP.md#faza-1-bezpieczeństwo-krytyczne) for details.

## 🧪 Testing

The test suite includes:
- Unit tests for individual components
- Integration tests for configuration loading
- Encryption/decryption tests
- Environment-specific configuration tests

Run tests:
```bash
dotnet test
```

## 🤝 Contributing

See [Contributing Guidelines](../README.md#contributing) in the main README.

## 📋 Roadmap

See [ROADMAP.md](ROADMAP.md) for:
- Planned features
- Security improvements
- API enhancements
- Documentation plans

## 📖 Additional Resources

- [ASP.NET Core Configuration](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
- [.NET Generic Host](https://docs.microsoft.com/en-us/dotnet/core/extensions/generic-host)
- [Docker Configuration](https://docs.docker.com/compose/compose-file/)
- [Kubernetes ConfigMaps](https://kubernetes.io/docs/concepts/configuration/configmap/)
