# ADR-004: CLI Tool for Configuration File Encryption/Decryption

## Status

Proposed

**Date**: 2026-02-06

## Context

### Current Situation

The project has two simple console applications for encryption/decryption:

1. **`Voyager.Configuration.Encrypt`** - Encrypts a single text string
2. **`Voyager.Configuration.Decrypt`** - Decrypts a single text string

**Current implementation:**
```bash
# Decrypt a single value
Voyager.Configuration.Decrypt.exe "encrypted_base64_string"

# Encrypt a single value
Voyager.Configuration.Encrypt.exe "plaintext_value"
```

Both tools:
- Read encryption key from `ASPNETCORE_ENCODEKEY` environment variable
- Accept only a **single text string** as argument (not entire files)
- Are separate console applications (not packaged as dotnet tools)
- Have long, cumbersome names requiring full path

### The Problem

**Limitations of current tools:**

1. **Single value only** - Cannot encrypt/decrypt entire JSON configuration files
   - To encrypt a file, user must manually encrypt each value separately
   - Copy encrypted values back into JSON file by hand
   - Error-prone and time-consuming

2. **Not distributed as package** - Not available as dotnet global tool
   - Users must build from source
   - No versioning or easy updates
   - Not discoverable via NuGet

3. **Poor usability** - Long executable names and awkward workflow
   - `Voyager.Configuration.Decrypt.exe` is verbose
   - No proper CLI arguments (only positional)
   - No help text or options
   - No progress feedback for operations

4. **No migration support** - Given ADR-003 deprecates encryption
   - Users need to decrypt existing configs to migrate to SOPS
   - Current tools don't facilitate this migration
   - No batch processing for multiple files

### User Requirements

From project maintainer:
> "Potrzebuję tool do całościowego szyfrowania i odszyfrowania plików konfiguracyjnych.
> Obecne programy ograniczone są tylko do określonego jednego tekstu - co nie jest wygodne.
> Chciałbym mieć możliwość używania tego jako narzędzie po zainstalowaniu jako pakiet.
> Nazwa tych programów nie jest zaszczęsliwa bo wymaga podawania długiego ciągu."

**Translation:**
- Need tool for **whole file** encryption/decryption
- Current programs limited to **single text** - not convenient
- Want to install as **package** (dotnet tool)
- Current **names are not fortunate** (too long)

### Context from ADR-003

[ADR-003: Encryption Delegation to External Tools](ADR-003-encryption-delegation-to-external-tools.md) recommends:
- Deprecating built-in encryption (removed in v3.0)
- Migrating to SOPS, Kubernetes Secrets, or cloud secret managers

**This creates a need for:**
- Migration tooling to decrypt legacy DES-encrypted configs
- One-time batch decryption of existing files
- Converting encrypted configs to plain text for SOPS migration

## Decision

**Create a unified CLI tool as a dotnet global tool: `vconfig`**

This tool serves as a **migration helper** for users transitioning from built-in encryption to external secret management solutions (SOPS, etc.).

**Command name:** `vconfig` (short, friendly, memorable)

### Proposed Solution

**Package:** `Voyager.Configuration.Tool` (dotnet global tool)
**Command:** `vconfig` (short, memorable)

**Commands:**

```bash
# Install tool globally
dotnet tool install -g Voyager.Configuration.Tool

# Encrypt values in JSON file (creates library-compatible encrypted config)
vconfig encrypt --input config/database.json --output config/database.json --key-env ENCKEY
# Input:  { "ConnectionString": "Server=prod;Password=secret" }
# Output: { "ConnectionString": "AgB7CjEyMzQ1Njc4..." }

# Decrypt values in JSON file (for migration to SOPS)
vconfig decrypt --input config/database.json --output config/database.plain.json --key-env ENCKEY
# Input:  { "ConnectionString": "AgB7CjEyMzQ1Njc4..." }
# Output: { "ConnectionString": "Server=prod;Password=secret" }

# Encrypt single value (backward compatibility with old tools)
vconfig encrypt-value "text to encrypt"
# Output: AgB7CjEyMzQ1Njc4...

# Decrypt single value (backward compatibility with old tools)
vconfig decrypt-value "AgB7CjEyMzQ1Njc4..."
# Output: text to encrypt

# Batch encrypt multiple files
vconfig encrypt --batch "config/*.json" --output-dir config/encrypted --key-env ENCKEY

# Migrate: decrypt all values and prepare for SOPS
vconfig migrate --input config/secrets.json --output config/secrets.plain.json
```

