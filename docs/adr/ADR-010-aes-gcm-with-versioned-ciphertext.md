# ADR-010: AES-256-GCM Encryption with Versioned Ciphertext and Legacy DES Fallback

## Status

Accepted

**Date**: 2026-04-15

## Context

Three pressures converge on the encryption story:

1. **DES is insecure and marked obsolete.** `System.Security.Cryptography.DES` raises `SYSLIB0021`. 56-bit keys are brute-forceable; DES-CBC has no authentication, so tampering and wrong-key decryption can silently return garbage rather than throwing.

2. **[ADR-003](ADR-003-encryption-delegation-to-external-tools.md) proposed migrating to SOPS** instead of implementing AES. The plan was: deprecate built-in encryption, point users at SOPS. In practice this created two problems:
   - **Plaintext-on-disk threat.** The common SOPS workflow (`sops -d file.json > plain.json`) leaves decrypted secrets sitting on disk, where AI coding agents, IDE indexers, swap files, backups, and other processes can read them without any audit trail. Voyager's original model (decrypt in-memory at `IConfiguration` level, plaintext never touches disk) is actually **stronger** against this threat than naive SOPS usage.
   - **Operational cost pushed to users.** SOPS requires age/PGP/KMS setup, key distribution tooling, and team training. For small-to-medium deployments this is disproportionate.

