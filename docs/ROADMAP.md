# Voyager.Configuration.MountPath - Plan Rozwoju

## Podsumowanie

Ten dokument opisuje plan ulepszeń biblioteki Voyager.Configuration.MountPath, mający na celu poprawę jakości kodu, bezpieczeństwa i użyteczności dla programistów.

---

## Faza 1: Bezpieczeństwo (KRYTYCZNE)

### 1.1 Wymiana algorytmu szyfrowania DES na AES-256-GCM

**Problem:** Biblioteka używa przestarzałego algorytmu DES (56-bit), który jest podatny na ataki brute-force.

**Pliki do zmiany:**
- `src/Voyager.Configuration.MountPath/Encryption/CoreEncoder.cs`
- `src/Voyager.Configuration.MountPath/Encryption/Encryptor.cs`

**Zadania:**
- [ ] Utworzyć nową klasę `AesGcmCipherProvider` z AES-256-GCM
- [ ] Dodać interfejs `ICipherProvider` dla abstrakcji algorytmu
- [ ] Zachować `LegacyDesCipherProvider` dla migracji istniejących danych
- [ ] Dodać opcję konfiguracji wyboru algorytmu
- [ ] Utworzyć narzędzie do migracji zaszyfrowanych danych

#### Strategia kompatybilności wstecznej

**UWAGA:** Stare pliki zaszyfrowane DES NIE będą działać z nowym AES bez migracji!

**Opcja A: Auto-detection (zalecana)**
```csharp
public class HybridEncryptor : IEncryptor
{
    // Nowe dane: prefix "AES:" + base64(IV + ciphertext + tag)
    // Stare dane: brak prefixu, próba DES fallback

    public string Decrypt(string ciphertext)
    {
        if (ciphertext.StartsWith("AES:"))
            return DecryptAes(ciphertext[4..]);

        // Fallback dla legacy DES (z ostrzeżeniem w logach)
        _logger.LogWarning("Using deprecated DES decryption. Please migrate.");
        return DecryptLegacyDes(ciphertext);
    }
}
```

**Opcja B: Explicit configuration**
```csharp
builder.AddEncryptedJsonFile("config.json", key, new EncryptionOptions
{
    Algorithm = EncryptionAlgorithm.Aes256Gcm,  // lub .LegacyDes
    AllowLegacyFallback = true  // próbuj DES jeśli AES fails
});
```

**Narzędzie CLI: Voyager.Configuration.Tool**

Nowe narzędzie CLI do szyfrowania, deszyfrowania i migracji plików konfiguracyjnych.

```bash
# Instalacja jako global tool
dotnet tool install -g Voyager.Configuration.Tool

# Lub uruchomienie z projektu
dotnet run --project src/Voyager.Configuration.Tool
```

**Komendy:**

```bash
# === SZYFROWANIE PLIKU ===
# Szyfruje wszystkie wartości w pliku JSON
voyager-config encrypt appsettings.json --key "MojKluczSzyfrowania" --output appsettings.encrypted.json

# Szyfrowanie w miejscu (nadpisuje plik)
voyager-config encrypt appsettings.json --key "MojKluczSzyfrowania" --in-place

# Szyfrowanie z nowym algorytmem AES
voyager-config encrypt appsettings.json --key "MojKlucz" --algorithm aes

# === DESZYFROWANIE PLIKU ===
# Deszyfruje wszystkie wartości w pliku JSON
voyager-config decrypt appsettings.encrypted.json --key "MojKluczSzyfrowania" --output appsettings.json

# Deszyfrowanie w miejscu
voyager-config decrypt appsettings.encrypted.json --key "MojKluczSzyfrowania" --in-place

# === MIGRACJA DES → AES ===
# Konwersja pliku z DES na AES (ten sam klucz)
voyager-config migrate appsettings.json --key "MojKlucz"

# Konwersja z zmianą klucza
voyager-config migrate appsettings.json --old-key "StaryKlucz" --new-key "NowyKlucz"

# Batch migration (wiele plików)
voyager-config migrate ./config/*.json --key "MojKlucz"

# === WERYFIKACJA ===
# Sprawdza czy plik jest poprawnie zaszyfrowany
voyager-config verify appsettings.encrypted.json --key "MojKlucz"

# === INFORMACJE ===
# Pokazuje algorytm użyty w pliku (DES/AES)
voyager-config info appsettings.encrypted.json
```