### Tool Architecture

**Technology:**
- .NET Global Tool (packageable, versioned, easily distributed)
- System.CommandLine library for modern CLI parsing
- Multi-targeting: net6.0+ (no net48 for tools)

**Commands:**

1. **`encrypt`** - Encrypt values in JSON configuration file
   ```
   vconfig encrypt [OPTIONS]

   Options:
     -i, --input <FILE>       Input plain JSON file (required)
     -o, --output <FILE>      Output encrypted JSON file (default: overwrite input)
     -k, --key <KEY>          Encryption key (not recommended)
     --key-env <VAR>          Environment variable with key (default: ASPNETCORE_ENCODEKEY)
     -f, --force              Overwrite existing output file
     --batch <PATTERN>        Encrypt multiple files matching pattern
     --in-place               Encrypt file in-place (modify original)

   Encrypts string values in JSON while preserving structure.
   Numbers and booleans remain unencrypted.

   ⚠️  WARNING: Built-in encryption is deprecated. Use SOPS instead.
   This command creates files compatible with Voyager.Configuration.MountPath v1.x-v2.x.
   ```

2. **`decrypt`** - Decrypt values in JSON configuration file
   ```
   vconfig decrypt [OPTIONS]

   Options:
     -i, --input <FILE>       Input encrypted JSON file (required)
     -o, --output <FILE>      Output decrypted JSON file (required)
     -k, --key <KEY>          Encryption key (not recommended)
     --key-env <VAR>          Environment variable with key (default: ASPNETCORE_ENCODEKEY)
     -f, --force              Overwrite existing output file
     --batch <PATTERN>        Decrypt multiple files matching pattern

   Decrypts string values in JSON while preserving structure.
   Use this to prepare files for SOPS migration.
   ```

3. **`encrypt-value`** - Encrypt single text value
   ```
   vconfig encrypt-value <TEXT> [OPTIONS]

   Options:
     -k, --key <KEY>          Encryption key (not recommended)
     --key-env <VAR>          Environment variable with key

   Backward compatibility with Voyager.Configuration.Encrypt.
   Outputs encrypted base64 string.
   ```

4. **`decrypt-value`** - Decrypt single text value
   ```
   vconfig decrypt-value <ENCRYPTED_TEXT> [OPTIONS]

   Options:
     -k, --key <KEY>          Encryption key (not recommended)
     --key-env <VAR>          Environment variable with key

   Backward compatibility with Voyager.Configuration.Decrypt.
   Outputs decrypted plain text.
   ```

5. **`migrate`** - Decrypt and prepare for SOPS migration
   ```
   vconfig migrate [OPTIONS]

   Options:
     -i, --input <FILE>       Input encrypted JSON file (required)
     -o, --output <FILE>      Output plain JSON file (required)
     --key-env <VAR>          Environment variable with key
     --verify                 Verify JSON structure after decryption

   This command:
   1. Decrypts all values in JSON file
   2. Validates JSON structure
   3. Outputs plain text ready for SOPS encryption
   ```

6. **`version`** - Show version information
7. **`help`** - Show help for commands

### File Processing

**Value-level encryption (like SOPS):**

The tool encrypts **values within JSON structure**, not the entire file. This keeps the structure readable and Git-friendly.

```json
// Original plain file (database.json)
{
  "ConnectionStrings": {
    "Default": "Server=prod-db;Database=app;User=admin;Password=secret123"
  },
  "Database": {
    "Timeout": 30,
    "Host": "prod-server.example.com"
  }
}

// After encryption (values are encrypted, structure preserved)
{
  "ConnectionStrings": {
    "Default": "AgB7CjEyMzQ1Njc4OTBhYmNkZWYxMjM0NTY3ODkwYWJjZGVm..."
  },
  "Database": {
    "Timeout": 30,
    "Host": "AgBxMjM0NTY3ODkwYWJjZGVmMTIzNDU2Nzg5MGFiY2RlZg..."
  }
}

// After decryption (back to original)
{
  "ConnectionStrings": {
    "Default": "Server=prod-db;Database=app;User=admin;Password=secret123"
  },
  "Database": {
    "Timeout": 30,
    "Host": "prod-server.example.com"
  }
}
```

