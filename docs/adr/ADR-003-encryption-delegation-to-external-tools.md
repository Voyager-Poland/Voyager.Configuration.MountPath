# ADR-003: Encryption Delegation to External Tools

## Status

Proposed

**Date**: 2026-02-06

## Context

The library provides optional encryption functionality for configuration values through custom `Encryptor` and `LegacyDesCipherProvider` classes. This feature allows users to store encrypted values in JSON configuration files and have them automatically decrypted when loaded.

### Current Implementation

- Custom `Encryptor` class implementing `IEncryptor` interface
- Legacy DES encryption (56-bit, deprecated and insecure)
- Planned migration to AES-256-GCM (see ROADMAP.md 1.1)
- Encryption is optional - separate extension methods (`AddEncryptedMountConfiguration`)
- Designed for containerized environments (Docker, Kubernetes)

### The Question

**Should the library maintain its own encryption implementation, or should it delegate secret management to external tools and platforms?**

This decision impacts:
1. **Security posture** - maintaining cryptographic code is risky
2. **Scope and complexity** - is encryption within the library's purpose?
3. **Maintenance burden** - crypto code requires ongoing security updates
4. **User experience** - self-contained vs. external dependencies
5. **Cloud-native alignment** - modern secret management patterns

## Decision

**Recommend deprecating built-in encryption in favor of external secret management tools.**

However, maintain backward compatibility for existing users with clear migration guidance.

### Rationale

1. **Separation of Concerns**
   - Core purpose: Conditional configuration file loading by environment
   - Encryption is orthogonal to configuration loading
   - "Do one thing well" principle

2. **Security Best Practices**
   - Cryptography is hard - leave it to specialists
   - External tools have dedicated security teams
   - Regular security audits and CVE monitoring
   - Key rotation and access control

3. **Cloud-Native Ecosystem Maturity**
   - Kubernetes Secrets
   - HashiCorp Vault
   - Azure Key Vault / AWS Secrets Manager / GCP Secret Manager
   - Mozilla SOPS (encrypt files in git)
   - Age encryption tool
   - Sealed Secrets (Bitnami)

4. **Reduced Maintenance Burden**
   - No need to maintain crypto code
   - No need to migrate DES → AES
   - No security vulnerabilities in library code
   - Focus on core functionality

5. **Better Integration**
   - External tools integrate with CI/CD
   - Support key rotation without code changes
   - Audit logging and access control
   - Multi-environment key management

## Alternatives Considered

### Alternative 1: Keep Custom Encryption (Current Approach)

**Migrate from DES to AES-256-GCM as planned in ROADMAP.md**

**Pros:**
- Self-contained - no external dependencies
- Simple for users - just provide a key
- Already implemented and working
- Familiar to existing users

**Cons:**
- Security risk - maintaining crypto code is dangerous
- Not the library's core purpose - scope creep
- Requires ongoing security maintenance
- DES is already deprecated (SYSLIB0021 warning)
- Migration complexity (DES → AES compatibility)
- No key rotation mechanism
- No access control or audit logging

**Example:**
```csharp
config.AddEncryptedMountConfiguration(settings =>
{
    settings.FileName = "database";
    settings.Key = Environment.GetEnvironmentVariable("ENCRYPTION_KEY");
});
```

### Alternative 2: ASP.NET Core Data Protection API

**Delegate to Microsoft's built-in encryption**

**Pros:**
- Built into .NET - no external tools
- Microsoft-maintained and audited
- Key rotation support
- Machine/user key stores

**Cons:**
- Requires Data Protection infrastructure setup
- Keys stored on host filesystem (not ideal for containers)
- Still requires library to handle encryption logic
- Doesn't solve "not our core purpose" problem

**Example:**
```csharp
services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/mnt/keys"));

config.AddProtectedMountConfiguration(settings =>
{
    settings.FileName = "database";
});
```

**Assessment:** Better than custom crypto, but still adds complexity.

### Alternative 3: External CLI Tools (Recommended)

**Delegate to specialized secret management tools**

#### Option 3A: Mozilla SOPS

**Best for: Encrypted files in Git**

```bash
# Encrypt entire file
sops -e config/database.json > config/database.enc.json

# Decrypt at runtime (Kubernetes)
kubectl create secret generic config \
  --from-file=database.json=<(sops -d config/database.enc.json)
```

**Pros:**
- Encrypts entire file or specific keys
- Integrates with KMS providers (AWS, Azure, GCP, age)
- Built for git-committed configs
- Decryption at container start

**Cons:**
- Requires sops in container or init container
- Learning curve for setup

#### Option 3B: Age Encryption

**Best for: Simple file encryption**