**Opcje wspólne:**
```
--key, -k         Klucz szyfrowania (wymagany)
--output, -o      Plik wyjściowy (domyślnie: stdout lub nowy plik)
--in-place, -i    Nadpisz plik wejściowy
--algorithm, -a   Algorytm: aes (domyślny), des (legacy)
--verbose, -v     Szczegółowe logowanie
--dry-run         Pokaż co zostanie zrobione bez wykonania
```

**Przykładowy workflow:**
```bash
# 1. Masz plik z danymi niezaszyfrowanymi
cat appsettings.json
# { "ConnectionString": "Server=localhost;Password=secret" }

# 2. Zaszyfrujesz go
voyager-config encrypt appsettings.json -k "MojSuperTajnyKlucz123" -o appsettings.encrypted.json

# 3. Wynik
cat appsettings.encrypted.json
# { "ConnectionString": "AES:SGVsbG8gV29ybGQh..." }

# 4. Aplikacja używa zaszyfrowanego pliku
# builder.AddEncryptedJsonFile("appsettings.encrypted.json", "MojSuperTajnyKlucz123")

# 5. W razie potrzeby możesz odszyfrować
voyager-config decrypt appsettings.encrypted.json -k "MojSuperTajnyKlucz123"
# { "ConnectionString": "Server=localhost;Password=secret" }
```

**Struktura projektu:**
```
src/
  Voyager.Configuration.Tool/
    Program.cs
    Commands/
      EncryptCommand.cs
      DecryptCommand.cs
      MigrateCommand.cs
      VerifyCommand.cs
      InfoCommand.cs
    Voyager.Configuration.Tool.csproj
```

**Zadania:**
- [ ] Utworzyć projekt `Voyager.Configuration.Tool` jako .NET global tool
- [ ] Zaimplementować `EncryptCommand` - szyfrowanie pliku
- [ ] Zaimplementować `DecryptCommand` - deszyfrowanie pliku
- [ ] Zaimplementować `MigrateCommand` - migracja DES → AES
- [ ] Zaimplementować `VerifyCommand` - weryfikacja poprawności
- [ ] Zaimplementować `InfoCommand` - informacje o pliku
- [ ] Obsługa batch processing (wildcards)
- [ ] Obsługa stdin/stdout dla pipeline'ów
- [ ] Publikacja jako `dotnet tool` na NuGet

**Plan migracji dla użytkowników:**
1. Upgrade do v2.0 z `AllowLegacyFallback = true`
2. Uruchom narzędzie migracji na wszystkich plikach
3. Przetestuj aplikację
4. Wyłącz `AllowLegacyFallback` w v2.1+
5. W v3.0 usuń wsparcie DES całkowicie

### 1.2 Usunięcie domyślnego klucza szyfrowania

**Problem:** Hardcoded klucz `"DEFAULT123456789011"` w `EncryptedJsonConfigurationSource.cs:10`

**Zadania:**
- [ ] Usunąć domyślną wartość klucza
- [ ] Wymagać jawnego podania klucza (throw exception jeśli brak)
- [ ] Dodać walidację minimalnej długości klucza (min. 32 znaki dla AES-256)
- [ ] Dodać walidację entropii klucza

### 1.3 Implementacja funkcji wyprowadzania klucza (KDF)

**Problem:** Bezpośrednie użycie stringa jako klucza (`Encryptor.cs:12-13`)
- Obecny kod: `key.Substring(0, 8)` → tylko 8 bajtów dla DES
- Stare klucze mogą mieć 16-26 znaków (nie 32)

**Rozwiązanie: PBKDF2 do rozciągnięcia dowolnego klucza**

