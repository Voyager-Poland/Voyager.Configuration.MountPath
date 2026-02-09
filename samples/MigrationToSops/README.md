# Migracja z Encryption do SOPS

> **📋 Status:** Planowane

## Co będzie zawierać ten przykład?

1. **Krok po kroku migracja:**
   - Deszyfrowanie starych plików używając `vconfig decrypt`
   - Instalacja SOPS i Age
   - Generowanie kluczy Age
   - Szyfrowanie plików używając SOPS
   - Aktualizacja kodu (usunięcie `AddEncryptedMountConfiguration`)

2. **Przykłady dla różnych środowisk:**
   - Lokalna migracja (Age keys)
   - Kubernetes (Age secrets)
   - CI/CD pipeline (GitHub Actions, Azure DevOps)
   - Cloud KMS (AWS KMS, Azure Key Vault, GCP KMS)

3. **Supervisor example** - używany przez adminów Voyager

## Struktura (planowana)

```
MigrationToSops/
├── before/                        # Stara konfiguracja (DES encryption)
│   ├── config/
│   │   ├── secrets.json          # Zaszyfrowane
│   │   └── database.json         # Zaszyfrowane
│   └── Program.cs                # Używa AddEncryptedMountConfiguration
├── after/                         # Nowa konfiguracja (SOPS)
│   ├── config/
│   │   ├── secrets.json          # Zaszyfrowane przez SOPS
│   │   └── database.json         # Zaszyfrowane przez SOPS
│   ├── Program.cs                # Używa AddMountConfiguration (bez encryption)
│   └── .sops.yaml                # Konfiguracja SOPS
├── scripts/
│   ├── 01-decrypt-old-files.sh   # vconfig decrypt
│   ├── 02-install-sops.sh        # Instalacja SOPS + Age
│   ├── 03-generate-keys.sh       # age-keygen
│   ├── 04-encrypt-with-sops.sh   # sops -e
│   └── 05-update-code.sh         # Usuń AddEncrypted...
├── k8s/
│   ├── age-secret.yaml           # Age key jako Secret
│   └── deployment.yaml           # Deploy z SOPS
└── README.md
```

## Temporary Workaround

Dopóki ten przykład nie zostanie utworzony, zobacz:
- [ADR-003](../../docs/adr/ADR-003-encryption-delegation-to-external-tools.md) - **Szczegółowy przewodnik migracji**
  - Krok po kroku instrukcje
  - Przykłady dla Kubernetes
  - Przykłady dla Supervisor
  - Porównanie rozwiązań

## Narzędzie vconfig

```bash
# Instalacja
dotnet tool install -g Voyager.Configuration.Tool --prerelease

# Deszyfrowanie dla migracji
vconfig decrypt --input config/secrets.json --output config/secrets.plain.json --key "YourEncryptionKey"

# Wartość pojedyncza
vconfig decrypt-value "encrypted_string" --key "YourKey"
```

## SOPS Quick Start

```bash
# Instalacja
brew install sops age  # Mac
# lub zobacz: https://github.com/mozilla/sops

# Generowanie klucza Age
age-keygen -o ~/.config/sops/age/keys.txt

# Szyfrowanie
export SOPS_AGE_KEY_FILE=~/.config/sops/age/keys.txt
sops -e config/secrets.json > config/secrets.json

# Deszyfrowanie (w deployment script)
sops -d /config-encrypted/secrets.json > /config/secrets.json
```

## Dlaczego migrować?

✅ SOPS oferuje:
- **AES-256-GCM** zamiast przestarzałego DES (56-bit)
- **GitOps-friendly** - encrypted pliki w Git
- **Cloud KMS** - AWS, Azure, GCP
- **Lepsze narzędzia** - edycja bez manual decrypt/encrypt

❌ Wbudowane szyfrowanie (deprecated):
- DES jest niebezpieczny
- Brak integracji z KMS
- Zarządzanie kluczami problematyczne
- Zostanie usunięte w v3.0

**Zobacz:** [ADR-003](../../docs/adr/ADR-003-encryption-delegation-to-external-tools.md) dla pełnych szczegółów.
