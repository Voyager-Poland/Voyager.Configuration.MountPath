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

**Narzędzie migracji (CLI):**
```bash
# Konwersja pliku z DES na AES
dotnet Voyager.Configuration.Migrate upgrade config.json --old-key "stary" --new-key "nowy"

# Batch migration
dotnet Voyager.Configuration.Migrate upgrade ./config/*.json --old-key "stary" --new-key "nowy"
```

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

**Zadania:**
- [ ] Zaimplementować PBKDF2 lub Argon2 do wyprowadzania klucza
- [ ] Generować losowy IV dla każdej operacji szyfrowania
- [ ] Przechowywać IV razem z zaszyfrowanymi danymi

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
- [ ] Utworzyć `IEncryptor` w `Encryption/IEncryptor.cs`
- [ ] Utworzyć `ISettingsProvider` w `ISettingsProvider.cs`
- [ ] Utworzyć `ICipherProvider` w `Encryption/ICipherProvider.cs`
- [ ] Zrefaktorować istniejące klasy do implementacji interfejsów

### 2.2 Dependency Injection

**Problem:** `EncryptedJsonConfigurationProvider` bezpośrednio tworzy `Encryptor`

**Zadania:**
- [ ] Wstrzykiwać `IEncryptor` przez konstruktor
- [ ] Dodać rejestrację w DI container
- [ ] Umożliwić podmianę implementacji

### 2.3 Implementacja IDisposable

**Problem:** `EncryptedJsonConfigurationProvider` nie zwalnia zasobów

**Zadania:**
- [ ] Zaimplementować `IDisposable` w `EncryptedJsonConfigurationProvider`
- [ ] Zaimplementować `IDisposable` w `Encryptor` (jeśli używa zasobów)
- [ ] Dodać using/dispose w kodzie klienckim

### 2.4 Refaktoryzacja klasy Settings

**Problem:** Brak walidacji, mutowalne właściwości, magic strings

**Zadania:**
- [ ] Dodać walidację w setterach (ścieżki plików, niepuste stringi)
- [ ] Wydzielić stałe do klasy `SettingsDefaults`
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
- [ ] Poprawić nazwy klas testowych
- [ ] Poprawić nazwy metod testowych wg konwencji `[Method]_[Condition]_[Result]`
- [ ] Poprawić nazwy parametrów

### 3.2 Włączenie Nullable Reference Types

**Plik:** `Voyager.Configuration.MountPath.csproj`

**Zadania:**
- [ ] Zmienić `<Nullable>disable</Nullable>` na `<Nullable>enable</Nullable>`
- [ ] Dodać adnotacje `?` gdzie nullable jest zamierzone
- [ ] Dodać null checks gdzie potrzebne
- [ ] Naprawić ostrzeżenia kompilatora

### 3.3 Usunięcie zbędnego kodu

**Zadania:**
- [ ] Usunąć zbędne `Close()` w `CoreEncoder.cs` (using już zamyka)
- [ ] Usunąć nieużywane referencje do editorconfig w projektach testowych
- [ ] Uprościć hierarchię dziedziczenia w testach

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
- [ ] Security Considerations
- [ ] Migration Guide (z DES do AES)
- [ ] API Reference
- [ ] Troubleshooting
- [ ] Performance Tips

### 5.3 Architecture Decision Records (ADRs)

**Zadania:**
- [ ] Utworzyć folder `docs/adr/`
- [ ] ADR-001: Wybór algorytmu szyfrowania
- [ ] ADR-002: Struktura konfiguracji
- [ ] ADR-003: Strategia wersjonowania

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

### 7.1 Spójność parametrów w extension methods

**Problem:** Niespójna kolejność parametrów w `ConfigurationEncryptedExtension.cs`

**Zadania:**
- [ ] Ujednolicić kolejność: `builder, path, key, optional, reloadOnChange`
- [ ] Dodać overloady z sensownymi defaults
- [ ] Deprecate niespójne metody

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
