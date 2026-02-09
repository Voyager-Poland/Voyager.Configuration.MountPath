# Migration Guide

## Migrating to v2.0

Version 2.0 introduces several breaking changes as part of the deprecation strategy for built-in encryption and code quality improvements following SOLID principles.

### Breaking Changes

#### 1. Removed `ConfigurationEncryptedExtension` Class

**Status:** ❌ REMOVED in v2.0

The obsolete `ConfigurationEncryptedExtension` class has been removed to resolve method ambiguity issues. This class was split into two separate classes following the Single Responsibility Principle.

**Before (v1.x):**
```csharp
using Microsoft.Extensions.DependencyInjection;

config.AddEncryptedMountConfiguration(key, provider, "database");
config.AddEncryptedJsonFile(builder, "secrets.json", key, false, true);
```

**After (v2.0):**
```csharp
using Microsoft.Extensions.DependencyInjection;

// For mount path configuration - use EncryptedMountConfigurationExtensions
config.AddEncryptedMountConfiguration(key, provider, "database");

// For direct JSON file loading - use EncryptedJsonFileExtensions
config.AddEncryptedJsonFile("secrets.json", key, false, true);
```

**Migration:**
- No code changes required! Extension methods work the same way.
- If you were explicitly qualifying with the class name, remove the class prefix.
- Both new classes are in the same namespace (`Microsoft.Extensions.DependencyInjection`).

**Why this change?**
- Resolved ambiguous method call errors (CS0121)
- Followed Single Responsibility Principle
- Cleaner separation of concerns:
  - `EncryptedMountConfigurationExtensions` - Mount path operations
  - `EncryptedJsonFileExtensions` - Low-level JSON file operations

#### 2. Built-in Encryption Deprecated

**Status:** ⚠️ DEPRECATED in v2.0, will be REMOVED in v3.0

Built-in DES encryption is deprecated. See [ADR-003](adr/ADR-003-encryption-delegation-to-external-tools.md) for rationale.

**Recommended alternatives:**
1. **Mozilla SOPS** - For GitOps and encrypted config files
2. **Kubernetes Secrets + Sealed Secrets** - For Kubernetes deployments
3. **Cloud Secret Managers** - Azure Key Vault, AWS Secrets Manager, GCP Secret Manager
4. **dotnet user-secrets** - For local development

**Migration tool available:**
```bash
# Install migration helper
dotnet tool install -g Voyager.Configuration.Tool --prerelease

# Decrypt existing encrypted config for migration to SOPS
vconfig decrypt --input config/secrets.json --output config/secrets.plain.json

# Now encrypt with SOPS
sops -e config/secrets.plain.json > config/secrets.json
```

See [ADR-003: Encryption Delegation](adr/ADR-003-encryption-delegation-to-external-tools.md) for detailed migration instructions.

### Improvements in v2.0

#### Custom Exception Types (ADR-006)

Better error messages with context:

**Before:**
```
FileNotFoundException: Could not find file 'C:\config\appsettings.json'
```

**After:**
```
ConfigurationException: Failed to load configuration file 'appsettings.json'.
File not found: C:\config\appsettings.json
  Mount path: C:\config
  FileName: appsettings.json
```

**Benefits:**
- Clearer error messages with configuration context
- Easier debugging in production
- Specific exception types: `ConfigurationException`, `EncryptionException`

#### Extension Methods Split (SRP)

Extension methods reorganized following Single Responsibility Principle:

**Files:**
- `ConfigurationExtension.cs` - Non-encrypted mount configuration
- `EncryptedMountConfigurationExtensions.cs` - Encrypted mount configuration
- `EncryptedJsonFileExtensions.cs` - Low-level encrypted JSON file operations

**No code changes required** - all methods work the same way as extension methods.

### Version Compatibility

| Version | .NET Framework 4.8 | .NET Core 3.1 | .NET 6.0 | .NET 8.0 |
|---------|-------------------|---------------|----------|----------|
| v1.x    | ✅                | ✅            | ✅       | ✅       |
| v2.x    | ✅                | ✅            | ✅       | ✅       |
| v3.x    | ✅ (planned)      | ✅ (planned)  | ✅       | ✅       |

### Roadmap

- **v2.0** (Current) - Deprecation warnings, improved architecture, custom exceptions
- **v3.0** (Planned) - Complete removal of built-in encryption

### Need Help?

- 📖 Check [ROADMAP.md](ROADMAP.md) for planned features
- 📝 Review [Architecture Decision Records](adr/) for design rationale
- 🐛 Open an issue on [GitHub](https://github.com/Voyager-Poland/Voyager.Configuration.MountPath/issues)

## Migrating from Older Versions

### From v1.0 to v1.x

No breaking changes. All v1.0 code is compatible with v1.x releases.

---

*Last updated: 2026-02-08*
