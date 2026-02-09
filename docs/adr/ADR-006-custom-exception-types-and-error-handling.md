# ADR-006: Custom Exception Types and Error Handling

## Status

**Accepted**

**Date**: 2026-02-07

## Context

### Current Error Handling

The library currently relies on framework exceptions for error handling:

```csharp
// Framework exceptions bubble up naturally
var json = File.ReadAllText(path); // Throws FileNotFoundException
var config = JsonSerializer.Deserialize<Dictionary<string, string>>(json); // Throws JsonException
var decrypted = encryptor.Decrypt(value); // Throws CryptographicException
```

**Current behavior:**
- Framework exceptions (FileNotFoundException, JsonException, CryptographicException) propagate to caller
- Error messages are generic and may lack context about configuration loading
- No way to distinguish configuration errors from other application errors
- Stack traces point to low-level operations, not configuration context

### The Question

Should we introduce custom exception types and enhanced error handling?

**Proposed improvements:**
1. Custom exception hierarchy (e.g., `ConfigurationException`, `EncryptionException`)
2. Validation in all public methods with clear error messages
3. Exception wrapping with contextual information (which file, which setting, why it failed)

### Example Scenarios

#### Scenario 1: Missing Configuration File

**Current behavior:**
```csharp
config.AddMountConfiguration(provider, "appsettings");
// Throws: System.IO.FileNotFoundException: Could not find file 'C:\config\appsettings.json'
```

**Proposed behavior:**
```csharp
config.AddMountConfiguration(provider, "appsettings");
// Throws: Voyager.Configuration.ConfigurationException:
//   Failed to load configuration file 'appsettings.json' from mount path 'C:\config'.
//   File not found: C:\config\appsettings.json
//   Inner exception: System.IO.FileNotFoundException
```

#### Scenario 2: Invalid JSON

**Current behavior:**
```csharp
config.AddMountConfiguration(provider, "database");
// Throws: System.Text.Json.JsonException:
//   The JSON value could not be converted to System.Collections.Generic.Dictionary...
```

**Proposed behavior:**
```csharp
config.AddMountConfiguration(provider, "database");
// Throws: Voyager.Configuration.ConfigurationException:
//   Failed to parse configuration file 'database.json' from mount path 'C:\config'.
//   The file contains invalid JSON. Check file syntax.
//   Inner exception: System.Text.Json.JsonException
```

#### Scenario 3: Decryption Failure

**Current behavior:**
```csharp
config.AddEncryptedMountConfiguration(encryptionKey, provider, "secrets");
// Throws: System.Security.Cryptography.CryptographicException:
//   The input is not a valid Base-64 string...
```

**Proposed behavior:**
```csharp
config.AddEncryptedMountConfiguration(encryptionKey, provider, "secrets");
// Throws: Voyager.Configuration.EncryptionException:
//   Failed to decrypt configuration value in 'secrets.json'.
//   Key: 'Database:Password'
//   Ensure the encryption key is correct and the value was encrypted with compatible settings.
//   Inner exception: System.Security.Cryptography.CryptographicException
```

#### Scenario 4: Invalid Settings

**Current behavior:**
```csharp
var settings = new Settings { MountPath = "" }; // Empty string accepted
// Later causes FileNotFoundException with unclear context
```

**Proposed behavior:**
```csharp
var settings = new Settings { MountPath = "" };
// Throws immediately: System.ArgumentException:
//   MountPath cannot be null or empty. (Parameter 'value')
```

## Decision

**We will implement custom exception types with focused scope.**

**What we will do:**
1. ✅ Create `ConfigurationException` for configuration loading errors
2. ✅ Create `EncryptionException` for encryption/decryption errors (inherits from ConfigurationException)
3. ✅ Wrap low-level exceptions with contextual information
4. ✅ Add validation in `Settings` class properties
5. ✅ Improve error messages to include: filename, mount path, operation context

**What we will NOT do:**
1. ❌ Exhaustive validation in every method (over-engineering)
2. ❌ Complex exception hierarchy (keep it simple: 2 types)
3. ❌ Exception types for every possible error (use framework exceptions where appropriate)
4. ❌ Custom exceptions for programmer errors (use framework ArgumentException, ArgumentNullException)