**Key features:**
- ✅ **Structure preserved** - Keys remain readable, only values encrypted
- ✅ **Git-friendly** - Can see which keys changed in diffs
- ✅ **Selective encryption** - Can encrypt only specific properties (e.g., passwords)
- ✅ **Type-aware** - Numbers, booleans remain unencrypted; strings can be encrypted
- ✅ **Library-compatible** - Output files work directly with `AddEncryptedMountConfiguration()`

**Note:** Similar to SOPS approach, but uses library's DES encryption. This is the format expected by `Voyager.Configuration.MountPath` encrypted file loading.

### Example Workflows

#### Workflow 1: Create Encrypted Config for Library

```bash
# 1. Create plain config file
cat > config/database.json <<EOF
{
  "ConnectionStrings": {
    "Default": "Server=prod-db;Database=myapp;User=admin;Password=Secret123!"
  },
  "Database": {
    "Timeout": 30
  }
}
EOF

# 2. Encrypt values in file (in-place)
export ASPNETCORE_ENCODEKEY="MyEncryptionKey12345"
vconfig encrypt --input config/database.json --in-place

# 3. Verify structure preserved
cat config/database.json
# Output:
# {
#   "ConnectionStrings": {
#     "Default": "AgB7CjEyMzQ1Njc4OTBhYmNkZWYxMjM0NTY3ODkwYWJjZGVm..."
#   },
#   "Database": {
#     "Timeout": 30
#   }
# }

# 4. Use in application with AddEncryptedMountConfiguration()
```

#### Workflow 2: Decrypt for SOPS Migration

```bash
# 1. Decrypt existing encrypted config
vconfig decrypt --input config/database.json --output config/database.plain.json

# 2. Verify decryption worked
cat config/database.plain.json
# Shows original values

# 3. Encrypt with SOPS
sops -e config/database.plain.json > config/database.json

# 4. Delete plain text file (security)
rm config/database.plain.json

# 5. Commit SOPS-encrypted file to Git
git add config/database.json
git commit -m "Migrate from DES to SOPS encryption"
```

#### Workflow 3: Batch Process Multiple Files

```bash
# Encrypt all plain configs
vconfig encrypt --batch "config/*.json" --output-dir config/encrypted

# Or decrypt all encrypted configs for migration
vconfig decrypt --batch "config/*.json" --output-dir config/decrypted

# Now ready for SOPS encryption
for file in config/decrypted/*.json; do
  sops -e "$file" > "config/$(basename $file)"
done
```

#### Workflow 4: Single Value Operations (Backward Compatibility)

```bash
# Encrypt single value (like old Voyager.Configuration.Encrypt)
vconfig encrypt-value "Server=prod;Password=secret"
# Output: AgB7CjEyMzQ1Njc4OTBhYmNkZWYxMjM0NTY3ODkwYWJjZGVm...

# Decrypt single value (like old Voyager.Configuration.Decrypt)
vconfig decrypt-value "AgB7CjEyMzQ1Njc4OTBhYmNkZWYxMjM0NTY3ODkwYWJjZGVm..."
# Output: Server=prod;Password=secret

# Use in shell scripts
ENCRYPTED=$(vconfig encrypt-value "my-secret")
echo "Encrypted: $ENCRYPTED"
```

## Alternatives Considered

### Alternative 1: Enhance Existing Console Apps

**Keep `Voyager.Configuration.Encrypt` and `Voyager.Configuration.Decrypt`, add file support**

**Pros:**
- Less breaking change
- Simpler implementation

**Cons:**
- Still not distributed as package
- Still have long names
- Separate tools for encrypt/decrypt
- No unified migration workflow

**Assessment:** Rejected - doesn't solve distribution or naming issues

### Alternative 2: Don't Build Any Tool

