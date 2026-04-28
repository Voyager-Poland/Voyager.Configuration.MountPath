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

#### 2. Encryption Upgraded: DES → AES-256-GCM

**Status:** ✅ AES-256-GCM available in v2.3.0 — legacy DES reads still supported

Built-in encryption has been upgraded from legacy DES to AES-256-GCM (see [ADR-010](adr/ADR-010-aes-gcm-with-versioned-ciphertext.md)). This is **not a breaking change** — existing DES-encrypted files continue to work.

**What changed:**
- New writes use AES-256-GCM with versioned format (`v2:BASE64(nonce||ciphertext||tag)`)
- Existing DES values are readable via `AllowLegacyDes=true` (default in v2.x)
- A new 32-byte AES key is required (the old DES key cannot be reused)

**Migration from DES to AES:**
```bash
# Install CLI tool
dotnet tool install -g Voyager.Configuration.Tool --prerelease

# 1. Generate new AES key
export ASPNETCORE_AES_KEY=$(vconfig keygen)

# 2. Re-encrypt: old DES key decrypts, new AES key encrypts
vconfig reencrypt \
  --input config/secrets.json \
  --legacy-key-env ASPNETCORE_ENCODEKEY \
  --new-key-env ASPNETCORE_AES_KEY

# 3. Swap env vars in deployment (put AES key in ASPNETCORE_ENCODEKEY)
# 4. Remove old DES key from secret manager / vault
```

**Staged legacy removal:**
- **v2.x** (current): `AllowLegacyDes=true` — DES reads work, AES writes default
- **v3.x** (planned): `AllowLegacyDes=false` — DES disabled by default, opt-in available
- **v4.x** (planned): DES code removed entirely

**Extension point:** The `IEncryptor` / `IEncryptorFactory` interfaces allow custom encryption implementations if your organization has specific requirements (KMS, key rotation, audit logging).

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

- **v2.0** - SOLID architecture, custom exceptions, extension methods split
- **v2.3** (Current) - AES-256-GCM encryption (ADR-010), CLI keygen/reencrypt
- **v3.0** (Planned) - `AllowLegacyDes` defaults to `false`
- **v4.0** (Planned) - Legacy DES code removed entirely

### Need Help?

- 📖 Check [ROADMAP.md](ROADMAP.md) for planned features
- 📝 Review [Architecture Decision Records](adr/) for design rationale
- 🐛 Open an issue on [GitHub](https://github.com/Voyager-Poland/Voyager.Configuration.MountPath/issues)

## Migrating from Older Versions

### From v1.0 to v1.x

No breaking changes. All v1.0 code is compatible with v1.x releases.

---

*Last updated: 2026-04-20*