3. **Encryption was treated as a second-class feature** in documentation because the team assumed it was being deprecated. README lacks a clear "how to encrypt your config" section. Deprecation warnings printed by the CLI tool ([Program.cs:250-260](../../src/Voyager.Configuration.Tool/Program.cs#L250-L260)) steer users away from the very feature they are trying to use.

### The AI-agent threat model, explicitly

Modern development loops include AI agents with broad filesystem read access. An agent's `Read` tool does not distinguish `plain.json` from `README.md`. A plaintext secrets file in the repo tree is:
- readable by the agent on any task,
- may be included in context sent to LLM providers,
- indexed by IDE search, embeddings, and telemetry,
- captured by editor swap files, VCS stash, and OS-level backups.

Keeping plaintext **off disk** is therefore a first-order security property, not a nice-to-have. This is the property Voyager's in-process decryption has always provided — and the one the SOPS-migration path risks losing.

## Decision

Replace the deprecation path with a **pragmatic AES-256-GCM implementation** that preserves in-memory decryption, supports legacy DES files during transition, and re-promotes encryption to a first-class, documented feature.

### 1. Algorithm — AES-256-GCM

- Use `System.Security.Cryptography.AesGcm` from the BCL.
- 256-bit key (see §2 "Key material" below).
- 12-byte random nonce per value (`RandomNumberGenerator.GetBytes(12)`).
- 16-byte authentication tag verifies integrity (wrong key / tampered value → `AuthenticationTagMismatchException`, never silent garbage).
- Per-value encryption (not per-file), matching the current DES behavior and `IConfiguration` semantics.

### 2. Key material — raw 32-byte key, Base64 in env var

The encryption key is **32 random bytes**, stored Base64-encoded in an environment variable (default `ASPNETCORE_ENCODEKEY`, matching today's convention):

```
ASPNETCORE_ENCODEKEY=yK3vM9pQ+L2nR5sT8wXaB1cD4eF7gH0iJ2kL3mN4oP8=
```

**Generation — new CLI command `vconfig keygen`:**

```bash
vconfig keygen
# → yK3vM9pQ+L2nR5sT8wXaB1cD4eF7gH0iJ2kL3mN4oP8=
#
# Save this value in ASPNETCORE_ENCODEKEY.
# Anyone with this key can decrypt your configuration.
```

Implementation is trivial:

```csharp
var bytes = RandomNumberGenerator.GetBytes(32);
Console.WriteLine(Convert.ToBase64String(bytes));
```

**Startup validation** in `AesGcmCipherProvider`:
- Decode Base64; fail fast with `EncryptionException` on invalid Base64.
- Require decoded length == 32 bytes; fail with a clear message pointing at `vconfig keygen`.

**No PBKDF2 / password derivation.** Alternatives considered and rejected:

1. *Accept any string; derive key via PBKDF2 with constant salt.* Rejected — constant salt defeats the purpose; an attacker can precompute a rainbow table once per deployment.
2. *PBKDF2 with per-value salt prepended to ciphertext.* Rejected — adds wire-format complexity and noticeable startup cost (50+ values × 100k iterations each). If we already need a proper wire format, we may as well require a real key.
3. *Accept weak passwords and "upgrade" them via PBKDF2.* Rejected — gives a false sense of security. PBKDF2 on `password123` is still guessable; the UX suggests otherwise.

Industry practice for symmetric-at-rest keys is a random high-entropy key from a KMS / vault / env var, not a password-derived key. Voyager follows that.

**DES → AES key migration.** The legacy DES key (8-byte, often derived from an arbitrary string password) **cannot be reused** as an AES-256 key. Users generate a new key and re-encrypt:

```bash
# 1. Generate new AES key
export ASPNETCORE_AES_KEY=$(vconfig keygen)

# 2. Re-encrypt: old DES key decrypts, new AES key encrypts
vconfig reencrypt \
  --input config/secrets.json \
  --legacy-key-env ASPNETCORE_ENCODEKEY \
  --new-key-env ASPNETCORE_AES_KEY

# 3. Swap env vars in deployment (AES_KEY → ENCODEKEY)
# 4. Remove old DES key from secret manager / vault
```

The `reencrypt` command therefore accepts **two** key env vars during migration; steady-state deployments still use a single `ASPNETCORE_ENCODEKEY`.

### 3. Wire format — versioned ciphertext

Every AES-encrypted value carries an explicit version prefix:

```
v2:BASE64(nonce || ciphertext || tag)
```

Values without a prefix are treated as **legacy DES ciphertext**. This makes algorithm dispatch **deterministic** — no try/catch guessing, no risk of DES-CBC returning silent garbage for an AES ciphertext.

### 4. Dual-mode read, single-mode write

A new `VersionedEncryptor : IEncryptor` routes on the prefix:

```csharp
public string Decrypt(string value)
{
    if (value.StartsWith("v2:", StringComparison.Ordinal))
        return _aes.Decrypt(value.AsSpan(3));

    if (!_allowLegacyDes)
        throw new EncryptionException("Legacy DES ciphertext detected but AllowLegacyDes is disabled.");

    _logger?.LogWarning("Decrypting legacy DES value — run `vconfig reencrypt` to migrate.");
    return _legacyDes.Decrypt(value);
}

public string Encrypt(string value) => _aes.Encrypt(value);   // always AES
```

- **Reads:** both AES (`v2:...`) and DES (raw Base64) work, controlled by `AllowLegacyDes` flag (default `true` in v2.x, `false` in v3.x, removed in v4.x).
- **Writes:** always AES. This guarantees the plaintext surface shrinks monotonically — a legacy file re-saved after any edit is fully migrated.
- **Mixed files supported** during migration — a file can contain both `v2:...` and legacy values.

### 5. Migration tooling

New CLI command `vconfig reencrypt`:

```bash
vconfig reencrypt --input config/secrets.json
```

- Reads through `VersionedEncryptor` (accepts DES and AES).
- Writes through `AesGcmEncryptor` (always `v2:...`).
- Leaves already-AES values untouched (idempotent).
- Supports `--dry-run` and reports count of migrated values.
- **DES garbage detection:** DES-CBC has no authentication — a plaintext value that happens to be valid Base64 with length divisible by 8 can DES-"decrypt" without error, producing garbage bytes. The reencrypt command checks for U+FFFD (Unicode replacement character) in the decrypted output; `Encoding.UTF8.GetString` replaces invalid byte sequences with U+FFFD, so garbage DES output reliably triggers this check. Values that produce U+FFFD are left untouched (treated as non-encrypted plaintext). This prevents silent data loss on config files containing mixed plaintext and encrypted values — a scenario explicitly supported by the versioned ciphertext design.

### 6. Remove SOPS-migration messaging

SOPS stays **supported as an option** (nothing stops users from integrating it), but is no longer the recommended default. The tool stops nudging users toward it.

### 7. Re-promote encryption to a first-class feature in docs

Encryption usage was deliberately under-documented in the README while the team believed it was being deprecated. This gets reversed.

### Alternatives considered

1. **Try-catch algorithm dispatch (try DES, fall back to AES, or vice versa).** Rejected: DES-CBC has no authentication, so wrong-algorithm decryption can succeed silently with garbage output in a non-negligible fraction of cases. Versioned prefix eliminates the guessing.
2. **Continue with SOPS-only direction (ADR-003).** Rejected in light of the AI-agent threat model and the operational cost to small teams. See Context.
3. **Keep DES, wait for SOPS adoption.** Rejected: DES stays insecure; `SYSLIB0021` pressure grows; users are actively hitting [issues #4](https://github.com/Voyager-Poland/Voyager.Configuration.MountPath/issues/4) / [#6](https://github.com/Voyager-Poland/Voyager.Configuration.MountPath/issues/6) with the current tooling.
4. **AES-CBC instead of AES-GCM.** Rejected: CBC needs separate HMAC for integrity; GCM gives authenticated encryption in one primitive and is in the BCL.

## Consequences

### Positive

- **Modern cryptography** — AES-256-GCM replaces 56-bit DES; authenticated encryption eliminates silent-corruption failure modes.
- **Plaintext stays off disk** — preserves Voyager's strongest property against AI-agent / indexer / backup exposure.
- **Zero breaking changes for consumers** — `IEncryptor` / `IConfiguration` API unchanged; existing DES-encrypted configs continue to load.
- **Deterministic migration path** — version prefix, re-encrypt command, staged removal of legacy support.
- **Encryption becomes a first-class, documented feature** again, appropriate for the typical Voyager user (containerized .NET apps with env-var key management).

### Negative

- **Key management unchanged in scope** — still a single symmetric key in an env var. No rotation, no audit log, no KMS integration. Organizations with those requirements should integrate SOPS+KMS through a custom `IEncryptor` (the extension point is preserved).
- **~200 LoC of crypto code to own.** Mitigated by using BCL `AesGcm` (no custom primitives), comprehensive test vectors, and keeping the implementation small and auditable.
- **Migration window** — users must run `vconfig reencrypt` before v4.x removes DES. Communicated via changelog and runtime warning logs.

### Neutral

- SOPS remains viable for users who prefer it; `IEncryptor` is an extension point.
- Existing `LegacyDesCipherProvider` class already exists as an internal abstraction and can be reused.

### Breaking Changes

- **v2.x (this ADR):** none. AES becomes default for new writes; DES reads still work.
- **v3.x:** `AllowLegacyDes` defaults to `false`. Users must opt in or re-encrypt. Announced one minor version ahead.
- **v4.x:** `LegacyDesCipherProvider` removed entirely. Un-migrated files fail to load with a clear error pointing at `vconfig reencrypt`.

## Implementation Plan

### Phase 1 — AES implementation (v2.next)

1. Add `AesGcmCipherProvider : ICipherProvider` using `System.Security.Cryptography.AesGcm`.
2. Add `VersionedEncryptor : IEncryptor` dispatching on the `v2:` prefix.
3. Wire `VersionedEncryptor` as the default in `IEncryptorFactory`, keeping `LegacyDesCipherProvider` available for reads.
4. Unit tests: round-trip, tamper detection, wrong-key rejection, mixed-file (AES + DES values) loading, nonce uniqueness.
5. Update `Encryptor.Encrypt` call sites to emit `v2:` format.

### Phase 2 — Migration tooling (v2.next)

6. Implement `vconfig keygen` command — emits a fresh Base64-encoded 32-byte key via `RandomNumberGenerator.GetBytes(32)`.
7. Implement `vconfig reencrypt` command (accepts DES + AES on read, writes AES; accepts `--legacy-key-env` and `--new-key-env`).
8. Add `--dry-run` and summary output.
9. Integration tests against fixtures containing pure-DES, pure-AES, and mixed files.

### Phase 3 — De-deprecation and documentation (v2.next)

10. **Remove deprecation warnings:**
    - Delete `ShowDeprecationWarning()` in [Program.cs:250-260](../../src/Voyager.Configuration.Tool/Program.cs#L250-L260).
    - Remove `⚠️ WARNING: Built-in encryption is DEPRECATED` banner from `vconfig encrypt`.
    - Remove the "You can now encrypt this file with SOPS" hint from `vconfig decrypt`. Replace with a neutral message.
    - Remove `[Obsolete]` attribute from public encryption types; **keep** `[Obsolete]` on `LegacyDesCipherProvider` only.
    - Update `Voyager.Configuration.Tool.csproj` and `Voyager.Configuration.MountPath.csproj` — remove any package-level `<PackageTags>deprecated</PackageTags>` or similar.
11. **Add encryption documentation to README.md:**
    - New section "Encrypting configuration" (placed prominently, not buried).
    - Quickstart: key generation, env var setup, `vconfig encrypt` example, runtime `AddEncryptedMountConfiguration` example.
    - Threat model note: why in-memory decryption matters (AI-agent / indexer / backup exposure).
    - Link to AES-vs-SOPS comparison for users deciding.
    - Migration section for DES users: one-liner `vconfig reencrypt`.
12. Update [MIGRATION.md](../MIGRATION.md) — remove the SOPS-only migration section; add a DES→AES section.
13. Update [ROADMAP.md](../ROADMAP.md) — remove "Deprecate encryption features" task; add AES-GCM implementation task.
14. Update samples: rename `samples/MigrationToSops/` to `samples/MigrationFromDes/` (or add new sample alongside).

### Phase 4 — Staged legacy removal

15. **v3.x:** change `AllowLegacyDes` default to `false`. Runtime logs a clear migration command when disabled config encounters DES ciphertext. Announce in CHANGELOG one version ahead.
16. **v4.x:** remove `LegacyDesCipherProvider` and related DES code paths entirely.

### Phase 5 — ADR housekeeping

17. Update [ADR-003](ADR-003-encryption-delegation-to-external-tools.md) status to **Superseded by ADR-010** (only the "deprecate built-in encryption" conclusion is superseded; SOPS-as-option remains documented).
18. Add ADR-010 to [ADR index](README.md).

## References

- [Issue #4 — numeric value encryption bug](https://github.com/Voyager-Poland/Voyager.Configuration.MountPath/issues/4)
- [Issue #6 — issue #4 not fully resolved](https://github.com/Voyager-Poland/Voyager.Configuration.MountPath/issues/6)
- [ADR-003: Encryption Delegation to External Tools](ADR-003-encryption-delegation-to-external-tools.md) — partially superseded by this ADR
- [ADR-008: Encrypting Non-String JSON Values](ADR-008-encrypting-non-string-json-values.md) — type handling carries over to AES
- [NIST SP 800-38D: AES-GCM](https://csrc.nist.gov/publications/detail/sp/800-38d/final)
- [.NET `AesGcm` documentation](https://learn.microsoft.com/dotnet/api/system.security.cryptography.aesgcm)
- `SYSLIB0021` — DES obsolete warning

## Related ADRs

- **Supersedes** (in part): [ADR-003](ADR-003-encryption-delegation-to-external-tools.md) — the "deprecate built-in encryption" conclusion. SOPS remains supported as an extension point.
- **Builds on:** [ADR-008](ADR-008-encrypting-non-string-json-values.md) — non-string primitive handling applies identically to AES.
- **Related:** [ADR-004](ADR-004-cli-tool-for-configuration-encryption.md) — CLI tool gains `reencrypt` command.