**Rationale:** Custom exceptions improve debuggability and user experience at startup without adding excessive complexity.

## Detailed Analysis

### Arguments FOR Custom Exceptions

#### 1. ✅ Domain-Specific Error Handling

Users can catch configuration-specific errors:

```csharp
try
{
    var app = builder.Build(); // Loads configuration
}
catch (ConfigurationException ex)
{
    // Handle configuration errors specifically
    logger.LogCritical(ex, "Failed to load configuration. Check mount paths and file formats.");
    Environment.Exit(1);
}
catch (Exception ex)
{
    // Other startup errors
    logger.LogCritical(ex, "Application failed to start.");
    Environment.Exit(1);
}
```

Without custom exceptions, users must catch generic exceptions:
```csharp
catch (Exception ex) // Too broad!
{
    // Could be FileNotFoundException, JsonException, or any other exception
}
```

#### 2. ✅ Better Error Messages with Context

Custom exceptions can include configuration-specific context:

```
ConfigurationException: Failed to load configuration file 'appsettings.json' from mount path '/config'.
  Mount path: /config
  Requested file: appsettings.json
  Full path: /config/appsettings.json
  Error: File not found
```

vs. framework exception:
```
FileNotFoundException: Could not find file '/config/appsettings.json'
```

The custom exception immediately tells the user:
- What operation failed (loading configuration)
- Which configuration file (appsettings.json)
- Where it looked (mount path /config)
- Why it failed (file not found)

#### 3. ✅ Distinguishes Configuration Errors from Code Errors

Same exception type (FileNotFoundException) can mean different things:

```csharp
// Configuration error - user needs to fix deployment/mount
config.AddMountConfiguration(provider, "appsettings"); // FileNotFoundException

// Code error - developer needs to fix code
var data = File.ReadAllText("data.txt"); // FileNotFoundException
```

Custom exceptions make the distinction clear.

#### 4. ✅ Professional Library Standard

Well-regarded libraries provide custom exceptions:
- Entity Framework: `DbUpdateException`, `DbConcurrencyException`
- ASP.NET Core: `InvalidOperationException` with specific messages
- Newtonsoft.Json: `JsonException`, `JsonReaderException`

Users expect configuration libraries to provide clear, specific errors.

#### 5. ✅ Debugging in Production

When errors occur in production logs:

**With custom exceptions:**
```
ERROR [2026-02-07 10:30:15] ConfigurationException: Failed to load configuration file 'database.json' from mount path '/mnt/config'.
  Mount path: /mnt/config
  Requested file: database.json
  Error: Invalid JSON at line 15, column 8
```

**Without custom exceptions:**
```
ERROR [2026-02-07 10:30:15] JsonException: The JSON value could not be converted to System.Collections.Generic.Dictionary`2[System.String,System.String].
  Path: $ | LineNumber: 15 | BytePositionInLine: 8
```

The first error message immediately tells operations team what to fix.

### Arguments AGAINST Custom Exceptions

#### 1. ❌ Framework Exceptions Are Already Descriptive

.NET framework exceptions are well-designed:
- `FileNotFoundException` - clear what happened
- `JsonException` - indicates JSON parsing error
- `ArgumentNullException` - indicates null argument

Do we need to wrap these?

**Counter-argument:** Framework exceptions lack **configuration context**. They don't tell you:
- Which configuration file failed
- What mount path was used
- What the library was trying to do

#### 2. ❌ Startup Fails Anyway

Configuration errors occur at startup. The application can't proceed without configuration, so it fails immediately.

**Example:**
```csharp
var app = builder.Build(); // Fails here - app never starts
```

Since the app can't start, does it matter whether the exception is `FileNotFoundException` or `ConfigurationException`?

**Counter-argument:** It matters for:
1. **Debugging** - developers need to understand what went wrong
2. **Operations** - team needs to know what to fix (missing file vs. wrong permissions vs. invalid JSON)
3. **Monitoring** - alerting systems can categorize errors (configuration vs. code bugs)

#### 3. ❌ Added Complexity

Custom exceptions add code to maintain:

```csharp
// New exception classes
public class ConfigurationException : Exception { }
public class EncryptionException : ConfigurationException { }

