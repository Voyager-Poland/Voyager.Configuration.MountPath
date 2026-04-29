# vconfig - Voyager Configuration Tool

CLI tool for encrypting and decrypting JSON configuration files compatible with [Voyager.Configuration.MountPath](https://github.com/Voyager-Poland/Voyager.Configuration.MountPath) library.

Default cipher is **AES-256-GCM** with versioned ciphertext (`v2:` prefix). Legacy DES-encrypted files remain readable for migration via the `reencrypt` command.

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

### Generate an Encryption Key

Generate a fresh AES-256 key (Base64-encoded). The key is printed to stdout; the warning text goes to stderr so you can pipe stdout to a file or environment variable:

```bash
vconfig keygen
```

Save the value in the `ASPNETCORE_ENCODEKEY` environment variable. Anyone with the key can decrypt your configuration.

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
  "ConnectionString": "v2:AAECAwQFBgcICQoLDA0OD...",
  "Timeout": 30
}
```

### Decrypt JSON Configuration

Decrypt an encrypted JSON file to a new plain file:

```bash
vconfig decrypt --input config/secrets.json --output config/secrets.plain.json
```

### Re-encrypt Legacy DES → AES-256-GCM

Migrate a file encrypted with the legacy DES cipher to AES-256-GCM. Reads with the legacy DES key and writes with the new AES key. Use `--dry-run` first to see what would change:

```bash
# Preview migration without writing
vconfig reencrypt \
  --input config/secrets.json \
  --legacy-key-env ASPNETCORE_ENCODEKEY \
  --new-key-env NEW_AES_KEY \
  --dry-run

# Apply migration in-place
vconfig reencrypt \
  --input config/secrets.json \
  --legacy-key-env ASPNETCORE_ENCODEKEY \
  --new-key-env NEW_AES_KEY
```

`--legacy-key-env` defaults to `ASPNETCORE_ENCODEKEY`. After migration, point your application at the new AES key.

### Single Value Operations

Backward compatible with old `Voyager.Configuration.Encrypt/Decrypt` tools:

```bash
# Encrypt single value
vconfig encrypt-value "text to encrypt"

# Decrypt single value
vconfig decrypt-value "v2:AAECAwQFBgcICQoLDA0OD..."
```

## Encryption Key

The tool reads the encryption key from an environment variable (default: `ASPNETCORE_ENCODEKEY`):

```bash
# Set encryption key
export ASPNETCORE_ENCODEKEY="$(vconfig keygen)"

# Or use a custom environment variable
vconfig encrypt --input config.json --key-env MY_KEY_VAR
```

Passing the key via `--key` is supported but not recommended (it appears in process listings and shell history).

## Commands

- `keygen` - Generate a new AES-256 encryption key
- `encrypt` - Encrypt values in JSON file
- `decrypt` - Decrypt values in JSON file
- `reencrypt` - Re-encrypt a JSON file from legacy DES to AES-256-GCM
- `encrypt-value` - Encrypt single text value
- `decrypt-value` - Decrypt single text value

Run `vconfig --help` or `vconfig <command> --help` for detailed usage.

## Links

- **GitHub**: https://github.com/Voyager-Poland/Voyager.Configuration.MountPath
- **Library Package**: [Voyager.Configuration.MountPath](https://www.nuget.org/packages/Voyager.Configuration.MountPath)
- **Encryption design**: [ADR-010: AES-256-GCM with versioned ciphertext](https://github.com/Voyager-Poland/Voyager.Configuration.MountPath/blob/main/docs/adr/ADR-010-aes-gcm-with-versioned-ciphertext.md)
- **CLI design**: [ADR-004: CLI Tool](https://github.com/Voyager-Poland/Voyager.Configuration.MountPath/blob/main/docs/adr/ADR-004-cli-tool-for-configuration-encryption.md)
- **License**: MIT
