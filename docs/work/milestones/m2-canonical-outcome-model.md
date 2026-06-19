# M2 — Canonical Player Asset Outcome Model

- Fixed diff base: `0cc6f13`
- PRD: `docs/prd/2026-06-reliability-maintainability-program.md`
- ADR: `docs/adr/0004-canonical-player-asset-outcome-model.md`

## Scope

M2 introduces a pure domain outcome graph where top-level Player Assets and Clothing Content are both represented as `PlayerAsset` inputs and each input receives exactly one `PlayerAssetOutcome`.

## TDD Evidence

### Red

Command:

```bash
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet test ModifiedItemDrop.Domain.Tests/ModifiedItemDrop.Domain.Tests.csproj -v minimal
```

Result: failed to compile because M2 API did not exist yet (`PlayerAsset.ClothingContent`, `OutcomeRule.Keep`, `OutcomeRule.Delete`, `OutcomeTarget.Any`, `OutcomeTarget.ForItem`, `OutcomeTarget.ForClothingContent`, `DeathOutcomePlanner.PlanDeathSession`, `Keep`/`Delete` outcome kinds).

### Green

Command:

```bash
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet test ModifiedItemDrop.Domain.Tests/ModifiedItemDrop.Domain.Tests.csproj -v minimal
```

Result: `Passed: 2, Failed: 0`.

## Review Notes

- Outcome graph now treats Clothing Content as first-class `PlayerAsset` input with parent/source metadata, not a side collection.
- `DeathOutcomePlan` exposes lookup by Asset ID and stores one outcome per planned asset.
- `Keep` is modeled as requiring restoration and being Durable Claim eligible; `Drop` and `Delete` are terminal projections.
- Domain project still has no Rocket/RocketMod/Unturned/Unity/Steamworks references.
- Deliberately not covered in M2: probability roll semantics, equal-priority invalid config, XML parsing, safe mode, and runtime adapters. Those belong to later PRD milestones.