// Exception wrapping in every method
try { /* ... */ }
catch (FileNotFoundException ex)
{
    throw new ConfigurationException($"Failed to load...", ex);
}
catch (JsonException ex)
{
    throw new ConfigurationException($"Failed to parse...", ex);
}
```

**More code means:**
- More tests needed
- More documentation
- More potential bugs

**Counter-argument:** The complexity is minimal (2 exception classes) and the benefit (better error messages) outweighs the cost. This is a one-time investment that improves user experience forever.

#### 4. ❌ Risk of Over-Engineering

Could lead to excessive exception types:

```csharp
// Over-engineered exception hierarchy (DON'T DO THIS)
ConfigurationException
├── ConfigurationFileNotFoundException
├── ConfigurationParseException
│   ├── JsonParseException
│   └── XmlParseException
├── ConfigurationValidationException
│   ├── MissingRequiredValueException
│   └── InvalidValueFormatException
└── EncryptionException
    ├── DecryptionFailedException
    ├── InvalidKeyException
    └── InvalidCipherTextException
```

This is excessive for a library that just loads JSON files.

**Counter-argument:** We'll keep it simple - just 2 exception types:
1. `ConfigurationException` - for configuration loading errors
2. `EncryptionException` - for encryption-specific errors

#### 5. ❌ Users Rarely Catch Specific Exceptions

At startup, users typically use generic catch:

```csharp
try
{
    var app = builder.Build();
    await app.RunAsync();
}
catch (Exception ex) // Generic catch - custom exceptions don't help
{
    logger.LogCritical(ex, "Failed to start");
    return 1;
}
```

If users don't catch `ConfigurationException` specifically, what's the benefit?

**Counter-argument:** Even with generic catch, the exception **message** and **type** appear in logs. Better messages help debugging even without specific catch blocks.

### Validation: Where to Apply

#### Public Method Validation

**What should be validated:**

✅ **Settings properties** (critical - affects all operations):
```csharp
public required string MountPath
{
    get => _mountPath;
    init
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("MountPath cannot be null or empty.", nameof(value));
        _mountPath = value;
    }
}
```

✅ **Extension method parameters** (null checks):
```csharp
public static IConfigurationBuilder AddMountConfiguration(
    this IConfigurationBuilder builder,
    ISettingsProvider settings,
    string fileName)
{
    ArgumentNullException.ThrowIfNull(builder);
    ArgumentNullException.ThrowIfNull(settings);
    ArgumentNullException.ThrowIfNull(fileName);

    // ... implementation
}
```

❌ **Don't over-validate** (redundant checks):
```csharp
// DON'T DO THIS - framework already validates
if (!File.Exists(path))
    throw new ConfigurationException("File not found");

// Framework FileStream will throw FileNotFoundException anyway
// Just wrap the framework exception with context
```

#### Validation Strategy

**Fail-fast principle:**
- Validate at API boundaries (public methods)
- Let framework exceptions handle file/JSON errors
- Wrap framework exceptions with context

**Don't duplicate framework validation:**
- Framework validates file existence → wrap FileNotFoundException
- Framework validates JSON syntax → wrap JsonException
- Framework validates crypto → wrap CryptographicException

## Implementation Plan

### 1. Exception Classes

```csharp
namespace Voyager.Configuration
{
    /// <summary>
    /// Exception thrown when configuration loading fails.
    /// </summary>
    public class ConfigurationException : Exception
    {
        public string? MountPath { get; }
        public string? FileName { get; }

        public ConfigurationException(string message)
            : base(message) { }

        public ConfigurationException(string message, Exception innerException)
            : base(message, innerException) { }

        public ConfigurationException(
            string message,
            string? mountPath,
            string? fileName,
            Exception? innerException = null)
            : base(message, innerException)
        {
            MountPath = mountPath;
            FileName = fileName;
        }
    }

    /// <summary>
    /// Exception thrown when encryption or decryption fails.
    /// </summary>
    public class EncryptionException : ConfigurationException
    {
        public string? Key { get; }

        public EncryptionException(string message)
            : base(message) { }

        public EncryptionException(string message, Exception innerException)
            : base(message, innerException) { }