```csharp
public class KeyDerivation
{
    private const int Iterations = 100_000;
    private static readonly byte[] Salt = Encoding.UTF8.GetBytes("Voyager.Config.Salt.V2");

    public static (byte[] Key, byte[] BaseIV) DeriveKey(string password)
    {
        // Dowolny klucz (nawet 8 znaków) → 32 bajty dla AES-256
        using var pbkdf2 = new Rfc2898DeriveBytes(
            password,
            Salt,
            Iterations,
            HashAlgorithmName.SHA256);

        byte[] key = pbkdf2.GetBytes(32);    // AES-256 key
        byte[] baseIv = pbkdf2.GetBytes(12); // Base IV for counter mode
        return (key, baseIv);
    }
}
```

**Kompatybilność ze starymi kluczami:**
- Klucz "PowaznyTestks123456722228" (26 znaków) → PBKDF2 → 32 bajty ✓
- Klucz "ShortKey123" (11 znaków) → PBKDF2 → 32 bajty ✓
- Użytkownicy NIE muszą zmieniać swoich kluczy!

**Zadania:**
- [ ] Zaimplementować PBKDF2 z SHA-256
- [ ] Stały salt w kodzie (lub konfigurowalny)
- [ ] 100,000+ iteracji dla bezpieczeństwa
- [ ] Generować losowy IV dla każdej wartości
- [ ] Przechowywać IV w zaszyfrowanej wartości

### 1.4 Format zaszyfrowanych wartości (IV w wartości)

**Problem:** Gdzie zapisać IV skoro każda wartość JSON jest osobno szyfrowana?

**Obecny format:**
```json
{
  "ConnectionString": "base64(DES_ciphertext)",
  "ApiKey": "base64(DES_ciphertext)"
}
```

**Nowy format (IV embedded in value):**
```json
{
  "ConnectionString": "AES:base64(random_IV[12] + ciphertext + auth_tag[16])",
  "ApiKey": "AES:base64(random_IV[12] + ciphertext + auth_tag[16])"
}
```

**Struktura zaszyfrowanej wartości:**
```
┌─────────┬────────────────────┬──────────────┐
│ IV (12B)│ Ciphertext (N bytes)│ Auth Tag (16B)│
└─────────┴────────────────────┴──────────────┘
         ↓ Base64 encode ↓
"AES:SGVsbG8gV29ybGQhIQ=="
```

**Implementacja:**
```csharp
public string Encrypt(string plaintext)
{
    byte[] iv = RandomNumberGenerator.GetBytes(12);  // Losowy IV dla każdej wartości!
    byte[] plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
    byte[] ciphertext = new byte[plaintextBytes.Length];
    byte[] tag = new byte[16];

    using var aes = new AesGcm(_derivedKey, 16);
    aes.Encrypt(iv, plaintextBytes, ciphertext, tag);

    // IV + ciphertext + tag → Base64
    byte[] result = new byte[iv.Length + ciphertext.Length + tag.Length];
    iv.CopyTo(result, 0);
    ciphertext.CopyTo(result, iv.Length);
    tag.CopyTo(result, iv.Length + ciphertext.Length);

    return "AES:" + Convert.ToBase64String(result);
}

public string Decrypt(string encrypted)
{
    if (encrypted.StartsWith("AES:"))
    {
        byte[] data = Convert.FromBase64String(encrypted[4..]);
        byte[] iv = data[..12];
        byte[] ciphertext = data[12..^16];
        byte[] tag = data[^16..];

        byte[] plaintext = new byte[ciphertext.Length];
        using var aes = new AesGcm(_derivedKey, 16);
        aes.Decrypt(iv, ciphertext, tag, plaintext);

        return Encoding.UTF8.GetString(plaintext);
    }

    // Legacy DES fallback
    return DecryptLegacyDes(encrypted);
}
```

**Dlaczego losowy IV dla każdej wartości?**
- Ten sam plaintext + ten sam klucz = inny ciphertext (bezpieczne)
- Unikamy ataków "same plaintext detection"
- IV nie musi być tajny, tylko unikalny

