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
| ADR-002 | Encryption Algorithm Selection (DES → AES-256-GCM) | Planned | - |
| ADR-003 | Configuration Structure (mount paths, file naming) | Planned | - |
| ADR-004 | Versioning Strategy (SemVer, breaking changes) | Planned | - |

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
