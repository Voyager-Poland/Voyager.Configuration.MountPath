# Voyager.Configuration.MountPath - Plan Rozwoju

## Podsumowanie

Ten dokument opisuje plan ulepszeń biblioteki Voyager.Configuration.MountPath, mający na celu poprawę jakości kodu i użyteczności dla programistów.

> **⚠️ WAŻNA ZMIANA STRATEGII**
> Zgodnie z [ADR-003](adr/ADR-003-encryption-delegation-to-external-tools.md), wbudowane szyfrowanie jest **deprecjonowane** i zostanie usunięte w wersji 3.0.
> Zalecamy migrację do zewnętrznych narzędzi (SOPS, Kubernetes Secrets, Azure Key Vault).

---

## Faza 1: Deprecacja Szyfrowania i Migracja (v2.0) ✅ ZAKOŃCZONA

### 1.1 ✅ ADR-003: Decyzja o delegacji szyfrowania

**Status:** ✅ Zakończone

**Decyzja:** Deprecacja wbudowanego szyfrowania DES na rzecz zewnętrznych rozwiązań.

**Rekomendowane alternatywy:**
1. **Mozilla SOPS** - szyfrowanie plików dla GitOps
2. **Kubernetes Secrets + Sealed Secrets** - natywne k8s secrets
3. **Cloud Secret Managers** - Azure Key Vault, AWS Secrets Manager, GCP Secret Manager
4. **dotnet user-secrets** - dla developmentu

**Dlaczego NIE rozwi

jamy własnego szyfrowania:**
- ❌ DES jest przestarzały i niebezpieczny (56-bit)
- ❌ Migracja do AES wymaga breaking changes i migracji danych
- ❌ Zarządzanie kluczami to skomplikowany problem
- ❌ Zewnętrzne narzędzia są lepsze, bezpieczniejsze i bardziej uniwersalne
- ✅ SOPS oferuje lepsze bezpieczeństwo (AES-256-GCM)
- ✅ Integracja z cloud KMS (AWS, Azure, GCP)
- ✅ Separation of concerns: konfiguracja ≠ secret management

**Zobacz:** [ADR-003: Encryption Delegation to External Tools](adr/ADR-003-encryption-delegation-to-external-tools.md)

### 1.2 ✅ ADR-004: CLI Tool dla Migracji

**Status:** ✅ Zakończone

**Zaimplementowano:** `vconfig` - narzędzie CLI do migracji

**Package:** `Voyager.Configuration.Tool` (dotnet global tool)
**Command:** `vconfig`
**Wersja:** v1.3.0-preview.4

**Komendy:**
```bash
# Instalacja
dotnet tool install -g Voyager.Configuration.Tool --prerelease

# Deszyfrowanie dla migracji do SOPS
vconfig decrypt --input config.json --output config.plain.json

# Szyfrowanie (backward compatibility)
vconfig encrypt --input config.json --in-place

# Single value operations
vconfig encrypt-value "text to encrypt"
vconfig decrypt-value "encrypted_text"
```

**Cel narzędzia:** Migration helper, NIE nowy system szyfrowania!
- ✅ Deszyfruj stare pliki zaszyfrowane DES
- ✅ Przygotuj do migracji na SOPS
- ✅ Backward compatibility z `Voyager.Configuration.Encrypt/Decrypt`
- ⚠️ Deprecation warnings podczas użycia

**Zobacz:** [ADR-004: CLI Tool for Configuration Encryption](adr/ADR-004-cli-tool-for-configuration-encryption.md)

### 1.3 ✅ Deprecation Notices

**Status:** ✅ Zakończone

- ✅ README zaktualizowany z ostrzeżeniami o deprecacji
- ✅ Security Considerations - rekomendacje SOPS
- ✅ Wszystkie extension methods dla encryption oznaczone jako deprecated w dokumentacji
- ✅ CLI tool pokazuje deprecation warning

### 1.4 ✅ Dokumentacja Migracji

**Status:** ✅ Zakończone

- ✅ ADR-003 zawiera szczegółowe instrukcje migracji
- ✅ Przykłady dla Kubernetes
- ✅ Przykłady dla Supervisor (używany przez adminów)
- ✅ SOPS setup guide
- ✅ Workflow migracji krok po kroku

---

## Faza 2: Architektura i SOLID ✅ ZAKOŃCZONA

### 2.1 ✅ Utworzenie interfejsów

**Status:** ✅ Zakończone

- ✅ `IEncryptor` w `Encryption/IEncryptor.cs`
- ✅ `ISettingsProvider` w `ISettingsProvider.cs`
- ✅ `ICipherProvider` w `Encryption/ICipherProvider.cs`
- ✅ `IEncryptorFactory` dla dependency injection

### 2.2 ✅ Dependency Injection

**Status:** ✅ Zakończone

- ✅ Wstrzykiwanie `IEncryptor` przez `IEncryptorFactory`
- ✅ Rejestracja w DI container (`ServiceCollectionExtensions`)
- ✅ Możliwość podmiany implementacji

### 2.3 ✅ Implementacja IDisposable

