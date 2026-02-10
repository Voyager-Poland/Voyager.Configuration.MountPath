# ADR-007: Aktualizacja wymagań projektowych dla biblioteki infrastrukturalnej

## Status

Proposed

**Date**: 2026-02-10

## Context

Dokumenty wymagań w folderze `requirements/` zostały napisane jako ogólne standardy firmowe dla aplikacji biznesowych. Jednak projekt `Voyager.Configuration.MountPath` jest **biblioteką infrastrukturalną** (.NET configuration provider), co wymaga odmiennych wzorców niż typowa aplikacja biznesowa.

### Zidentyfikowane rozbieżności

#### 1. Result Pattern vs Exceptions (AI-INSTRUCTIONS.md)

**Wymaganie:** "Error Handling - Result Pattern ONLY", "NEVER throw exceptions for business logic"

**Stan faktyczny:** Biblioteka stosuje wyjątki zgodnie ze standardowymi konwencjami .NET:
- Guard clauses: `throw new ArgumentNullException()`, `throw new ArgumentException()`
- Walidacja w setterach `Settings`: `if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException(...)`
- Custom exception: `EncryptionException` (ADR-006)
- Extension methods rzucają wyjątki dla null parametrów

**Uzasadnienie:** Biblioteki infrastrukturalne w ekosystemie .NET stosują wyjątki na granicach API. Pattern Result jest odpowiedni dla logiki biznesowej, ale nie dla walidacji parametrów wejściowych biblioteki. Microsoft sam stosuje tę konwencję w `IConfigurationBuilder`, `JsonConfigurationProvider` itp.

#### 2. Brak wytycznych dot. mutowalności (TELL-DONT-ASK-GUIDE.md)

**Wymaganie:** Przewodnik opisuje wyjątki dla DTO i Value Objects, ale nie adresuje wzorca **mutable configuration objects**.

**Stan faktyczny:** Klasa `Settings` jest mutowalnym `record` z publicznymi setterami (z walidacją):
```csharp
public record Settings
{
    public string FileName { get; set; }      // walidacja w setterze
    public string ConfigMountPath { get; set; } // walidacja w setterze
    public string? Key { get; set; }           // walidacja długości klucza
    public bool Optional { get; set; }         // brak walidacji
}
```

Jest to **świadomy wzorzec projektowy** stosowany w ASP.NET Core:
```csharp
builder.AddMountConfiguration(settings =>
{
    settings.FileName = "database";
    settings.ConfigMountPath = "/etc/config";
    settings.Optional = false;
});
```

Ten wzorzec (Action\<TOptions\>) jest standardem w ekosystemie .NET (`AddDbContext`, `AddAuthentication`, `AddMvc` itp.) i wymaga mutowalnych obiektów konfiguracyjnych.

#### 3. Konwencja nazewnictwa pól prywatnych (NAMING-RULES.md)

**Wymaganie (przed aktualizacją):** Brak sekcji o polach prywatnych.

**Stan faktyczny:** Kod konsekwentnie używa `_camelCase` (`_fileName`, `_configMountPath`, `_encryptor`).

**Status:** ✅ Już naprawione - dodano sekcję "Prywatne pola klas" do NAMING-RULES.md.

#### 4. Scope zasad SOLID i Railway Programming

**Wymaganie:** Wszystkie przykłady w SOLID-PRINCIPLES.md używają `Result<T>`.

**Stan faktyczny:** Biblioteka nie implementuje pattern Result - jest to provider konfiguracji, nie serwis biznesowy. Zasady SOLID są przestrzegane (SRP, OCP, DIP), ale bez Result pattern.

## Decision

Aktualizujemy dokumenty wymagań, aby uwzględnić specyfikę biblioteki infrastrukturalnej:

### 1. AI-INSTRUCTIONS.md - dodanie sekcji o wyjątkach w bibliotekach

Dodajemy sekcję wyjaśniającą, kiedy wyjątki są właściwym podejściem:

- **Guard clauses** na granicach publicznego API (`ArgumentNullException`, `ArgumentException`)
- **Biblioteki infrastrukturalne** - zgodność z konwencjami .NET SDK
- **Custom exceptions** dla specyficznych domen (np. `EncryptionException`)
- Result pattern pozostaje wymagany dla **logiki biznesowej w aplikacjach**

### 2. TELL-DONT-ASK-GUIDE.md - dodanie wyjątku dla mutable configuration

Dodajemy punkt do sekcji "Kiedy można naruszyć zasadę":

- **Mutable configuration objects** - obiekty konfiguracyjne z wzorca `Action<TOptions>` wymagają publicznych setterów z walidacją. Jest to standardowy wzorzec ASP.NET Core.
- Walidacja w setterach jest formą Tell Don't Ask - obiekt sam pilnuje swoich invariantów.

### 3. Zachowanie spójności

- Wymagania dokumentują **aspiracyjne standardy** dla nowego kodu
- Dokumenty powinny jasno rozróżniać: aplikacja biznesowa vs biblioteka infrastrukturalna
- Istniejący kod biblioteki jest zgodny ze standardami .NET i nie wymaga refaktoryzacji

## Consequences

### Positive

- Wymagania odzwierciedlają rzeczywiste wzorce stosowane w projekcie
- Nowi developerzy nie będą zdezorientowani rozbieżnościami
- AI asystenci (Copilot, Claude) będą generować kod zgodny z faktycznym stylem projektu
- Jasne rozróżnienie: kiedy Result pattern, kiedy exceptions

### Negative

- Wymagania stają się bardziej złożone (więcej wyjątków od reguł)
- Potrzeba utrzymywania dwóch kontekstów (aplikacja vs biblioteka)

### Neutral

- Istniejący kod nie wymaga zmian - aktualizacja dotyczy tylko dokumentacji
- Zasady SOLID pozostają bez zmian - są uniwersalne

### Breaking Changes

Brak - zmiana dotyczy wyłącznie dokumentacji wymagań.

## Implementation Plan

1. Dodać sekcję "Wyjątki dla bibliotek infrastrukturalnych" do `AI-INSTRUCTIONS.md`
2. Dodać punkt "Mutable configuration objects" do `TELL-DONT-ASK-GUIDE.md`
3. Zaktualizować indeks ADR w `docs/adr/README.md`

## References

- [Microsoft: Design Guidelines for Exceptions](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/exceptions)
- [ASP.NET Core Options Pattern](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options)
- ADR-006: Custom Exception Types and Error Handling

## Related ADRs

- ADR-006: Custom Exception Types - wprowadził `EncryptionException`, co jest wzorcem wyjątku w bibliotece
