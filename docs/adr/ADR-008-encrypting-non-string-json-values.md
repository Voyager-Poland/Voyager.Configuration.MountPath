## ADR-008: Encrypting Non-String JSON Values (Numbers, Booleans, Null)

## Status

Accepted

**Date**: 2026-04-15

## Context

The built-in encryption (both the runtime `EncryptedJsonConfigurationProvider` and the `vconfig` CLI tool) originally assumed every value in a configuration JSON file was a string. Real-world configuration files, however, routinely contain other JSON primitives — numbers, booleans, and `null`:

```json
{
  "MyTemplate": {
    "IdTemplate": 77,
    "prawda": true
  }
}
```

Two production bugs were reported around this assumption:

- **[Issue #4](https://github.com/Voyager-Poland/Voyager.Configuration.MountPath/issues/4)** — "Problem z szyfrowaniem wartości liczbowych". The CLI tool walked the JSON tree and only encrypted `JsonValueKind.String` nodes; numeric and boolean nodes were written back to the output file unchanged. At runtime, `EncryptedJsonConfigurationProvider` then tried to Base64-decode every value (including the untouched `77`) and threw:

  ```
  Voyager.Configuration.MountPath.EncryptionException :
    Failed to decrypt configuration value in 'mail.json'.
    Key: 'MailConfig:TicketSaleMailSettings:IdTemplate'.
    The value is not in a valid encrypted format.
    ----> System.FormatException : The input is not a valid Base-64 string...
  ```

- **[Issue #6](https://github.com/Voyager-Poland/Voyager.Configuration.MountPath/issues/6)** — "poprzedni błąd nie został rozwiązany". The first remediation was not sufficient: users who already had partially-encrypted files in production continued to hit the same `FormatException` because only the CLI had been touched in the intermediate attempt. The runtime provider still treated every token as an encrypted Base64 string.

### Root cause analysis

The confusion stems from a subtle impedance mismatch. ASP.NET's `JsonConfigurationFileParser` flattens a JSON document into a `Dictionary<string, string>` — so downstream, via `IConfiguration`, both `77` and `"77"` are indistinguishable. But the *encryption* layer sits **below** that flattening, operating on the raw `JsonNode` tree, where `77` (a `JsonValueKind.Number`) and `"77"` (a `JsonValueKind.String`) are distinct.

A symmetric fix needs to happen on both sides:
1. The **writer** (CLI tool) must produce an encrypted ciphertext string for every non-null primitive, regardless of its original JSON kind.
2. The **reader** (runtime provider) must be tolerant of historical files where primitives were left unencrypted.

## Decision

We implemented a **two-part fix**, aligning the writer and the reader with the behavior of `JsonConfigurationFileParser`:

### Part 1 — CLI tool: encrypt all primitives as their string representation

In [Program.cs:262-295](../../src/Voyager.Configuration.Tool/Program.cs#L262-L295), `EncryptJsonNode` now extracts a string representation for every `JsonValue` primitive before encrypting:

| JSON kind | Encrypted as |
|-----------|--------------|
| `String`  | the string itself |
| `True`    | `"True"` (capitalized, matching `JsonConfigurationFileParser`) |
| `False`   | `"False"` |
| `Number`  | raw JSON text (preserves `77`, `77.0`, `1e5` exactly) |
| `Null`    | left as JSON `null` (not encrypted) |

The result is always a JSON string containing Base64 ciphertext, so the file round-trips cleanly through the runtime decryptor.

### Part 2 — Runtime provider: skip decryption for non-string tokens

In `EncryptedJsonConfigurationProvider` (commit `338a16b`), the reader now inspects the `JsonTokenType` and only attempts decryption when it sees `String`. Numbers, booleans, and `null` pass through untouched — matching what `JsonConfigurationFileParser` would have done with the equivalent plaintext file.

This is **defense in depth**: even if a user hand-edits an encrypted config and leaves a number raw, the runtime will no longer throw `FormatException`. It also supports mixed files produced by older versions of the CLI.

### Alternatives considered

1. **Force users to quote every non-string value before encryption.** Rejected: brittle, bad UX, and silently changes the file's semantics.
2. **Preserve original JSON types through the round-trip by encoding the type in the ciphertext.** Rejected: adds a custom wire format on top of the ciphertext, incompatible with the plan to deprecate built-in encryption in favor of SOPS ([ADR-003](ADR-003-encryption-delegation-to-external-tools.md)). Not worth the complexity for a deprecated code path.
3. **Fix only the runtime provider (tolerate raw numbers in encrypted files).** Rejected on its own: leaks plaintext numeric/boolean values — `IdTemplate: 77` in an "encrypted" secrets file is information disclosure. The CLI fix is required for the security guarantee; the runtime fix is required for compatibility.

## Consequences

### Positive

- **Bug fixed end-to-end.** Issues [#4](https://github.com/Voyager-Poland/Voyager.Configuration.MountPath/issues/4) and [#6](https://github.com/Voyager-Poland/Voyager.Configuration.MountPath/issues/6) are resolved. Configs with numeric/boolean values encrypt and decrypt cleanly.
- **No plaintext leakage.** All primitives — including `IdTemplate: 77` and `prawda: true` — are now actually encrypted in the output file.
- **Compatible with legacy files.** The tolerant reader means partially-encrypted files from older tool versions still load.
- **Aligns with ASP.NET semantics.** The chosen string representations (`"True"`, `"False"`, raw number text) match exactly what `JsonConfigurationFileParser` would produce from the plaintext file, so `IConfiguration` binding behaves identically before and after encryption.

### Negative

- **Type information is lost on round-trip.** `vconfig encrypt` then `vconfig decrypt` produces a file where `77` becomes `"77"` and `true` becomes `"True"`. This is acceptable because:
  - `IConfiguration` treats all values as strings anyway; `.Get<T>()` / `Bind()` converts via `TypeConverter`, so `"77"` → `int 77` and `"True"` → `bool true` at binding time.
  - The decrypted file is a **migration artifact**, not a production config. It is intended to be re-encrypted immediately with SOPS (see the "You can now encrypt this file with SOPS" hint printed by `decrypt`).
- **Not suitable for direct `JsonSerializer.Deserialize<T>`.** A decrypted file read with strict `System.Text.Json` into a POCO with `int`/`bool` properties will fail. Mitigation: documented — the decrypted file is only meant to be consumed through `IConfiguration` or re-encrypted by SOPS.

### Neutral

- The deprecation path toward SOPS is unaffected ([ADR-003](ADR-003-encryption-delegation-to-external-tools.md)). SOPS preserves JSON types natively, so this trade-off disappears once users migrate.

### Breaking Changes

None. The change strictly expands what the tool and provider accept; previously-working configs continue to work.

## Implementation Plan

1. ✅ Update `EncryptJsonNode` in [Program.cs](../../src/Voyager.Configuration.Tool/Program.cs) to handle `Number`, `True`, `False`, `Null` (commit `0d59107`).
2. ✅ Update `EncryptedJsonConfigurationProvider` to skip decryption for non-string tokens (commit `338a16b`).
3. ✅ Add regression tests: `config/encoded_with_encrypted_numbers.json` and `config/encoded_with_numbers.json` in `Voyager.Configuration.MountPath.Test`.
4. ✅ Document the round-trip type-loss behavior as a known limitation of the deprecated encryption path.

## References

- [Issue #4 — Problem z szyfrowaniem wartości liczbowych](https://github.com/Voyager-Poland/Voyager.Configuration.MountPath/issues/4)
- [Issue #6 — poprzedni błąd nie został rozwiązany](https://github.com/Voyager-Poland/Voyager.Configuration.MountPath/issues/6)
- Commit `338a16b` — runtime provider tolerance fix
- Commit `0d59107` — CLI tool encryption fix
- [ADR-003: Encryption Delegation to External Tools](ADR-003-encryption-delegation-to-external-tools.md)
- [ADR-004: CLI Tool for Configuration Encryption](ADR-004-cli-tool-for-configuration-encryption.md)

## Related ADRs

- [ADR-003](ADR-003-encryption-delegation-to-external-tools.md) — long-term direction (SOPS) that makes this trade-off temporary
- [ADR-004](ADR-004-cli-tool-for-configuration-encryption.md) — CLI tool that this ADR fixes