        public EncryptionException(
            string message,
            string? mountPath,
            string? fileName,
            string? key,
            Exception? innerException = null)
            : base(message, mountPath, fileName, innerException)
        {
            Key = key;
        }
    }
}
```

### 2. Exception Wrapping in Providers

```csharp
public override void Load()
{
    try
    {
        base.Load(); // Load JSON file
    }
    catch (FileNotFoundException ex)
    {
        throw new ConfigurationException(
            $"Failed to load configuration file '{Path.GetFileName(Source.Path)}' from mount path '{_settings.MountPath}'. " +
            $"File not found: {Source.Path}",
            _settings.MountPath,
            Path.GetFileName(Source.Path),
            ex);
    }
    catch (JsonException ex)
    {
        throw new ConfigurationException(
            $"Failed to parse configuration file '{Path.GetFileName(Source.Path)}'. " +
            $"The file contains invalid JSON. Check file syntax at line {ex.LineNumber}.",
            _settings.MountPath,
            Path.GetFileName(Source.Path),
            ex);
    }
    catch (Exception ex) when (ex is not ConfigurationException)
    {
        // Wrap any other unexpected exceptions
        throw new ConfigurationException(
            $"Unexpected error loading configuration file '{Path.GetFileName(Source.Path)}'.",
            _settings.MountPath,
            Path.GetFileName(Source.Path),
            ex);
    }

    // Decrypt values
    try
    {
        DecryptConfiguration();
    }
    catch (CryptographicException ex)
    {
        throw new EncryptionException(
            $"Failed to decrypt configuration values in '{Path.GetFileName(Source.Path)}'. " +
            $"Ensure the encryption key is correct and values were encrypted with compatible settings.",
            _settings.MountPath,
            Path.GetFileName(Source.Path),
            null, // No specific key - don't expose which keys failed
            ex);
    }
}
```

### 3. Validation in Settings

```csharp
public record Settings : ICloneable
{
    private string _mountPath = SettingsDefaults.MountPath;

    public required string MountPath
    {
        get => _mountPath;
        init
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException(
                    "MountPath cannot be null or empty.",
                    nameof(value));
            _mountPath = value;
        }
    }

    // Pattern property - no validation needed (any string pattern is valid)
    public string FileNamePattern { get; init; } = SettingsDefaults.FileNamePattern;

    // SpecificFileName - validate if provided
    private string? _specificFileName;
    public string? SpecificFileName
    {
        get => _specificFileName;
        init
        {
            if (value != null && string.IsNullOrWhiteSpace(value))
                throw new ArgumentException(
                    "SpecificFileName cannot be empty or whitespace.",
                    nameof(value));
            _specificFileName = value;
        }
    }
}
```

### 4. Validation in Extension Methods

```csharp
public static IConfigurationBuilder AddMountConfiguration(
    this IConfigurationBuilder builder,
    ISettingsProvider settings,
    string fileName)
{
    // Null checks with framework exceptions
    ArgumentNullException.ThrowIfNull(builder);
    ArgumentNullException.ThrowIfNull(settings);
    ArgumentNullException.ThrowIfNull(fileName);

    // Additional validation
    if (string.IsNullOrWhiteSpace(fileName))
        throw new ArgumentException(
            "fileName cannot be empty or whitespace.",
            nameof(fileName));

    // Implementation...
}
```

## Alternatives Considered

### Alternative 1: Use Framework Exceptions Only

**Approach:** Don't create custom exceptions, rely entirely on framework.

**Pros:**
- Zero additional code
- Users familiar with framework exceptions

**Cons:**
- Poor error messages lacking context
- Can't distinguish configuration errors from code errors
- Difficult debugging in production

**Assessment:** ❌ Rejected - error messages would be too generic

### Alternative 2: Rich Exception Hierarchy

**Approach:** Create specific exception for every error type.

```csharp
ConfigurationException
├── ConfigurationFileNotFoundException
├── ConfigurationParseException
├── ConfigurationValidationException
└── EncryptionException
    ├── DecryptionFailedException
    └── InvalidEncryptionKeyException
