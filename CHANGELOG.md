# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.3.0-preview.1] - Unreleased

### Added

- **AES-256-GCM encryption** — replaces DES as the default cipher for new writes (ADR-010)
  - `AesGcmCipherProvider` — 12-byte random nonce, 16-byte authentication tag, Base64 32-byte key validation
  - Versioned wire format `v2:BASE64(nonce || ciphertext || tag)` — deterministic dispatch, no try/catch between algorithms
  - On .NET Framework 4.8: BouncyCastle `GcmBlockCipher` polyfill (new dependency `BouncyCastle.Cryptography 2.4.0`, net48 only). Wire format byte-for-byte identical across all TFMs
  - Wrong key or tampered ciphertext → `AuthenticationTagMismatchException` (never silent garbage)

- **`VersionedEncryptor`** — dual-mode `IEncryptor` routing on `v2:` prefix
  - Writes always AES-256-GCM (`v2:...`)
  - Reads both AES (`v2:`) and legacy DES (raw Base64), controlled by `AllowLegacyDes` flag (default `true`)
  - One-shot migration warning via `Trace.TraceWarning` when legacy DES value is read
  - Thread-safe warning emission (`Interlocked.CompareExchange`)

- **`DefaultEncryptorFactory` auto-detection** — returns `VersionedEncryptor` for Base64-32 AES keys; falls back to legacy `Encryptor` for old short-string DES keys (zero breaking changes)

- **CLI: `vconfig keygen`** — generates a cryptographically random AES-256 key (32 bytes, Base64) with security warning

- **CLI: `vconfig reencrypt`** — migrates DES-encrypted JSON files to AES-256-GCM
  - `--input`, `--legacy-key-env`, `--new-key-env`, `--dry-run`
  - Idempotent: values already prefixed `v2:` are left untouched
  - DES garbage detection: post-decrypt U+FFFD check prevents silent data loss on Base64-looking plaintext values
  - Reports migrated / already-AES / total counts

- **ADR-010** — status changed from Proposed to Accepted; documents AES-256-GCM decision, versioned ciphertext format, DES garbage detection, and staged legacy removal plan (v3: AllowLegacyDes=false, v4: DES removed)

### Changed

- **`DefaultEncryptorFactory`** — now produces `VersionedEncryptor` when key is a valid AES-256 key; existing DES deployments with short-string keys continue to work unchanged

### Deprecated

- **Legacy DES encryption** — `AllowLegacyDes` defaults to `true` in v2.x. Will default to `false` in v3.x and be removed in v4.x. Run `vconfig reencrypt` to migrate.

### Security

- **Authenticated encryption** — AES-256-GCM replaces unauthenticated DES-CBC. Wrong key or tampered values now always throw instead of silently returning garbage.
- **Plaintext stays off disk** — in-memory decryption at `IConfiguration` level preserved. No SOPS-style plaintext-on-disk exposure to AI agents, IDE indexers, or swap files.

---

## [Unreleased]

### Added

- **Build infrastructure** - Centralized build configuration using Directory.Build.props
  - `Directory.Build.props` - Main configuration file applying to all projects
  - `build/Build.Versioning.props` - MinVer 7.0.0 for automatic versioning from Git tags
  - `build/Build.CodeQuality.props` - Code analyzers, TreatWarningsAsErrors, deterministic builds
  - `build/Build.SourceLink.props` - SourceLink for GitHub integration
  - `build/Build.NuGet.props` - NuGet package settings

- **Code style enforcement** - Added `.editorconfig` with comprehensive C# coding conventions
  - Naming conventions (interfaces, types, fields)
  - Formatting rules (braces, spacing, indentation)
  - Code style preferences (var usage, expression-bodied members, pattern matching)

### Changed

- **Versioning** - Switched from manual version in .csproj to MinVer automatic versioning
  - Version is now calculated from Git tags (format: `v1.2.3`)
  - Assembly version uses major version only for binary compatibility
  - Pre-release builds automatically get `preview` suffix

- **Company name** - Updated from "Voyager.com sp. z o.o." to "Sindbad IT sp. z o.o."

- **Project files simplified** - Removed duplicated settings now inherited from Directory.Build.props
  - Voyager.Configuration.MountPath.csproj
  - Voyager.Configuration.Encrypt.csproj
  - Voyager.Configuration.Decrypt.csproj

### Fixed

- **Build compatibility** - Added NoWarn for known issues in existing code
  - SYSLIB0021 - DESCryptoServiceProvider obsolete warning (to be refactored later)
  - NU1701 - .NET Framework package compatibility in Owin test project
  - Disabled nullable for projects with legacy code

## [1.2.8] - Previous Release

- Last manually versioned release before MinVer adoption