**Argument:** Encryption is deprecated, so why build tooling for it?

**Pros:**
- Less maintenance
- No investment in deprecated feature
- Forces users to adopt SOPS directly

**Cons:**
- **Existing users are stuck** - no way to decrypt their configs!
- Hostile to current user base
- Forces manual migration (copy each value, decrypt separately, paste back)
- Bad user experience

**Assessment:** Rejected - we owe existing users a migration path

### Alternative 3: PowerShell/Bash Scripts

**Provide migration scripts instead of compiled tool**

```powershell
# decrypt-config.ps1
param($InputFile, $OutputFile, $Key)
# ... load library, decrypt, save
```

**Pros:**
- No tool packaging needed
- Easy to modify for users

**Cons:**
- Platform-specific (separate .ps1 and .sh)
- Requires .NET SDK to reference library
- Not versioned or distributable
- Harder to maintain

**Assessment:** Rejected - less professional, harder to use

### Alternative 4: Web-based Tool

**Create a web application for encryption/decryption**

**Pros:**
- No installation needed
- Cross-platform by default
- Could have nice UI

**Cons:**
- **Security nightmare** - uploading secret configs to web app
- Requires hosting
- Privacy concerns
- Complexity

**Assessment:** Rejected - security implications are unacceptable

### Alternative 5: VS Code Extension

**Create VS Code extension for in-editor encryption/decryption**

**Pros:**
- Integrated into developer workflow
- Nice UI possibilities
- Context menu integration

**Cons:**
- Only works in VS Code
- Not scriptable (no CI/CD integration)
- Requires JavaScript/TypeScript development
- Overkill for simple task

**Assessment:** Rejected - too specialized, not scriptable

## Recommended Approach: Dotnet Global Tool

**Decision:** Create `vconfig` as a dotnet global tool

### Why This Is The Right Choice

1. **Solves All Problems**
   - ✅ Encrypts/decrypts entire files
   - ✅ Distributed as NuGet package
   - ✅ Short, memorable name
   - ✅ Supports migration to SOPS

2. **Professional Distribution**
   - Standard .NET tool installation (`dotnet tool install -g`)
   - Versioned and updatable
   - Works on all platforms (Windows, Linux, macOS)
   - Discoverable on NuGet

3. **Good User Experience**
   - Modern CLI with System.CommandLine
   - Help text, options, validation
   - Progress feedback
   - Clear error messages

4. **Migration-Focused**
   - Primary purpose: help users migrate away from built-in encryption
   - Temporary tool (can be deprecated in v3.0 along with encryption)
   - Clearly documented as migration helper

5. **Scriptable & Automatable**
   - Works in CI/CD pipelines
   - Batch processing support
   - Exit codes for error handling
   - Can be used in shell scripts

## Consequences

### Positive

1. **Better User Experience**
   - One command for both encryption and decryption
   - Entire file processing
   - Easy installation from NuGet

2. **Facilitates Migration**
   - Users can decrypt legacy configs
   - Batch processing for multiple files
   - Clear migration workflow to SOPS

3. **Professional Distribution**
   - Versioned tool
   - Easy updates
   - Cross-platform

4. **Scriptable**
   - Can be used in automation
   - CI/CD integration
   - Batch operations

5. **Deprecation Path**
   - Tool can be marked as deprecated in v2.x
   - Removed in v3.0 along with encryption
   - Time-limited: only needed during migration period

### Negative

1. **Development Effort**
   - Need to build and test CLI tool
   - Package as dotnet tool
   - Write documentation
   - Support and maintenance

2. **More Packages to Maintain**
   - Another NuGet package to publish
   - Versioning coordination
   - CI/CD for tool package

3. **Potential Confusion**
   - Users might think encryption is still recommended
   - Need clear deprecation warnings in tool output
   - Documentation must emphasize SOPS migration

4. **Temporary Tool**
   - Will be deprecated and removed in v3.0
   - Investment in tooling for deprecated feature
   - But: necessary for user migration

### Mitigation Strategies

