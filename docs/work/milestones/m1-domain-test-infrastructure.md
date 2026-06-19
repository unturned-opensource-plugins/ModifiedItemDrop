# M1 — Domain Test Infrastructure

- Fixed diff base: `f744179`
- PRD: `docs/prd/2026-06-reliability-maintainability-program.md`
- Goal: introduce a pure domain project plus xUnit test project for v2 outcome planning.
- TDD tracer bullet:
  - Given a PrimaryWeapon player asset
  - And an always-drop outcome rule (`chance = 1.0`)
  - When death outcome planning runs
  - Then the player asset outcome is `Drop`
- Constraints:
  - `ModifiedItemDrop.Domain` must not reference Rocket/RocketMod/Unturned/Unity/Steamworks/plugin APIs.
  - Tests target the domain surface directly.
  - Runtime plugin integration is out of scope for M1.

## TDD Evidence

### Red

Command:

```bash
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet test ModifiedItemDrop.Domain.Tests/ModifiedItemDrop.Domain.Tests.csproj -v minimal
```

Result: failed to compile because the tracer bullet referenced missing domain API (`PlayerAsset`, `PlayerAssetSlot`, `OutcomeRule`, `OutcomeTarget`, `DeathOutcomePlanner`, `PlayerAssetOutcomeKind`).

### Green

Command:

```bash
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet test ModifiedItemDrop.Domain.Tests/ModifiedItemDrop.Domain.Tests.csproj -v minimal
```

Result: `Passed: 1, Failed: 0`.

## Review Notes

- `ModifiedItemDrop.Domain` targets `netstandard2.0` to remain consumable by the existing `net48` plugin while tests run on `net8.0`.
- Domain project contains no Rocket/RocketMod/Unturned/Unity/Steamworks references.
- Implementation is intentionally narrow: only the M1 tracer bullet surface is implemented. Probability edge cases, tie priority invalidation, fallback rules, additional slots, and non-Drop outcomes remain for later PRD milestones/tests.
- Hidden risk: local `dotnet` was absent. M1 verification installed Homebrew `dotnet@8`; commands currently require `DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH` unless the shell profile is updated.