### 1.4 Dodanie uwierzytelniania szyfrowania

**Problem:** Brak weryfikacji integralności (HMAC) - dane mogą być zmodyfikowane

**Zadania:**
- [ ] Użyć AES-GCM (ma wbudowane uwierzytelnianie) lub
- [ ] Dodać HMAC-SHA256 do walidacji integralności

---

## Faza 2: Architektura i SOLID (WYSOKIE)

### 2.1 Utworzenie interfejsów

**Problem:** Brak interfejsów utrudnia testowanie i narusza DIP

**Nowe interfejsy:**
```csharp
public interface IEncryptor
{
    string Encrypt(string plaintext);
    string Decrypt(string ciphertext);
}

public interface ISettingsProvider
{
    Settings GetSettings(string filename = "appsettings");
}

public interface ICipherProvider
{
    byte[] Encrypt(byte[] data, byte[] key);
    byte[] Decrypt(byte[] data, byte[] key);
}
```

**Zadania:**
- [x] Utworzyć `IEncryptor` w `Encryption/IEncryptor.cs`
- [x] Utworzyć `ISettingsProvider` w `ISettingsProvider.cs`
- [x] Utworzyć `ICipherProvider` w `Encryption/ICipherProvider.cs`
- [x] Zrefaktorować istniejące klasy do implementacji interfejsów

### 2.2 Dependency Injection

**Problem:** `EncryptedJsonConfigurationProvider` bezpośrednio tworzy `Encryptor`

**Zadania:**
- [x] Wstrzykiwać `IEncryptor` przez konstruktor (via IEncryptorFactory)
- [x] Dodać rejestrację w DI container (ServiceCollectionExtensions)
- [x] Umożliwić podmianę implementacji

### 2.3 Implementacja IDisposable

**Problem:** `EncryptedJsonConfigurationProvider` nie zwalnia zasobów

**Zadania:**
- [x] Zaimplementować `IDisposable` w `EncryptedJsonConfigurationProvider`
- [ ] Zaimplementować `IDisposable` w `Encryptor` (jeśli używa zasobów)
- [ ] Dodać using/dispose w kodzie klienckim

### 2.4 Refaktoryzacja klasy Settings

**Problem:** Brak walidacji, mutowalne właściwości, magic strings

**Zadania:**
- [ ] Dodać walidację w setterach (ścieżki plików, niepuste stringi)
- [x] Wydzielić stałe do klasy `SettingsDefaults`
- [ ] Rozważyć Builder Pattern dla konstrukcji
- [ ] Dodać `init` settery dla niemutowalności po konstrukcji

---

## Faza 3: Jakość kodu (ŚREDNIE)

### 3.1 Poprawki nazewnictwa

**Typy z błędami:**
| Obecna nazwa | Poprawna nazwa |
|--------------|----------------|
| `ForceSpecyficConfiguration` | `ForceSpecificConfiguration` |
| `EncodedConnectioString` | `EncodedConnectionString` |
| `DecoreEncode` (test) | `EncryptAndDecrypt_WithValidText_ReturnsOriginalText` |
| `dataParamtxt` | `encryptedData` |
| `CoreEncoder` | `LegacyDesCipherProvider` |

**Zadania:**
- [x] Poprawić nazwy klas testowych (ForceSpecyficConfiguration → ForceSpecificConfiguration, EncodedConnectioString → EncodedConnectionString)
- [x] Poprawić nazwy metod testowych wg konwencji `[Method]_[Condition]_[Result]`
- [x] Poprawić nazwy parametrów
- [x] Zmienić nazwę CoreEncoder → LegacyDesCipherProvider

### 3.2 Włączenie Nullable Reference Types

**Plik:** `Voyager.Configuration.MountPath.csproj`

**Zadania:**
- [ ] Zmienić `<Nullable>disable</Nullable>` na `<Nullable>enable</Nullable>`
- [ ] Dodać adnotacje `?` gdzie nullable jest zamierzone
- [ ] Dodać null checks gdzie potrzebne
- [ ] Naprawić ostrzeżenia kompilatora

