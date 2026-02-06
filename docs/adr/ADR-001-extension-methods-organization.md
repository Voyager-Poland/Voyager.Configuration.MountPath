# ADR-001: Organization of Configuration Extension Methods

## Status

Accepted

**Date**: 2026-02-06

## Context

The library provides extension methods for `IConfigurationBuilder` to simplify configuration loading in ASP.NET Core applications. These extension methods need to be organized in a way that:

1. Follows the .NET convention of placing extension methods in the extended type's namespace
2. Adheres to Single Responsibility Principle (SRP)
3. Provides clear separation between different concerns
4. Makes the API discoverable through IntelliSense

Initial implementation had all encrypted configuration methods in a single file (`ConfigurationEncryptedExtension.cs`), which violated SRP by mixing two distinct responsibilities:
- High-level mount configuration API
- Low-level JSON file configuration API

## Decision

We organize extension methods according to their responsibility, with each file containing methods for a single concern:

### File Organization

```
Microsoft.Extensions.DependencyInjection/
├── ConfigurationExtension.cs              // Non-encrypted mount configuration
├── EncryptedMountConfigurationExtensions.cs  // Encrypted mount configuration (planned)
├── EncryptedJsonFileExtensions.cs           // Encrypted JSON file configuration (planned)
└── ServiceCollectionExtensions.cs           // DI registration
```

### Namespace Convention

All configuration extension methods are placed in `Microsoft.Extensions.DependencyInjection` namespace, following .NET's convention for configuration extensions. This ensures:
- Extensions appear automatically in IntelliSense when users have `using Microsoft.Extensions.DependencyInjection;`
- Consistency with ASP.NET Core configuration patterns
- No additional `using` directives required

### Responsibility Separation

1. **ConfigurationExtension.cs**
   - Purpose: Non-encrypted configuration from mount paths
   - Methods: `AddMountConfiguration` overloads
   - Responsibility: Loading plain JSON files from configurable mount paths

2. **EncryptedMountConfigurationExtensions.cs** (to be created)
   - Purpose: Encrypted configuration from mount paths
   - Methods: `AddEncryptedMountConfiguration` overloads
   - Responsibility: Loading and decrypting JSON files from mount paths

3. **EncryptedJsonFileExtensions.cs** (to be created)
   - Purpose: Low-level encrypted JSON file operations
   - Methods: `AddEncryptedJsonFile` overloads
   - Responsibility: Adding individual encrypted JSON files with full control

4. **ServiceCollectionExtensions.cs**
   - Purpose: Dependency injection registration
   - Methods: `AddVoyagerConfiguration`, `AddVoyagerEncryption`
   - Responsibility: Registering library services in DI container

### Naming Conventions

- File names use plural "Extensions" suffix (e.g., `EncryptedMountConfigurationExtensions.cs`)
- Class names match file names exactly
- Classes are `public static`
- Methods follow pattern: `Add{Feature}{ConfigurationType}`

## Consequences

### Positive

1. **SRP Compliance**: Each file has a single, well-defined responsibility
2. **Maintainability**: Changes to mount configuration don't affect JSON file configuration
3. **Testability**: Easier to test each concern independently
4. **Discoverability**: Users find appropriate methods through IntelliSense
5. **Consistency**: Follows .NET Core configuration extension patterns
6. **Future-proof**: Easy to add new configuration sources without modifying existing files

### Negative

1. **More Files**: Three files instead of one for encrypted configuration
2. **Namespace Confusion**: Some developers might wonder why we use `Microsoft.Extensions.DependencyInjection` namespace
   - Mitigation: This is standard .NET practice, documented in code comments
3. **Initial Learning Curve**: Developers need to understand the distinction between mount configuration and JSON file configuration
   - Mitigation: Clear XML documentation and README examples

### Breaking Changes

None for current version. Future refactoring (splitting `ConfigurationEncryptedExtension.cs`) should:
- Use `[Obsolete]` attribute on old locations
- Provide migration period (one major version)
- Document migration in CHANGELOG

## Implementation Plan

1. ✅ Phase 1: Create `ServiceCollectionExtensions.cs` with DI registration
2. 🔄 Phase 2: Split `ConfigurationEncryptedExtension.cs`:
   - Create `EncryptedMountConfigurationExtensions.cs`
   - Create `EncryptedJsonFileExtensions.cs`
   - Mark old class as `[Obsolete]`
3. 📋 Phase 3: Update documentation and examples

## References

- [ROADMAP.md - Section 7.1](../ROADMAP.md#71-refaktoryzacja-extension-methods-srp--api-consistency)
- [ROADMAP.md - Section 2.2](../ROADMAP.md#22-dependency-injection)
- [Microsoft Docs: Extension Methods Best Practices](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/extension-methods)
- [ASP.NET Core Configuration](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)

## Related ADRs

- ADR-002: Configuration Structure (planned)
- ADR-003: Encryption Strategy (planned)
