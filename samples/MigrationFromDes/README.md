# Migracja z DES na AES-256-GCM

## Szybki start

```bash
# 1. Zainstaluj narzędzie CLI
dotnet tool install -g Voyager.Configuration.Tool --prerelease

# 2. Wygeneruj nowy klucz AES-256
export ASPNETCORE_AES_KEY=$(vconfig keygen)

# 3. Zmigruj plik konfiguracyjny
vconfig reencrypt \
  --input config/secrets.json \
  --legacy-key-env ASPNETCORE_ENCODEKEY \
  --new-key-env ASPNETCORE_AES_KEY

# 4. Podmień klucz w deploymencie
#    (ustaw ASPNETCORE_ENCODEKEY na nowy klucz AES)

# 5. Usuń stary klucz DES z secret managera
```

## Co się zmienia?

| Aspekt | Przed (DES) | Po (AES-256-GCM) |
|--------|-------------|-------------------|
| Algorytm | DES-CBC (56-bit) | AES-256-GCM |
| Autentykacja | Brak — zły klucz = ciche śmieci | Tag 16B — zły klucz = wyjątek |
| Format | Base64 bez prefiksu | `v2:BASE64(nonce\|\|ct\|\|tag)` |
| Klucz | Dowolny string >= 8 znaków | 32 losowe bajty w Base64 |
| net48 | Natywny DES BCL | BouncyCastle polyfill |

## Kod aplikacji — bez zmian

Migracja jest transparentna. `AddEncryptedMountConfiguration` automatycznie rozpoznaje format `v2:` i deszyfruje wartości AES-GCM:

```csharp
builder.ConfigureAppConfiguration((context, config) =>
{
    var provider = context.HostingEnvironment.GetSettingsProvider();
    var encryptionKey = Environment.GetEnvironmentVariable("ASPNETCORE_ENCODEKEY");

    config.AddEncryptedMountConfiguration(encryptionKey, provider, "secrets");
});
```

## Weryfikacja

```bash
# Sprawdź bez zapisywania (dry-run)
vconfig reencrypt \
  --input config/secrets.json \
  --legacy-key-env ASPNETCORE_ENCODEKEY \
  --new-key-env ASPNETCORE_AES_KEY \
  --dry-run
# -> "Dry run: 5 value(s) would be migrated, 0 already AES, 5 total."
```

## Plan usuwania legacy DES

- **v2.x** (obecna): `AllowLegacyDes=true` — DES odczyt działa, zapis zawsze AES
- **v3.x**: `AllowLegacyDes=false` — DES wyłączony domyślnie
- **v4.x**: Kod DES usunięty całkowicie

## Alternatywa: SOPS

Dla organizacji wymagających KMS, rotacji kluczy lub audit logu, [SOPS](https://github.com/mozilla/sops) pozostaje wspieraną opcją przez extension point `IEncryptor`. Zobacz [ADR-003](../../docs/adr/ADR-003-encryption-delegation-to-external-tools.md).

## Dokumentacja

- [ADR-010: AES-256-GCM](../../docs/adr/ADR-010-aes-gcm-with-versioned-ciphertext.md) — pełna decyzja architektoniczna
- [MIGRATION.md](../../docs/MIGRATION.md) — przewodnik migracji