### 3.3 Usunięcie zbędnego kodu

**Zadania:**
- [x] Usunąć zbędne `Close()` w `CoreEncoder.cs` (using już zamyka) - renamed to LegacyDesCipherProvider with proper using statements
- [x] Usunąć nieużywane referencje do editorconfig w projektach testowych
- [x] Uprościć hierarchię dziedziczenia w testach (5 levels → 1 level with ConfigurationTestBase)

### 3.4 Spójność przestrzeni nazw

**Problem:** Używanie `Microsoft.Extensions.DependencyInjection` jako namespace

**Zadania:**
- [ ] Przenieść do `Voyager.Configuration.MountPath.Extensions`
- [ ] Lub dodać komentarz wyjaśniający konwencję extension methods

---

## Faza 4: Funkcjonalność (ŚREDNIE)

### 4.1 Obsługa zagnieżdżonych struktur JSON

**Problem:** Szyfrowanie działa tylko dla płaskich kluczy

**Zadania:**
- [ ] Zaimplementować rekurencyjne odszyfrowywanie
- [ ] Dodać opcję selektywnego szyfrowania (tylko określone ścieżki)
- [ ] Obsłużyć tablice w JSON

### 4.2 Selektywne szyfrowanie

**Zadania:**
- [ ] Dodać atrybut/konwencję do oznaczania pól do szyfrowania
- [ ] Np. klucze kończące się na `:encrypted` lub prefix `ENC:`
- [ ] Obsługa mieszanych plików (część plain, część encrypted)

### 4.3 Wsparcie dla async

**Zadania:**
- [ ] Dodać async overloady dla extension methods
- [ ] `AddEncryptedJsonFileAsync`
- [ ] Async loading w provider

### 4.4 Lepsza obsługa błędów

**Zadania:**
- [ ] Utworzyć własne typy wyjątków (`EncryptionException`, `ConfigurationException`)
- [ ] Dodać walidację we wszystkich publicznych metodach
- [ ] Lepsze komunikaty błędów z kontekstem

---

## Faza 5: Dokumentacja (ŚREDNIE)

### 5.1 XML Documentation

**Zadania:**
- [ ] Dodać `///` komentarze do wszystkich publicznych typów i metod
- [ ] Usunąć `<NoWarn>1591</NoWarn>` z Build.CodeQuality.props
- [ ] Generować dokumentację API

### 5.2 Rozbudowa README

**Sekcje do dodania:**
- [x] Security Considerations - added encryption best practices and key management
- [x] Quick Start - added basic configuration examples
- [x] Encryption documentation - added encrypted configuration examples
- [x] DI examples - added dependency injection registration examples
- [x] Docker/Kubernetes examples - added container deployment examples
- [x] Fix spelling and grammar errors (metod→method, worsk→works, thest→test, etc.)
- [ ] Migration Guide (z DES do AES) - to be added in future version
- [ ] API Reference
- [ ] Troubleshooting
- [ ] Performance Tips

### 5.3 Architecture Decision Records (ADRs)

**Zadania:**
- [x] Utworzyć folder `docs/adr/`
- [x] ADR-001: Organization of Configuration Extension Methods (SRP, namespace conventions)
- [ ] ADR-002: Wybór algorytmu szyfrowania (DES → AES-256-GCM)
- [ ] ADR-003: Struktura konfiguracji (mount paths, file naming)
- [ ] ADR-004: Strategia wersjonowania (SemVer, breaking changes)

### 5.4 Przykłady użycia

**Zadania:**
- [ ] Dodać folder `samples/`
- [ ] Przykład podstawowy
- [ ] Przykład z Kubernetes/Docker
- [ ] Przykład migracji konfiguracji

---

## Faza 6: Testy (ŚREDNIE)

### 6.1 Rozszerzenie pokrycia testami