```bash
# Encrypt file with public key
age -r age1abc... -o database.json.age database.json

# Decrypt at runtime
age -d -i /keys/identity database.json.age > /config/database.json
```

**Pros:**
- Simple, modern encryption tool
- No complex infrastructure
- Small binary

**Cons:**
- Manual key management
- Need to decrypt before loading

#### Option 3C: Kubernetes Secrets + Sealed Secrets

**Best for: Kubernetes-native workflows**

```yaml
# SealedSecret (encrypted, safe for git)
apiVersion: bitnami.com/v1alpha1
kind: SealedSecret
metadata:
  name: database-config
spec:
  encryptedData:
    database.json: AgBc7H8...encrypted...

# Auto-decrypted to Secret by controller
# Mounted as file to /config/database.json
```

**Pros:**
- Native Kubernetes integration
- GitOps-friendly (encrypted in git)
- Automatic decryption by controller
- RBAC and audit logging

**Cons:**
- Kubernetes-only
- Requires Sealed Secrets controller

### Alternative 4: Cloud Secret Managers

**Delegate to cloud provider secret storage**

#### Azure Key Vault / AWS Secrets Manager / GCP Secret Manager

```csharp
// Use Azure Key Vault provider
config.AddAzureKeyVault(new Uri("https://vault.azure.net"), new DefaultAzureCredential());

// Secrets automatically loaded as configuration
var dbPassword = config["database:password"];
```

**Pros:**
- Enterprise-grade security
- Key rotation and access control
- Audit logging
- Managed service

**Cons:**
- Cloud provider lock-in
- Cost for secret storage
- Network dependency
- Complexity

**Assessment:** Best for enterprise, but overkill for simple scenarios.

### Alternative 5: Hybrid Approach

**Support multiple strategies via abstraction**

```csharp
// Built-in encryption (deprecated, backward compatible)
config.AddEncryptedMountConfiguration(settings => { ... });

// External tool integration (recommended)
config.AddMountConfiguration(settings => { ... }); // Load already-decrypted files

// Document external tool usage in README
```

**Assessment:** Maintain compatibility while recommending external tools.

## Recommended Approach

### Phase 1: Immediate (v2.0)

1. **Deprecate encryption features**
   - Mark `AddEncryptedMountConfiguration` as `[Obsolete]`
   - Mark `Encryptor` and encryption classes as `[Obsolete]`
   - Add XML doc warnings about security risks

2. **Update documentation**
   - Add "Secret Management" section to README
   - Document external tool alternatives:
     - Kubernetes Secrets (basic)
     - Sealed Secrets (GitOps)
     - Mozilla SOPS (flexible)
     - Age (simple)
     - Cloud secret managers (enterprise)
   - Provide migration examples

3. **Keep backward compatibility**
   - Existing encryption code continues to work
   - Security warnings in logs when used
   - No new encryption features added

### Phase 2: Future (v3.0, breaking)

1. **Remove encryption features entirely**
   - Delete `Encryption/` directory
   - Remove encryption extension methods
   - Update package description

2. **Focus on core purpose**
   - Configuration loading from mount paths
   - Conditional environment-based loading
   - Simple, predictable behavior

## Migration Strategy for Users

### For Development Environments

**Use `dotnet user-secrets`:**

```bash
# Store secrets outside of git
dotnet user-secrets set "database:password" "DevPassword123"

# Access in code (standard ASP.NET Core)
var dbPassword = config["database:password"];
```

### For Production Kubernetes

**Use Kubernetes Secrets + Sealed Secrets:**

```bash
# Create secret (encrypted in git as SealedSecret)
kubectl create secret generic database-config \
  --from-file=config/database.json --dry-run=client -o yaml \
  | kubeseal -o yaml > sealed-database-config.yaml

# Mount in pod
volumes:
  - name: config
    secret:
      secretName: database-config
```

### For File-Based Secrets (SOPS)

**Encrypt files before commit:**

```bash
# One-time setup
sops --encrypt --age age1abc... config/database.json > config/database.enc.json

# In Dockerfile/entrypoint
sops --decrypt /config-encrypted/database.json > /config/database.json

# Load normally
config.AddMountConfiguration(s => s.FileName = "database");
```

### For Current Users (Backward Compatibility)

**Continue using existing code until v3.0:**

```csharp
// Still works, but logs deprecation warnings
config.AddEncryptedMountConfiguration(settings =>
{
    settings.FileName = "database";
    settings.Key = Environment.GetEnvironmentVariable("ENCRYPTION_KEY");
});
```

## Consequences

### Positive

1. **Better Security**
   - No custom crypto code to audit
   - Leverage specialized tools
   - Reduced attack surface

2. **Clearer Purpose**
   - Library focuses on configuration loading
   - Users choose secret management strategy
   - Separation of concerns

