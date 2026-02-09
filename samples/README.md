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

### 3. [MigrationToSops](MigrationToSops/) - Migracja do SOPS
Przewodnik migracji z wbudowanego szyfrowania DES do Mozilla SOPS:
- Deszyfrowanie starych plików używając `vconfig`
- Konfiguracja SOPS (Age, AWS KMS, Azure Key Vault)
- Szyfrowanie plików używając SOPS
- Integracja z CI/CD

**Jak uruchomić:**
```bash
cd samples/MigrationToSops
# Zobacz README.md w tym folderze dla szczegółowych instrukcji
```

## Wymagania

- .NET 8.0 SDK (lub nowszy)
- Docker (dla przykładu Kubernetes)
- kubectl (dla przykładu Kubernetes)
- SOPS i Age/KMS (dla przykładu migracji)

## Dodatkowe Zasoby

- [Główny README](../README.md) - Pełna dokumentacja biblioteki
- [ADR-003](../docs/adr/ADR-003-encryption-delegation-to-external-tools.md) - Decyzja o delegacji szyfrowania
- [ADR-004](../docs/adr/ADR-004-cli-tool-for-configuration-encryption.md) - CLI tool dla migracji
- [ROADMAP](../docs/ROADMAP.md) - Plan rozwoju biblioteki

## Support

Jeśli masz pytania lub problemy:
- Otwórz issue na [GitHub](https://github.com/Voyager-Poland/Voyager.Configuration.MountPath/issues)
- Przejrzyj [dokumentację](../docs/)
