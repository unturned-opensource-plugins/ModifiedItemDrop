# ADR 0004 — Canonical Player Asset Outcome Model

Status: accepted

## Context

The v1 plugin spreads death-processing decisions across drop rules, pending restore collections, claim records, delete lists, and clothing-content snapshots. That makes it possible for the same Player Asset to be represented more than once, or to disappear from the authoritative flow when an exception, disconnect, or persistence failure happens.

The v2 PRD requires a single authoritative Player Asset Outcome model that can be tested without Rocket/Unturned runtime objects.

## Decision

ModifiedItemDrop v2 will model death processing as a pure domain outcome graph:

- Each Player Asset entering death processing is represented once in domain input.
- Each Player Asset receives exactly one `PlayerAssetOutcome`.
- Clothing Content is represented as a Player Asset with container/source metadata, not as a side collection.
- Runtime operations such as Drop, immediate restoration, Durable Claim fallback, and configured Delete are projections from the same outcome graph.
- Configured Outcome Rules may produce `Drop`, `Keep`, or `Delete` for death-processed Player Assets. `Claim` remains a runtime persistence/fallback projection, not a direct configured rule outcome.

## Consequences

- New code paths must not reconstruct drop/restore/claim state from separate runtime collections after domain planning.
- Tests can assert conservation by comparing input Player Asset IDs with output outcome Asset IDs.
- Plugin adapters may still translate Rocket/Unturned inventory and clothing structures into the domain model, but translation is outside the canonical decision point.
- This is a breaking v2 architecture decision and is not constrained by v1 configuration or command compatibility.
