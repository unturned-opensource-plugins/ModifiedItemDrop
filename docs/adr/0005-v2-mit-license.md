# ADR 0005 — V2 canonical license is MIT

Status: accepted

## Context

The v2 PRD required a single canonical license before release because repository metadata conflicted: `LICENSE` contained GPL text while README and generated release notes advertised MIT.

## Decision

ModifiedItemDrop v2.0.0 uses the MIT License as the canonical project license.

## Consequences

- `LICENSE`, README license badge/text, project package metadata, and release workflow checks must agree on MIT.
- Release notes must come from explicit release documentation rather than stale generated text.
- The release workflow must fail if the tag version, project version, release notes path, or license metadata drift.