**Status:** ✅ Zakończone

- ✅ `IDisposable` w `EncryptedJsonConfigurationProvider`
- ✅ Prawidłowe zarządzanie zasobami

### 2.4 ✅ Refaktoryzacja klasy Settings

**Status:** ✅ Zakończone

- ✅ Walidacja w setterach
- ✅ Klasa `SettingsDefaults` dla stałych
- ✅ ADR-002: Action<Settings> zamiast Builder Pattern
- ✅ Settings jako record z value equality
- ✅ IsExternalInit polyfill dla .NET Framework

---

## Faza 3: Jakość Kodu ✅ WIĘKSZOŚĆ ZAKOŃCZONA

### 3.1 ✅ Poprawki nazewnictwa

**Status:** ✅ Zakończone

- ✅ ForceSpecyficConfiguration → ForceSpecificConfiguration
- ✅ EncodedConnectioString → EncodedConnectionString
- ✅ CoreEncoder → LegacyDesCipherProvider
- ✅ Konwencje nazw testów: `[Method]_[Condition]_[Result]`

### 3.2 ✅ Nullable Reference Types

**Status:** ✅ Zakończone

- ✅ `<Nullable>enable</Nullable>`
- ✅ Adnotacje `?` gdzie nullable
- ✅ Null checks
- ✅ 0 ostrzeżeń kompilatora

### 3.3 ✅ Usunięcie zbędnego kodu

**Status:** ✅ Zakończone

- ✅ Usunięto zbędne `Close()` w LegacyDesCipherProvider
- ✅ Usunięto nieużywane referencje do editorconfig
- ✅ Uproszczono hierarchię dziedziczenia w testach (5 levels → 1 level)

### 3.4 ⏸️ Spójność przestrzeni nazw

**Status:** ⏸️ Odroczone (ADR-001: pozostawiono Microsoft.Extensions.DependencyInjection)

- ℹ️ ADR-001 uzasadnia użycie `Microsoft.Extensions.DependencyInjection` jako namespace dla extension methods (konwencja .NET)

---

## Faza 4: Jakość Kodu - Pozostałe (NISKI PRIORYTET)

### 4.1 XML Documentation

**Status:** ✅ Większość zakończona

- ✅ `///` komentarze w większości publicznych typów
- ✅ Usunięto `<NoWarn>1591</NoWarn>`
- ⏸️ Generowanie dokumentacji API (DocFX) - opcjonalne

### 4.2 Architecture Decision Records

**Status:** ✅ Zakończone

- ✅ ADR-001: Organization of Configuration Extension Methods
- ✅ ADR-002: Settings Builder Pattern Decision
- ✅ ADR-003: Encryption Delegation to External Tools
- ✅ ADR-004: CLI Tool for Configuration Encryption
- ✅ ADR-005: Async Configuration Loading (Rejected)
- ✅ ADR-006: Custom Exception Types and Error Handling (Accepted)

### 4.3 Refaktoryzacja Extension Methods (SRP)

**Status:** ✅ Zakończone

- ✅ Podział według SRP:
  - `ConfigurationExtension.cs` - AddMountConfiguration
  - `EncryptedMountConfigurationExtensions.cs` - AddEncryptedMountConfiguration
  - `EncryptedJsonFileExtensions.cs` - AddEncryptedJsonFile
- ✅ Ujednolicono kolejność parametrów
- ✅ ConfigurationEncryptedExtension oznaczone jako `[Obsolete]`

---

## Faza 5: Testy (ŚREDNI PRIORYTET)

### 5.1 Rozszerzenie pokrycia testami

**Brakujące testy:**
- [ ] SettingsProvider - edge cases (null, empty paths)
- [ ] Configuration merge/override scenarios
- [ ] Error cases (missing files, corrupted JSON)
- [ ] Key validation edge cases
- [ ] Extension methods null checks

### 5.2 Testy negatywne

**Zadania:**
- [ ] Test dla uszkodzonych zaszyfrowanych danych
- [ ] Test dla nieprawidłowych kluczy
- [ ] Test dla brakujących plików
- [ ] Test dla nieprawidłowego JSON

### 5.3 Testy integracyjne

**Zadania:**
- [ ] Test pełnego flow: encrypt file → load config → read values
- [ ] Test z różnymi środowiskami (Development, Production)
- [ ] Test reload on change

### 5.4 Benchmarki

**Zadania:**
- [ ] Dodać BenchmarkDotNet
- [ ] Benchmark szyfrowania/odszyfrowywania (dla legacy compatibility)
- [ ] Benchmark ładowania konfiguracji

---

## Faza 6: Funkcjonalność - Ogólna (NISKI PRIORYTET)

### 6.1 ✅ Lepsza obsługa błędów

**Status:** ✅ Zakończone (ADR)

**Decyzja:** ADR-006 zaakceptował implementację custom exceptions z ograniczonym zakresem.

**Implementacja (zaakceptowana przez ADR-006):**
- [ ] Utworzyć 2 typy wyjątków:
  - `ConfigurationException` - dla błędów ładowania konfiguracji
  - `EncryptionException` - dla błędów szyfrowania/deszyfrowania