1. **Clear Deprecation Warnings**
   ```bash
   $ vconfig encrypt file.json

   ⚠️  WARNING: Built-in encryption is DEPRECATED
   This tool is provided for migration purposes only.
   For new projects, use Mozilla SOPS instead.
   See: https://github.com/mozilla/sops

   Encrypting file.json...
   ```

2. **Migration-Focused Documentation**
   - README emphasizes migration workflow
   - Link to ADR-003 for SOPS guide
   - Examples show decrypt → SOPS workflow

3. **Limited Scope**
   - Only implement decrypt and migrate commands
   - Make encrypt command show strong warnings
   - Focus on helping users move away from encryption

4. **Deprecation Timeline**
   - v2.0: Tool released as migration helper
   - v2.x: Tool marked as deprecated
   - v3.0: Tool removed along with encryption library

## Implementation Plan

### Phase 1: Core Tool (v2.0)

1. **Create tool project**
   ```
   src/Voyager.Configuration.Tool/
   ├── Program.cs
   ├── Commands/
   │   ├── DecryptCommand.cs
   │   ├── EncryptCommand.cs
   │   └── MigrateCommand.cs
   ├── Services/
   │   ├── FileProcessor.cs
   │   └── EncryptionService.cs
   └── Voyager.Configuration.Tool.csproj
   ```

2. **Implement commands**
   - Use System.CommandLine library
   - Decrypt command with batch support
   - Migrate command with validation
   - Encrypt command with warnings

3. **Package configuration**
   ```xml
   <Project Sdk="Microsoft.NET.Sdk">
     <PropertyGroup>
       <OutputType>Exe</OutputType>
       <TargetFramework>net6.0</TargetFramework>
       <PackAsTool>true</PackAsTool>
       <ToolCommandName>vconfig</ToolCommandName>
       <PackageId>Voyager.Configuration.Tool</PackageId>
       <IsPackable>true</IsPackable>
     </PropertyGroup>
   </Project>
   ```

4. **Documentation**
   - README for tool
   - Migration guide
   - Examples for common scenarios

5. **CI/CD**
   - Build and test tool
   - Publish to NuGet
   - Version coordination with library

### Phase 2: Deprecation (v2.x)

1. Mark tool as deprecated in description
2. Add warnings to all commands
3. Update documentation to emphasize SOPS

### Phase 3: Removal (v3.0)

1. Remove tool from repository
2. Unlist from NuGet
3. Update migration guide (historical reference)

## Tool Naming

### Selected: `vconfig`

**Pros:**
- Short and memorable (5 characters)
- Clear purpose (v = Voyager, config = configuration)
- Easy to type
- Friendly and concise

**Alternatives considered:**
- `voyager-config` - Too long, initial candidate
- `voy-config` - Shorter but less clear
- `voyager-crypt` - Sounds too permanent
- `voyager-migrate` - Too specific to one command
- `vcfg` - Too cryptic

## Breaking Changes

### For Users of Existing Tools

**Current (deprecated):**
```bash
Voyager.Configuration.Decrypt.exe "encrypted_value"
```

**New:**
```bash
vconfig decrypt --input file.json --output plain.json
```

**Migration path:**
1. Install new tool: `dotnet tool install -g Voyager.Configuration.Tool`
2. Use new commands instead of old executables
3. Old executables can be removed from distribution

**Note:** Old tools can remain in repository but not packaged in v2.0+

## Security Considerations

1. **Key Handling**
   - Never log encryption keys
   - Support environment variables (not command-line args)
   - Warn if key provided via --key option
   - Clear key from memory after use

2. **File Permissions**
   - Check output file permissions
   - Warn if output file is world-readable
   - Recommend secure file permissions

3. **Temporary Files**
   - No temporary files written to disk
   - Process in-memory where possible
   - Secure cleanup if temp files needed

4. **Migration Workflow**
   - Remind users to delete plain text files after SOPS encryption
   - Warn about plain text files in output

## Success Metrics

1. **Adoption**
   - Number of NuGet downloads
   - User feedback on migration experience

2. **Migration Success**
   - Users successfully decrypt configs
   - Users migrate to SOPS
   - Positive feedback on migration guide

3. **Tool Quality**
   - No security issues reported
   - Clear error messages
   - Batch processing works reliably

## References

### Related ADRs

