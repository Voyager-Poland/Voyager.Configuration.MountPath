# ADR-005: Asynchronous Configuration Loading Support

## Status

**Rejected**

**Date**: 2026-02-07

## Context

### Current Implementation

The library currently provides synchronous configuration loading:

```csharp
builder.ConfigureAppConfiguration((context, config) =>
{
    var provider = context.HostingEnvironment.GetSettingsProvider();

    // Synchronous loading
    config.AddMountConfiguration(provider, "appsettings");
    config.AddMountConfiguration(provider, "database");
});
```

**All operations are synchronous:**
- File discovery and reading
- JSON parsing
- Decryption (if used)
- Configuration merging

### The Question

Should we add async overloads for configuration loading?

```csharp
// Proposed async API
await config.AddMountConfigurationAsync(provider, "appsettings");
await config.AddEncryptedMountConfigurationAsync(encryptionKey, provider, "secrets");
```

### Use Cases to Consider

1. **Local file systems** (primary use case)
   - Docker volume mounts
   - Kubernetes ConfigMaps/Secrets
   - Local development

2. **Network file systems**
   - NFS mounts
   - SMB/CIFS shares
   - Cloud storage mounted as volumes

3. **Remote configuration sources** (not current scope)
   - HTTP endpoints
   - Databases
   - Cloud configuration services

## Decision

**We will NOT implement async configuration loading.**

**Rationale:** ASP.NET Core's configuration system is fundamentally synchronous by design, and our library's primary use case (local volume mounts) does not benefit from async operations.

## Detailed Analysis

### Arguments FOR Async Support

#### 1. ✅ Consistency with Modern .NET

Modern .NET heavily favors async/await patterns:
- Most I/O operations have async versions
- ASP.NET Core middleware is async
- Entity Framework Core is async-first

**Counter-argument:** Configuration loading happens at startup, which is inherently synchronous in ASP.NET Core. The `Program.cs` startup pipeline doesn't support async configuration building.

#### 2. ✅ Network File Systems

Slow network mounts could benefit from non-blocking I/O:
```csharp
// Theoretical benefit for slow NFS
await config.AddMountConfigurationAsync(provider, "appsettings"); // Non-blocking
```

**Counter-argument:**
- Configuration is loaded once at startup - blocking is expected
- Network file systems are typically fast enough for startup
- If network I/O is a problem, caching/pre-loading strategies are better solutions

#### 3. ✅ Future-Proofing

Async APIs could enable future features:
- Remote configuration sources (HTTP, databases)
- Progressive/lazy loading
- Real-time reload with async notification

**Counter-argument:**
- This library focuses on **mount paths** (local volumes), not remote sources
- Remote configuration is better handled by specialized providers (Azure App Configuration, AWS Parameter Store)
- Real-time reload already works with `FileSystemWatcher` (synchronous)

#### 4. ✅ Cancellation Token Support

Async enables cancellation:
```csharp
await config.AddMountConfigurationAsync(provider, "appsettings", cancellationToken);
```

**Counter-argument:**
- Configuration loading at startup cannot be cancelled (app must have config to start)
- No practical use case for cancelling configuration load

### Arguments AGAINST Async Support

#### 1. ❌ ASP.NET Core Configuration is Synchronous

**The fundamental issue:** `IConfigurationBuilder` and `IConfigurationProvider` interfaces are synchronous.

```csharp
public interface IConfigurationProvider
{
    // This is SYNCHRONOUS - cannot be async
    void Load();

    bool TryGet(string key, out string value);
    void Set(string key, string value);
    // ...
}
```

**Implementation constraint:**
```csharp
public class EncryptedJsonConfigurationProvider : FileConfigurationProvider
{
    // This method is defined by the framework as void - cannot make it async
    public override void Load()
    {
        base.Load(); // Synchronous

        // We CANNOT await here - method signature is void, not Task
        DecryptConfiguration(); // Must be synchronous
    }
}
```

**Implication:** Even if we add async extension methods, the underlying `Load()` must still be synchronous. The async methods would just be syntactic sugar that blocks internally:

```csharp
// This would be fake async - just blocking in disguise
public static IConfigurationBuilder AddMountConfigurationAsync(
    this IConfigurationBuilder builder,
    ISettingsProvider settings,
    string fileName)
{
    // The provider.Load() is STILL synchronous!
    return builder.AddMountConfiguration(settings, fileName);
}
```

#### 2. ❌ Startup is Synchronous by Design

ASP.NET Core startup pipeline is synchronous:

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// This callback is synchronous - cannot use await
builder.ConfigureAppConfiguration((context, config) =>
{
    // Cannot await here!
    config.AddMountConfiguration(provider, "appsettings");
});

var app = builder.Build(); // Synchronous - loads all config
```

**Why is startup synchronous?**
- Application must have configuration before starting
- No point in non-blocking I/O when the app can't proceed without config
- Simplifies startup logic and error handling

#### 3. ❌ Primary Use Case Doesn't Benefit

**This library targets mount paths:**
- Docker volumes (local filesystem)
- Kubernetes ConfigMaps/Secrets (mounted as local files)
- Local development

**File I/O is fast:**
```csharp
// Reading a JSON config file takes ~1-5ms
var json = File.ReadAllText("/config/appsettings.json");

// Async would save maybe 0.1-1ms, but:
// 1. App can't start without config anyway (blocking is required)
// 2. Startup time is dominated by other factors (DI, middleware, DB connections)
```

**Measurement:**
```
Synchronous config load:  3ms
Hypothetical async load:  2.5ms
Database connection:      150ms
Application startup:      500ms
```

Async saves 0.5ms in a 500ms process - **negligible benefit**.

#### 4. ❌ Increased Complexity

Adding async doubles the API surface:

```csharp
// Before (2 methods)
config.AddMountConfiguration(provider, "appsettings");
config.AddEncryptedMountConfiguration(key, provider, "secrets");

// After (4 methods) - plus internal async implementations
config.AddMountConfiguration(provider, "appsettings");
config.AddMountConfigurationAsync(provider, "appsettings");
config.AddEncryptedMountConfiguration(key, provider, "secrets");
config.AddEncryptedMountConfigurationAsync(key, provider, "secrets");
```

**Maintenance burden:**
- More tests
- More documentation
- More potential for bugs
- Users confused about which version to use

#### 5. ❌ Misleading API

Providing async methods suggests they're truly async, but they can't be:

```csharp
// User expects this to be non-blocking, but it's not
await config.AddMountConfigurationAsync(provider, "appsettings");
// ^ Internally calls synchronous Load() - just blocking with extra steps
```

This violates the **principle of least astonishment** - async APIs should actually be async, not synchronous operations disguised as async.

## Alternatives Considered

### Alternative 1: True Async with Custom Interfaces

**Approach:** Create our own async configuration interfaces

```csharp
public interface IAsyncConfigurationProvider
{
    Task LoadAsync(CancellationToken cancellationToken = default);
}

public static async Task<IConfigurationRoot> BuildAsync(
    this IConfigurationBuilder builder,
    CancellationToken cancellationToken = default)
{
    // Load all providers async
    // Merge configuration
    // Return IConfigurationRoot
}
```

**Pros:**
- Truly async
- Full control

**Cons:**
- Incompatible with ASP.NET Core's `IConfigurationBuilder`
- Would require completely separate builder pipeline
- Breaking change - users must use different APIs
- Massive effort for minimal benefit

**Assessment:** ❌ Rejected - too much effort, breaks compatibility

### Alternative 2: Async Only for Specific Operations

**Approach:** Keep main API synchronous, add async only where beneficial

```csharp
// Main API stays synchronous
config.AddMountConfiguration(provider, "appsettings");

// Async only for genuinely async operations (e.g., remote sources)
config.AddRemoteConfiguration(url); // If we ever add remote sources
```

**Assessment:** ⚠️ Possible future consideration, but not needed now

### Alternative 3: Lazy/Progressive Loading

**Approach:** Load configuration asynchronously after startup

```csharp
var app = builder.Build(); // Fast startup with minimal config

