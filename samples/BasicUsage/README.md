# Basic Usage Example

Prosty przykład pokazujący podstawowe użycie biblioteki Voyager.Configuration.MountPath.

## Co pokazuje ten przykład?

1. **Ładowanie konfiguracji z plików JSON** - organizacja według concern (appsettings, database, logging)
2. **Różne pliki dla różnych środowisk** - automatyczne mergowanie base + environment specific
3. **Odczyt wartości z konfiguracji** - przez IConfiguration

## Struktura Plików

```
BasicUsageExample/
├── Program.cs                     # Główna aplikacja
├── config/
│   ├── appsettings.json          # Podstawowa konfiguracja aplikacji
│   ├── appsettings.Production.json # Production overrides
│   ├── database.json              # Konfiguracja bazy danych
│   ├── database.Production.json   # Production DB config
│   ├── logging.json               # Konfiguracja logowania
│   └── logging.Production.json    # Production logging
```

## Jak uruchomić?

### Development (domyślnie):
```bash
cd BasicUsageExample
dotnet run
```

### Production:
```bash
cd BasicUsageExample
dotnet run --environment Production
```

Lub ustaw zmienną środowiskową:
```bash
# Windows
set ASPNETCORE_ENVIRONMENT=Production
dotnet run

# Linux/Mac
export ASPNETCORE_ENVIRONMENT=Production
dotnet run
```

## Co się dzieje?

1. Aplikacja ładuje pliki konfiguracji z folderu `config/`:
   - `appsettings.json` (wymagany)
   - `appsettings.{Environment}.json` (opcjonalny)
   - `database.json` (wymagany)
   - `database.{Environment}.json` (opcjonalny)
   - `logging.json` (wymagany)
   - `logging.{Environment}.json` (opcjonalny)

2. Wartości z plików environment-specific **nadpisują** wartości z plików base

3. Wszystkie wartości są dostępne przez `IConfiguration`

## Przykładowe wyjście:

```
=== Voyager.Configuration.MountPath - Basic Usage Example ===

Environment: Development
Config Path: config/

=== Loaded Configuration Values ===

Application Settings:
  AppName: BasicUsageExample
  Version: 1.0.0
  Environment: Development

Database Configuration:
  ConnectionString: Server=localhost;Database=myapp;User=dev;Password=dev123
  Timeout: 30
  MaxRetries: 3

Logging Configuration:
  LogLevel:Default: Information
  LogLevel:Microsoft: Warning

=== All Configuration Keys ===
  AppName = BasicUsageExample
  ConnectionStrings:Default = Server=localhost;Database=myapp;User=dev;Password=dev123
  Database:Timeout = 30
  Database:MaxRetries = 3
  ...
```

## Kluczowe punkty

### 1. Organizacja według concern
Zamiast jednego wielkiego `appsettings.json`, mamy osobne pliki:
- `appsettings.json` - ogólne ustawienia aplikacji
- `database.json` - wszystko związane z bazą danych
- `logging.json` - konfiguracja logowania

### 2. Environment-specific overrides
Pliki `*.Production.json` nadpisują wartości z plików base:
```json
// database.json
{
  "ConnectionStrings": {
    "Default": "Server=localhost;..."  // Development
  }
}

// database.Production.json
{
  "ConnectionStrings": {
    "Default": "Server=prod-db.example.com;..."  // Production override
  }
}
```

### 3. Hot Reload
Zmiana pliku konfiguracji automatycznie przeładowuje wartości (jeśli `reloadOnChange: true`).

## Następne kroki

- Zobacz [KubernetesExample](../KubernetesExample/) - jak używać w Kubernetes/Docker
- Zobacz [MigrationToSops](../MigrationToSops/) - jak migrować z encryption do SOPS