```

**Pros:**
- Very specific catch blocks possible
- Extremely detailed error categorization

**Cons:**
- Over-engineering for a simple library
- More maintenance burden
- Users overwhelmed with exception types

**Assessment:** ❌ Rejected - too complex for our needs

### Alternative 3: Result<T> Pattern with Voyager.Common.Results

**Context:** Voyager already has a Result pattern library: [Voyager.Common.Results](https://github.com/Voyager-Poland/Voyager.Common.Results)

This library provides `Result<T>` types for explicit error handling without exceptions.

**Approach:** Use Result objects instead of throwing exceptions.

```csharp
// Theoretical usage with Voyager.Common.Results
public Result<IConfigurationRoot> BuildConfiguration()
{
    if (!File.Exists(path))
        return Result.Failure<IConfigurationRoot>("File not found");

    try
    {
        var config = LoadConfiguration();
        return Result.Success(config);
    }
    catch (JsonException ex)
    {
        return Result.Failure<IConfigurationRoot>($"Invalid JSON: {ex.Message}");
    }
}

// Usage
var result = BuildConfiguration();
if (result.IsFailure)
{
    logger.LogError(result.Error);
    return;
}
var config = result.Value;
```

**Pros:**
- Explicit error handling - forces caller to handle errors
- No exceptions - errors are values
- Type-safe error handling
- Already available in Voyager ecosystem

**Cons:**
- **Incompatible with ASP.NET Core configuration system** - `IConfigurationProvider.Load()` is `void`, expects exceptions
- **Breaking change** - would require completely different API from ASP.NET Core conventions
- **Infrastructure-level concern** - configuration loading is infrastructure, Result pattern is better for domain/application layer
- **Fail-fast is appropriate** - application can't start without configuration, exceptions are correct
- **Framework integration** - ASP.NET Core startup expects exceptions for configuration errors

**Why Result pattern is wrong here:**

1. **Layer mismatch** - Result pattern is excellent for:
   - Domain logic (business rules validation)
   - Application services (use case orchestration)
   - API responses (returning errors to clients)

   But **not** for infrastructure concerns like configuration loading.

2. **Framework contract** - ASP.NET Core's `IConfigurationProvider` interface:
   ```csharp
   public interface IConfigurationProvider
   {
       void Load(); // void - cannot return Result<T>
   }
   ```

   We cannot change this interface. The framework expects exceptions.

3. **Startup semantics** - Configuration loads at application startup:
   ```csharp
   var app = builder.Build(); // If config fails, app should throw, not return Result
   ```

   Startup failures should be exceptional (exceptions), not expected outcomes (Results).

**Assessment:** ❌ Rejected - Result pattern is great for domain/application layers, but incompatible with infrastructure-level ASP.NET Core configuration system. The framework contract requires exceptions, and fail-fast behavior is appropriate at startup.

### Alternative 4: Two Exception Types (CHOSEN)

**Approach:** Simple hierarchy with just 2 types:
1. `ConfigurationException` - general configuration errors
2. `EncryptionException` - encryption-specific errors

**Pros:**
- Simple and maintainable
- Clear error context
- Allows specific catch blocks when needed
- Professional error messages

**Cons:**
- Small amount of additional code

**Assessment:** ✅ **ACCEPTED** - optimal balance of simplicity and functionality

## Consequences

### Positive

1. **Better User Experience**
   - Clear, actionable error messages
   - Easy to understand what went wrong
   - Faster debugging

2. **Easier Operations**
   - Production logs immediately show configuration problems
   - Operations team knows what to fix
   - Monitoring can categorize errors

3. **Professional Library**
   - Follows .NET conventions for domain libraries
   - Users can catch configuration-specific errors
   - Better than generic framework exceptions

4. **Minimal Complexity**
   - Only 2 exception types
   - Simple implementation
   - Easy to maintain

5. **Backward Compatible**
   - Existing code still works (just gets better error messages)
   - No breaking changes to API

### Negative

1. **Additional Code**
   - 2 new exception classes (~50 lines)
   - Exception wrapping in providers (~30 lines)
   - More unit tests (~10 tests)

2. **Slight Performance Cost**
   - Exception wrapping adds minor overhead
   - Only affects error paths (not hot path)
   - Negligible impact (errors are rare at startup)

### Migration

**No migration needed** - this is a non-breaking change:
- Existing code continues to work
- Users get better error messages automatically
- Optional: users can add specific catch blocks for `ConfigurationException`

## Testing Strategy

### Unit Tests

```csharp
[Fact]
public void Load_WhenFileNotFound_ThrowsConfigurationException()
{
    // Arrange
    var provider = CreateProvider("/nonexistent/path");

    // Act & Assert
    var ex = Assert.Throws<ConfigurationException>(() => provider.Load());
    Assert.Contains("File not found", ex.Message);
    Assert.Contains("/nonexistent/path", ex.Message);
    Assert.NotNull(ex.InnerException);
    Assert.IsType<FileNotFoundException>(ex.InnerException);
}

