# vconfig - Voyager Configuration Tool

CLI tool for encrypting and decrypting JSON configuration files compatible with [Voyager.Configuration.MountPath](https://github.com/Voyager-Poland/Voyager.Configuration.MountPath) library.

> **⚠️ DEPRECATION WARNING**
> Built-in encryption is deprecated. This tool is provided for **migration purposes only**.
> For new projects, use [Mozilla SOPS](https://github.com/mozilla/sops) instead.

## Installation

Install globally as a .NET tool:

```bash
dotnet tool install -g Voyager.Configuration.Tool
```

Update to latest version:

```bash
dotnet tool update -g Voyager.Configuration.Tool
```

## Usage

### Encrypt JSON Configuration

Encrypts string values in JSON while preserving structure:

```bash
# Encrypt file in-place
vconfig encrypt --input config/secrets.json --in-place

# Encrypt to new file
vconfig encrypt --input config/secrets.json --output config/secrets.encrypted.json
```

**Example:**

```json
// Input (plain)
{
  "ConnectionString": "Server=prod;Password=secret123",
  "Timeout": 30
}

// Output (encrypted - structure preserved, only strings encrypted)
{
  "ConnectionString": "AgB7CjEyMzQ1Njc4OTBhYmNkZWY...",
  "Timeout": 30
}
```

### Decrypt for Migration to SOPS

Decrypt existing encrypted configs to migrate to SOPS:

```bash
# Decrypt file
vconfig decrypt --input config/secrets.json --output config/secrets.plain.json

# Then encrypt with SOPS
sops -e config/secrets.plain.json > config/secrets.json
```

### Single Value Operations

Backward compatible with old `Voyager.Configuration.Encrypt/Decrypt` tools:

```bash
# Encrypt single value
vconfig encrypt-value "text to encrypt"

# Decrypt single value
vconfig decrypt-value "AgB7CjEyMzQ1Njc4..."
```

## Encryption Key

The tool reads the encryption key from an environment variable (default: `ASPNETCORE_ENCODEKEY`):

```bash
# Set encryption key
export ASPNETCORE_ENCODEKEY="your-encryption-key"

# Or use custom environment variable
vconfig encrypt --input config.json --key-env MY_KEY_VAR
```

## Commands

- `encrypt` - Encrypt values in JSON file (for library compatibility)
- `decrypt` - Decrypt values in JSON file (for SOPS migration)
- `encrypt-value` - Encrypt single text value
- `decrypt-value` - Decrypt single text value

Run `vconfig --help` or `vconfig <command> --help` for detailed usage.

## Migration Guide

See the [complete migration guide](https://github.com/Voyager-Poland/Voyager.Configuration.MountPath/blob/main/docs/adr/ADR-003-encryption-delegation-to-external-tools.md) for:

- Why encryption is deprecated
- Step-by-step SOPS migration
- Kubernetes and Supervisor deployment examples
- Security best practices

## Links

- **GitHub**: https://github.com/Voyager-Poland/Voyager.Configuration.MountPath
- **Library Package**: [Voyager.Configuration.MountPath](https://www.nuget.org/packages/Voyager.Configuration.MountPath)
- **Documentation**: [ADR-004: CLI Tool](https://github.com/Voyager-Poland/Voyager.Configuration.MountPath/blob/main/docs/adr/ADR-004-cli-tool-for-configuration-encryption.md)
- **License**: MIT
