# Voyager.Configuration.MountPath - Przykłady Użycia

Ten folder zawiera przykłady użycia biblioteki Voyager.Configuration.MountPath w różnych scenariuszach.

## Struktura Przykładów

### 1. [BasicUsage](BasicUsage/) - Podstawowe Użycie
Prosty przykład pokazujący:
- Ładowanie konfiguracji z plików JSON
- Używanie różnych plików dla różnych środowisk (Development, Production)
- Odczyt wartości z konfiguracji

**Jak uruchomić:**
```bash
cd samples/BasicUsage
dotnet run
```

### 2. [KubernetesExample](KubernetesExample/) - Kubernetes/Docker
Kompletny przykład wdrożenia w Kubernetes:
- Dockerfile dla aplikacji
- Manifesty Kubernetes (Deployment, ConfigMap, Service)
- Organizacja konfiguracji według concern (database, logging, services)
- Montowanie ConfigMaps jako plików

**Jak uruchomić:**
```bash
cd samples/KubernetesExample
# Build docker image
docker build -t myapp:latest .
# Deploy to Kubernetes
kubectl apply -f k8s/
```

### 3. [MigrationFromDes](MigrationFromDes/) - Migracja z DES na AES-256-GCM
Przewodnik migracji z legacy DES do AES-256-GCM (ADR-010):
- Generowanie nowego klucza AES (`vconfig keygen`)
- Re-szyfrowanie plików (`vconfig reencrypt`)
- Weryfikacja z `--dry-run`

**Jak uruchomić:**
```bash
cd samples/MigrationFromDes
# Zobacz README.md w tym folderze dla szczegółowych instrukcji
```

## Wymagania

- .NET 8.0 SDK (lub nowszy)
- Docker (dla przykładu Kubernetes)
- kubectl (dla przykładu Kubernetes)
- Voyager.Configuration.Tool (`vconfig`) — dla przykładu migracji DES→AES

## Dodatkowe Zasoby

- [Główny README](../README.md) - Pełna dokumentacja biblioteki
- [ADR-010](../docs/adr/ADR-010-aes-gcm-with-versioned-ciphertext.md) - AES-256-GCM encryption
- [ADR-004](../docs/adr/ADR-004-cli-tool-for-configuration-encryption.md) - CLI tool
- [ROADMAP](../docs/ROADMAP.md) - Plan rozwoju biblioteki

## Support

Jeśli masz pytania lub problemy:
- Otwórz issue na [GitHub](https://github.com/Voyager-Poland/Voyager.Configuration.MountPath/issues)
- Przejrzyj [dokumentację](../docs/)
