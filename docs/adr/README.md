# Architecture Decision Records (ADRs)

This directory contains Architecture Decision Records (ADRs) for the Voyager.Configuration.MountPath library.

## What is an ADR?

An Architecture Decision Record (ADR) is a document that captures an important architectural decision made along with its context and consequences.

## Why ADRs?

- **Knowledge Sharing**: Help team members understand why certain decisions were made
- **Onboarding**: New developers can quickly understand the project's architectural evolution
- **Historical Context**: Preserve the reasoning behind decisions, even as team members change
- **Accountability**: Document who made decisions and when
- **Prevent Rework**: Avoid revisiting already-discussed alternatives

## ADR Format

Each ADR follows this structure:

1. **Title**: ADR-XXX: [Short descriptive title]
2. **Status**: Proposed | Accepted | Deprecated | Superseded
3. **Context**: What is the issue we're seeing that is motivating this decision?
4. **Decision**: What is the change that we're proposing and/or doing?
5. **Consequences**: What becomes easier or more difficult to do because of this change?

## Index of ADRs

| ADR | Title | Status | Date |
|-----|-------|--------|------|
| [ADR-001](ADR-001-extension-methods-organization.md) | Organization of Configuration Extension Methods | Accepted | 2026-02-06 |
| [ADR-002](ADR-002-settings-builder-pattern.md) | Settings Builder Pattern | Accepted | 2026-02-06 |
| [ADR-003](ADR-003-encryption-delegation-to-external-tools.md) | Encryption Delegation to External Tools | Accepted | 2026-02-07 |
| [ADR-004](ADR-004-cli-tool-for-configuration-encryption.md) | CLI Tool for Configuration Encryption | Accepted | 2026-02-07 |
| [ADR-005](ADR-005-async-configuration-loading.md) | Async Configuration Loading | Accepted | 2026-02-07 |
| [ADR-006](ADR-006-custom-exception-types-and-error-handling.md) | Custom Exception Types and Error Handling | Accepted | 2026-02-07 |
| [ADR-007](ADR-007-requirements-alignment-with-infrastructure-library.md) | Aktualizacja wymagań dla biblioteki infrastrukturalnej | Proposed | 2026-02-10 |

## Creating a New ADR

1. Copy the template from `ADR-template.md`
2. Rename it to `ADR-XXX-short-title.md` (next number in sequence)
3. Fill in the sections
4. Update this README index
5. Submit a pull request

## References

- [Architecture Decision Records](https://adr.github.io/)
- [Documenting Architecture Decisions](https://cognitect.com/blog/2011/11/15/documenting-architecture-decisions)
- [ADR Tools](https://github.com/npryce/adr-tools)