- [ ] Opakowywać framework exceptions z kontekstem (MountPath, FileName)
- [ ] Walidacja w setterach klasy `Settings`
- [ ] Null checks w extension methods (`ArgumentNullException.ThrowIfNull`)
- [ ] Lepsze komunikaty błędów zawierające: filename, mount path, kontekst operacji

**Czego NIE robimy:**
- ❌ Nadmierna walidacja w każdej metodzie (over-engineering)
- ❌ Złożona hierarchia wyjątków (max 2 typy)
- ❌ Custom exceptions dla błędów programistycznych (użyj ArgumentException)

**Zobacz:** [ADR-006: Custom Exception Types and Error Handling](adr/ADR-006-custom-exception-types-and-error-handling.md)

### 6.2 ❌ Wsparcie dla async (ODRZUCONE)

**Status:** ❌ ODRZUCONE przez ADR-005

**Decyzja:** Nie implementujemy async configuration loading.

**Dlaczego odrzucono:**
- ASP.NET Core `IConfigurationProvider.Load()` jest synchroniczne z założenia
- Niemożliwe stworzenie prawdziwego async - byłby to fake async (blokowanie przebranne za async)
- Główny use case (lokalne volume mounts) nie wymaga async
- Startup aplikacji jest synchroniczny - blokowanie jest oczekiwane
- Dodaje kompleksowość bez rzeczywistych korzyści

**Zobacz:** [ADR-005: Async Configuration Loading](adr/ADR-005-async-configuration-loading.md)

~~Zadania (anulowane):~~
- ~~[ ] Dodać async overloads dla extension methods~~
- ~~[ ] `AddMountConfigurationAsync`~~
- ~~[ ] Async loading w provider~~

### 6.3 Przykłady użycia

**Zadania:**
- [ ] Dodać folder `samples/`
- [ ] Przykład podstawowy (mount configuration)
- [ ] Przykład z Kubernetes/Docker
- [ ] Przykład migracji z encryption do SOPS

---

## Priorytety Implementacji

| Faza | Priorytet | Status |
|------|-----------|--------|
| 1. Deprecacja i Migracja | ✅ KRYTYCZNY | ✅ Zakończone |
| 2. Architektura SOLID | ✅ WYSOKI | ✅ Zakończone |
| 3. Jakość kodu | ✅ ŚREDNI | ✅ Zakończone |
| 4. Jakość kodu - Pozostałe | 🟡 NISKI | Częściowo |
| 5. Testy | 🟡 ŚREDNI | Do zrobienia |
| 6. Funkcjonalność Ogólna | 🟡 NISKI | Do zrobienia |

---

## Wersjonowanie

| Wersja | Zakres zmian | Status |
|--------|--------------|--------|
| **v1.3.0-preview.4** | CLI tool `vconfig` (preview) | ✅ Zakończone |
| **v2.0.0** | Deprecation notices, CLI tool stable, SOLID refactoring | 🔄 W trakcie |
| **v2.x.x** | Bug fixes, tests, examples | 📋 Planowane |
| **v3.0.0** | **REMOVE encryption entirely** | 📋 Przyszłość |

---

## Metryki Sukcesu

### Wersja 2.0
- ✅ CLI tool `vconfig` dostępny na NuGet
- ✅ Deprecation warnings w README i dokumentacji
- ✅ ADR-003 i ADR-004 dokumentują decyzje
- ✅ Migracja do SOPS udokumentowana
- ✅ SOLID principles zastosowane
- ✅ Nullable reference types włączone
- ✅ 0 ostrzeżeń kompilatora

### Wersja 3.0 (Przyszłość)
- [ ] Całkowite usunięcie encryption
- [ ] Usunięcie deprecated extension methods
- [ ] Uproszczony kod - tylko mount path configuration

---

## ~~Nieaktualne Plany~~ (Anulowane przez ADR-003)

<details>
<summary>❌ Plany rozwoju szyfrowania (ANULOWANE)</summary>

### ❌ ~1.1 Wymiana algorytmu szyfrowania DES na AES-256-GCM~

**Status:** ❌ ANULOWANE przez ADR-003

**Decyzja:** Zamiast rozwijać własne szyfrowanie, deprecjonujemy je i rekomendujemy SOPS.

**Dlaczego anulowano:**
- Migracja DES → AES to breaking change dla wszystkich użytkowników
- Zarządzanie kluczami to skomplikowany problem
- SOPS oferuje lepsze bezpieczeństwo i funkcje
- Separation of concerns: biblioteka do ładowania config ≠ system szyfrowania

### ❌ ~Narzędzie CLI dla migracji DES → AES~

**Status:** ❌ ZMIENIONO

**Faktyczna implementacja:** CLI tool `vconfig` służy do migracji z DES do SOPS, nie do AES.

</details>

---

*Dokument utworzony: 2026-02-05*
*Ostatnia aktualizacja: 2026-02-07 (po ADR-003, ADR-004, ADR-005)*