- [ADR-003: Encryption Delegation to External Tools](ADR-003-encryption-delegation-to-external-tools.md) - Why encryption is deprecated, SOPS migration guide

### Technology

- [System.CommandLine](https://github.com/dotnet/command-line-api) - Modern command-line parsing
- [.NET Global Tools](https://docs.microsoft.com/en-us/dotnet/core/tools/global-tools) - Packaging and distribution
- [Mozilla SOPS](https://github.com/mozilla/sops) - Recommended alternative

### Similar Tools

- [dotnet-ef](https://www.nuget.org/packages/dotnet-ef/) - Entity Framework Core tools
- [dotnet-user-secrets](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets) - Secret management for development

## Appendix: Example Implementation

### Command Structure

```csharp
// Program.cs
using System.CommandLine;

var rootCommand = new RootCommand("Voyager Configuration Tool - Migration helper for legacy encryption");

var decryptCommand = new Command("decrypt", "Decrypt configuration file(s)");
decryptCommand.AddOption(new Option<string>("--input", "Input encrypted file"));
decryptCommand.AddOption(new Option<string>("--output", "Output decrypted file"));
decryptCommand.AddOption(new Option<string>("--key-env", () => "ASPNETCORE_ENCODEKEY", "Environment variable with key"));
decryptCommand.SetHandler(async (string input, string output, string keyEnv) =>
{
    await DecryptCommand.Execute(input, output, keyEnv);
}, /* bind options */);

rootCommand.AddCommand(decryptCommand);
// ... other commands

return await rootCommand.InvokeAsync(args);
```

### Deprecation Warning

```csharp
Console.ForegroundColor = ConsoleColor.Yellow;
Console.WriteLine("⚠️  WARNING: Built-in encryption is DEPRECATED");
Console.WriteLine("This tool is provided for migration purposes only.");
Console.WriteLine("For new projects, use Mozilla SOPS instead.");
Console.WriteLine("See: https://github.com/mozilla/sops");
Console.WriteLine();
Console.ResetColor();
```

### JSON Processing Logic

```csharp
// Encrypt JSON - recursively process all string values
public static JsonNode EncryptJson(JsonNode node, IEncryptor encryptor)
{
    if (node is JsonValue value)
    {
        // Encrypt only string values
        if (value.TryGetValue<string>(out var str))
        {
            return JsonValue.Create(encryptor.Encrypt(str));
        }
        // Numbers, booleans remain unchanged
        return value;
    }
    else if (node is JsonObject obj)
    {
        var result = new JsonObject();
        foreach (var (key, val) in obj)
        {
            result[key] = val != null ? EncryptJson(val, encryptor) : null;
        }
        return result;
    }
    else if (node is JsonArray arr)
    {
        var result = new JsonArray();
        foreach (var item in arr)
        {
            result.Add(item != null ? EncryptJson(item, encryptor) : null);
        }
        return result;
    }
    return node;
}
```

**Example transformation:**
```json
// Input (plain)
{
  "ConnectionStrings": {
    "Default": "Server=prod;Password=secret"
  },
  "Database": {
    "Timeout": 30,
    "EnableRetry": true,
    "Hosts": ["host1", "host2"]
  }
}

// Output (encrypted)
{
  "ConnectionStrings": {
    "Default": "AgB7CjEyMzQ1Njc4..."  // ← Encrypted
  },
  "Database": {
    "Timeout": 30,                      // ← Unchanged (number)
    "EnableRetry": true,                // ← Unchanged (boolean)
    "Hosts": [
      "AgBxMjM0NTY3ODkw...",           // ← Encrypted
      "AgByMzQ1Njc4OTBh..."            // ← Encrypted
    ]
  }
}
```

### Batch Processing

```csharp
// Encrypt multiple files matching pattern
var files = Directory.GetFiles(inputDir, pattern);
foreach (var file in files)
{
    var outputFile = Path.Combine(outputDir, Path.GetFileName(file));
    Console.WriteLine($"Encrypting {file} -> {outputFile}");
    await EncryptJsonFile(file, outputFile, key);
}
Console.WriteLine($"✓ Encrypted {files.Length} files");
```
