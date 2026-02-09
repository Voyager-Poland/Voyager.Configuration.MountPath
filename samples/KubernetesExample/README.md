# Kubernetes/Docker Example

> **📋 Status:** Planowane

## Co będzie zawierać ten przykład?

1. **Dockerfile** - konteneryzacja aplikacji ASP.NET Core
2. **Kubernetes manifesty:**
   - Deployment - definicja aplikacji
   - ConfigMap - osobne mapy dla różnych concern (database, logging, services)
   - Service - eksponowanie aplikacji
3. **docker-compose.yml** - przykład lokalny z Docker Compose
4. **Organizacja konfiguracji** - separacja według concern

## Struktura (planowana)

```
KubernetesExample/
├── src/
│   └── WebApp/                    # Przykładowa aplikacja ASP.NET Core
│       ├── Program.cs
│       ├── Dockerfile
│       └── appsettings.json
├── k8s/
│   ├── deployment.yaml           # Deployment + volume mounts
│   ├── database-config.yaml      # ConfigMap dla database
│   ├── logging-config.yaml       # ConfigMap dla logging
│   └── services-config.yaml      # ConfigMap dla external services
├── docker-compose.yml            # Docker Compose dla local dev
└── README.md
```

## Kluczowe aspekty

- **Volume mounts** - montowanie ConfigMaps jako plików
- **Separation of concerns** - osobne ConfigMaps dla różnych aspektów
- **Environment variables** - `ASPNETCORE_ENVIRONMENT` w deployment
- **Hot reload** - automatyczne przeładowanie przy zmianie ConfigMap
- **Read-only mounts** - bezpieczeństwo (`:ro`)

## Temporary Workaround

Dopóki ten przykład nie zostanie utworzony, zobacz:
- [README główny](../../README.md#kubernetes-example) - podstawowy przykład Kubernetes
- [ADR-003](../../docs/adr/ADR-003-encryption-delegation-to-external-tools.md) - zawiera przykłady Kubernetes