3. **Lower Maintenance**
   - No DES → AES migration needed
   - No crypto CVE monitoring
   - Simpler codebase

4. **Better Ecosystem Integration**
   - Works with GitOps workflows
   - Cloud-native secret management
   - Enterprise compliance (audit logs, key rotation)

5. **More Flexibility**
   - Users choose tool that fits their needs
   - Not locked into library's encryption choice
   - Can switch tools without code changes

### Negative

1. **Breaking Change (v3.0)**
   - Existing users must migrate
   - May cause upgrade friction
   - Need good documentation

2. **Increased User Responsibility**
   - Users must set up secret management
   - More moving parts
   - Learning curve for external tools

3. **No "Batteries Included" Encryption**
   - Not self-contained anymore
   - Requires external tool setup
   - More complex for simple scenarios

4. **Migration Effort**
   - Users with encrypted configs must decrypt
   - Re-encrypt with chosen tool
   - Update deployment scripts

### Mitigation Strategies

1. **Comprehensive Documentation**
   - Step-by-step migration guides
   - Examples for common scenarios
   - Comparison of external tools

2. **Gradual Deprecation**
   - v2.0: Mark as obsolete, keep working
   - v2.x: Add warnings to logs
   - v3.0: Remove (major version bump)

3. **Migration Helper Tool**
   - CLI tool to decrypt existing configs
   - Convert to plain JSON for re-encryption
   - Automate migration where possible

4. **Reference Implementation**
   - Sample projects showing external tool integration
   - Docker/Kubernetes examples
   - CI/CD pipeline examples

## Implementation Plan

### Version 2.0 (Non-Breaking)

1. Mark all encryption-related APIs as `[Obsolete]`
   ```csharp
   [Obsolete("Built-in encryption is deprecated. Use external secret management tools like SOPS, Sealed Secrets, or cloud secret managers. See documentation for migration guide.")]
   public static IConfigurationBuilder AddEncryptedMountConfiguration(...)
   ```

2. Add warning logs when encryption is used
   ```csharp
   _logger.LogWarning("Using deprecated built-in encryption. Migrate to external secret management tools.");
   ```

3. Update README with "Secret Management" section
   - Document why encryption is deprecated
   - Provide migration examples for:
     - Kubernetes Secrets
     - Sealed Secrets
     - Mozilla SOPS
     - Age
     - Cloud providers

4. Update ROADMAP.md
   - Remove task 1.1 (DES → AES migration)
   - Add task: "Document external secret management options"
   - Add task: "Deprecate encryption features"

5. Update package description
   - Remove emphasis on encryption
   - Focus on conditional config loading

### Version 3.0 (Breaking)

1. Remove encryption code
   - Delete `src/Voyager.Configuration.MountPath/Encryption/` directory
   - Remove `AddEncryptedMountConfiguration` extension methods
   - Remove encryption tests

2. Update documentation
   - Remove encryption from main README
   - Keep migration guide in separate doc

3. Update NuGet metadata
   - Major version bump (3.0.0)
   - Update release notes with breaking change
   - Link to migration guide

## References

### External Secret Management Tools

- [Mozilla SOPS](https://github.com/mozilla/sops) - Encrypted files in Git
- [Age](https://github.com/FiloSottile/age) - Modern encryption tool
- [Sealed Secrets](https://github.com/bitnami-labs/sealed-secrets) - Kubernetes GitOps
- [HashiCorp Vault](https://www.vaultproject.io/) - Enterprise secret management
- [Azure Key Vault](https://azure.microsoft.com/en-us/services/key-vault/)
- [AWS Secrets Manager](https://aws.amazon.com/secrets-manager/)
- [GCP Secret Manager](https://cloud.google.com/secret-manager)

### .NET Secret Management

- [ASP.NET Core Secret Management](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets)
- [ASP.NET Core Data Protection](https://docs.microsoft.com/en-us/aspnet/core/security/data-protection/)
- [Azure Key Vault Configuration Provider](https://docs.microsoft.com/en-us/aspnet/core/security/key-vault-configuration)

### Security Best Practices

- [OWASP Cryptographic Storage Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Cryptographic_Storage_Cheat_Sheet.html)
- [NIST Cryptographic Standards](https://csrc.nist.gov/projects/cryptographic-standards-and-guidelines)

## Related ADRs

- [ADR-001: Extension Methods Organization](ADR-001-extension-methods-organization.md) - How encryption extensions are structured
- [ADR-002: Settings Builder Pattern](ADR-002-settings-builder-pattern.md) - Configuration API design

### Supersedes

- ROADMAP.md Task 1.1 (DES → AES migration) - No longer needed if encryption is deprecated
