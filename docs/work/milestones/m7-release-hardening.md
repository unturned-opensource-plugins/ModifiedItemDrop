# M7 — Performance, Release, and Final Review Hardening

- Fixed diff base: `f386b23a69f168fc687824d3144fd526925f0d1d`
- PRD: `docs/prd/2026-06-reliability-maintainability-program.md`
- Canonical v2 license decision: MIT

## Slice 1 — Release metadata uses canonical MIT and v2.0.0

Behavior: release metadata must be internally consistent before v2.0.0 can ship. `LICENSE`, README license badge/text, project package metadata, release workflow, and explicit release notes must all agree on MIT and v2.0.0.

Red:

```bash
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet test ModifiedItemDrop.Domain.Tests/ModifiedItemDrop.Domain.Tests.csproj -v minimal --filter ReleaseMetadataIntegrityTests
# failed: LICENSE contained GPL text instead of MIT License
```

Green command:

```bash
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet test ModifiedItemDrop.Domain.Tests/ModifiedItemDrop.Domain.Tests.csproj -v minimal --filter ReleaseMetadataIntegrityTests
```

Result: release metadata integrity test passed.

Review note: ADR 0005 records the MIT license decision. Release notes now live in `docs/release/v2.0.0.md`, and the release workflow reads that explicit file rather than generating stale v1 notes inline.

## Slice 2 — Inventory Capability policy and packaged v2 configuration

Behavior: v2 hands slot sizing is represented as an Inventory Capability, not a Player Asset Outcome. The pure domain `InventoryCapabilityPolicy` selects the last matching permission rule, falls back to an explicit `default` rule, clamps non-positive dimensions to 1 and otherwise preserves configured dimensions, and returns a diagnostic explaining the applied rule. Runtime hands slot resizing and diagnostics export now use this policy. The packaged configuration file now uses `OutcomeRulesXml` and no longer ships a v1 `RuleSet` sample or v1 flat command examples.

Red:

```bash
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet test ModifiedItemDrop.Domain.Tests/ModifiedItemDrop.Domain.Tests.csproj -v minimal --filter InventoryCapabilityPolicyTests
# failed: InventoryCapabilityPolicy and HandsSlotCapabilityRule did not exist

DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet test ModifiedItemDrop.Domain.Tests/ModifiedItemDrop.Domain.Tests.csproj -v minimal --filter PackagedConfigurationTests
# failed: packaged configuration still contained <RuleSet>
```

## Slice 3 — Remove legacy v1 death-rule runtime tombstones

Behavior: runtime code no longer depends on v1 `ChanceResolver`, `DropRuleSet`, `DeathSettings`, `InventoryProcessor`, `ClothingProcessor`, or legacy respawn grant execution. Pending restore recovery is now owned by `RestoreManager`, while death decisions remain owned by v2 Outcome Rules and execution adapters. User-facing Claim messages now point to `/mid claims recover oldest`.

Red:

```bash
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet test ModifiedItemDrop.Domain.Tests/ModifiedItemDrop.Domain.Tests.csproj -v minimal --filter LegacyRuntimeTombstoneTests
# failed: runtime files still referenced ChanceResolver/DropRuleSet/DeathSettings/GiveRespawnItems
```

Green command:

```bash
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet test ModifiedItemDrop.Domain.Tests/ModifiedItemDrop.Domain.Tests.csproj -v minimal
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet build ModifiedItemDrop.csproj -v minimal
```

Result: domain tests `Passed: 75, Failed: 0`; plugin build succeeded with `0 Warning(s), 0 Error(s)`.