**Brakujące testy:**
- [ ] SettingsProvider - edge cases (null, empty paths)
- [ ] Configuration merge/override scenarios
- [ ] Error cases (missing files, corrupted JSON, invalid encryption)
- [ ] Key validation
- [ ] Base64 decoding errors
- [ ] Extension methods null checks

### 6.2 Testy negatywne

**Zadania:**
- [ ] Test dla uszkodzonych zaszyfrowanych danych
- [ ] Test dla nieprawidłowych kluczy
- [ ] Test dla brakujących plików
- [ ] Test dla nieprawidłowego JSON

### 6.3 Testy integracyjne

**Zadania:**
- [ ] Test pełnego flow: encrypt file → load config → read values
- [ ] Test z różnymi środowiskami (Development, Production)
- [ ] Test reload on change

### 6.4 Benchmarki

**Zadania:**
- [ ] Dodać BenchmarkDotNet
- [ ] Benchmark szyfrowania/odszyfrowywania
- [ ] Benchmark ładowania konfiguracji

---

## Faza 7: API Usability (NISKIE)

### 7.1 Refaktoryzacja extension methods (SRP + API Consistency)

**Problem 1: Naruszenie Single Responsibility Principle**
Plik `ConfigurationEncryptedExtension.cs` ma dwie odpowiedzialności:
- `AddEncryptedMountConfiguration` - wysokopoziomowe API (montowanie + szyfrowanie)
- `AddEncryptedJsonFile` - niskopoziomowe API (pliki JSON + szyfrowanie)

**Problem 2: Niespójna kolejność parametrów**

**Zadania:**
- [ ] Podzielić według SRP na osobne pliki:
  - `ConfigurationExtension.cs` (już OK - tylko AddMountConfiguration)
  - `EncryptedMountConfigurationExtensions.cs` (AddEncryptedMountConfiguration)
  - `EncryptedJsonFileExtensions.cs` (AddEncryptedJsonFile)
- [ ] Ujednolicić kolejność parametrów: `builder, path, key, optional, reloadOnChange`
- [ ] Dodać overloady z sensownymi defaults
- [ ] Deprecate niespójne metody przed usunięciem w przyszłej wersji

### 7.2 Builder Pattern dla Settings

```csharp
var settings = Settings.Builder()
    .WithFileName("appsettings")
    .WithEnvironment("Production")
    .WithEncryptionKey(key)
    .RequireFile()
    .Build();
```

### 7.3 Fluent API

**Zadania:**
- [ ] Upewnić się, że wszystkie metody zwracają `IConfigurationBuilder`
- [ ] Naprawić bug w loop w `AddEncryptedMountConfiguration`

---

## Priorytety implementacji

| Faza | Priorytet | Estymowany nakład |
|------|-----------|-------------------|
| 1. Bezpieczeństwo | KRYTYCZNY | Duży |
| 2. Architektura SOLID | WYSOKI | Średni |
| 3. Jakość kodu | ŚREDNI | Mały |
| 4. Funkcjonalność | ŚREDNI | Średni |
| 5. Dokumentacja | ŚREDNI | Mały |
| 6. Testy | ŚREDNI | Średni |
| 7. API Usability | NISKI | Mały |

---

## Metryki sukcesu

- [ ] 0 krytycznych problemów bezpieczeństwa
- [ ] 100% publicznych API z dokumentacją XML
- [ ] >80% pokrycia testami
- [ ] Wszystkie testy przechodzą na CI
- [ ] Nullable reference types włączone
- [ ] Brak ostrzeżeń kompilatora (poza celowo ignorowanymi)

---

## Wersjonowanie zmian

| Wersja | Zakres zmian |
|--------|--------------|
| 2.0.0 | Faza 1 (breaking: nowy algorytm szyfrowania) |
| 2.1.0 | Faza 2 (interfejsy, DI) |
| 2.2.0 | Faza 4 (nowe funkcje) |
| 2.x.x | Fazy 3, 5, 6, 7 (nie-breaking) |

---

*Dokument utworzony: 2026-02-05*
*Ostatnia aktualizacja: 2026-02-05*