[Fact]
public void Load_WhenInvalidJson_ThrowsConfigurationException()
{
    // Arrange
    var provider = CreateProviderWithInvalidJson();

    // Act & Assert
    var ex = Assert.Throws<ConfigurationException>(() => provider.Load());
    Assert.Contains("invalid JSON", ex.Message);
    Assert.IsType<JsonException>(ex.InnerException);
}

[Fact]
public void Load_WhenDecryptionFails_ThrowsEncryptionException()
{
    // Arrange
    var provider = CreateEncryptedProviderWithBadKey();

    // Act & Assert
    var ex = Assert.Throws<EncryptionException>(() => provider.Load());
    Assert.Contains("decrypt", ex.Message);
    Assert.Contains("encryption key", ex.Message);
    Assert.IsType<CryptographicException>(ex.InnerException);
}

[Fact]
public void Settings_WhenMountPathEmpty_ThrowsArgumentException()
{
    // Act & Assert
    var ex = Assert.Throws<ArgumentException>(() =>
        new Settings { MountPath = "" });
    Assert.Contains("MountPath", ex.Message);
}
```

## Examples

### Before: Generic Framework Exceptions

```csharp
// Old error message
System.IO.FileNotFoundException: Could not find file 'C:\config\appsettings.json'.
   at System.IO.FileStream.ValidateFileHandle(SafeFileHandle fileHandle)
   at System.IO.FileStream..ctor(String path, FileMode mode)
   at System.IO.File.ReadAllText(String path)
```

**User thinking:** "Where is this file supposed to be? What is trying to load it?"

### After: Custom ConfigurationException

```csharp
// New error message
Voyager.Configuration.ConfigurationException:
Failed to load configuration file 'appsettings.json' from mount path 'C:\config'.
File not found: C:\config\appsettings.json

Ensure the file exists and the mount path is correctly configured.
  Mount path: C:\config
  Requested file: appsettings.json
---> System.IO.FileNotFoundException: Could not find file 'C:\config\appsettings.json'
   at Voyager.Configuration.EncryptedJsonConfigurationProvider.Load()
```

**User thinking:** "Ah, the mount configuration is looking for appsettings.json in C:\config. I need to check my Kubernetes ConfigMap or Docker volume."

## Related Decisions

- **ADR-003**: Encryption deprecation - we still need good error messages for legacy encryption
- **ADR-005**: Async rejected - error handling remains synchronous, which simplifies exception handling

## Related Libraries

- **[Voyager.Common.Results](https://github.com/Voyager-Poland/Voyager.Common.Results)**: Voyager's Result pattern library for explicit error handling. Not used here because:
  - Result pattern is designed for domain/application layers, not infrastructure
  - ASP.NET Core configuration system expects exceptions (IConfigurationProvider.Load() is void)
  - Configuration loading at startup should fail fast with exceptions
  - See Alternative 3 for detailed analysis

## Future Considerations

### If Error Handling Needs Expansion

If future requirements emerge:

1. **Add more properties to exceptions** (e.g., `LineNumber`, `JsonPath`)
2. **Add factory methods** for common error scenarios
3. **Structured logging integration** (add IDs for monitoring)

But wait for real requirements - don't over-engineer.

## Conclusion

**Custom exception types improve user experience with minimal cost.**

**Key principles:**
1. ✅ Create 2 exception types: `ConfigurationException` and `EncryptionException`
2. ✅ Wrap framework exceptions with configuration context
3. ✅ Validate inputs at API boundaries (Settings, extension methods)
4. ❌ Don't over-validate (let framework handle file/JSON validation)
5. ❌ Don't create excessive exception hierarchy (keep it simple)

**The result:** Users get clear, actionable error messages that speed up debugging and reduce operational friction, while we maintain a simple, maintainable codebase.

---

**Decision**: ✅ Implement custom exception types with focused scope

**Last reviewed**: 2026-02-07