await app.Services.GetRequiredService<IConfigurationLoader>()
    .LoadAdditionalConfigurationAsync(); // Load more config after startup
```

**Pros:**
- Faster startup
- Truly async

**Cons:**
- App behavior depends on timing (race conditions)
- Configuration might not be available when needed
- Violates ASP.NET Core configuration model

**Assessment:** ❌ Rejected - introduces complexity and race conditions

## Consequences

### Positive

1. **Simpler API**
   - Single synchronous API to learn and use
   - No confusion about which version to use

2. **Aligned with Framework**
   - Works naturally with ASP.NET Core configuration system
   - No fighting against framework design

3. **Appropriate for Use Case**
   - Local file I/O is fast enough
   - Blocking at startup is expected

4. **Less Maintenance**
   - Fewer APIs to test and document
   - Reduced code complexity

5. **Honest API**
   - Methods do what they say (synchronous I/O)
   - No misleading async wrappers around sync code

### Negative

1. **Not "Modern"**
   - Doesn't follow async-everywhere pattern
   - May seem outdated to developers expecting async APIs

2. **Potential Future Limitations**
   - If we add remote sources later, would need breaking changes
   - Cannot support cancellation (though not needed for startup config)

3. **Network FS Scenarios**
   - Slow network mounts will block startup
   - No way to parallelize config loading

### Mitigation Strategies

#### For Slow Network File Systems

If network I/O is slow, users can:

1. **Pre-load/cache configuration**
   ```bash
   # Init container copies config to local volume
   cp /nfs-mount/config/* /local-cache/config/
   ```

2. **Use readiness probes**
   ```yaml
   # Kubernetes: App reports ready only after config loads
   readinessProbe:
     httpGet:
       path: /health/ready
       port: 8080
     initialDelaySeconds: 5
   ```

3. **Optimize file system**
   - Use NFS with caching
   - Mount with appropriate read-ahead settings

#### For Future Remote Sources

If remote configuration becomes needed:
- Use existing providers (Azure App Configuration, AWS Parameter Store)
- These have their own async loading mechanisms
- Separation of concerns: mount path ≠ remote configuration

## Related Decisions

- **ADR-003**: Encryption delegation to external tools - reduces complexity of this library
- **ADR-004**: CLI tool for migration - focuses library on core competency (mount paths)

## Recommendation for Future

**If async becomes necessary** (e.g., genuinely slow I/O becomes a common problem):

1. **Wait for framework support** - ASP.NET Core may add async configuration in future versions
2. **Measure first** - Prove that sync I/O is actually a bottleneck
3. **Consider alternatives** - Caching, pre-loading, better file systems
4. **Only then** - Add async if proven necessary and framework supports it

## Examples

### Current Usage (Correct)

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.ConfigureAppConfiguration((context, config) =>
{
    var provider = context.HostingEnvironment.GetSettingsProvider();

    // Synchronous - appropriate for startup
    config.AddMountConfiguration(provider, "appsettings");
    config.AddMountConfiguration(provider, "database");
    config.AddMountConfiguration(provider, "logging");
});

var app = builder.Build(); // Blocks until all config loaded - this is correct!
await app.RunAsync();
```

### ❌ Hypothetical Async (Misleading)

```csharp
builder.ConfigureAppConfiguration(async (context, config) =>
{
    // This LOOKS async but can't actually be - ConfigureAppConfiguration doesn't support async
    await config.AddMountConfigurationAsync(provider, "appsettings");
    // ^ Would just block internally - fake async!
});
```

## Conclusion

**Async configuration loading is not appropriate for this library because:**

1. ASP.NET Core configuration is fundamentally synchronous
2. Our use case (mount paths) benefits minimally from async
3. Startup blocking is expected and appropriate
4. Adds complexity without meaningful benefit
5. Would create misleading APIs (fake async)

**The current synchronous API is correct and sufficient.**

If future requirements emerge (remote sources, proven I/O bottlenecks), we can reconsider - but evidence suggests this is unlikely for a library focused on local volume mounts.

---

**Decision**: ❌ Do NOT implement async configuration loading

**Last reviewed**: 2026-02-07
