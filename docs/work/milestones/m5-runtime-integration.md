# M5 — Runtime Integration for Death, Respawn, Disconnect, Unload

- Fixed diff base: `dd442fc`
- PRD: `docs/prd/2026-06-reliability-maintainability-program.md`

## Scope

M5 wires the domain outcome model, rule engine, restoration projection, Durable Claims, and Drop fallback into runtime flows. Early slices stay in pure domain orchestration; Rocket/Unturned adapters are added after behavior is pinned.

## Slice 1 — Disconnect finalization fallback

Behavior: when a player disconnects during a Death Session, kept Player Assets must become a Durable Claim or Drop fallback before the session is forgotten. If Durable Claim creation fails and immediate restore is unavailable, unresolved kept Player Assets route to Drop fallback.

### Slice 1 TDD Evidence

Red: `dotnet test` failed because `IDurableClaimCreator`, `DeathSession`, and `DeathSessionFinalizer` did not exist.

Green command:

```bash
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet test ModifiedItemDrop.Domain.Tests/ModifiedItemDrop.Domain.Tests.csproj -v minimal
```

Green result: `Passed: 26, Failed: 0`.

Review note: this is pure domain orchestration. Runtime adapter execution of actual Rocket/Unturned drop operations remains a later M5 slice.

## Slice 2 — Plugin unload finalizes kept Player Assets

Behavior: plugin unload/server shutdown uses the same Death Session responsibility as disconnect. Kept Player Assets must become Durable Claims or Drop fallback before the session is forgotten.

Red: `dotnet test` failed because `DeathSessionFinalizer.FinalizePluginUnload` did not exist.

Green command:

```bash
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet test ModifiedItemDrop.Domain.Tests/ModifiedItemDrop.Domain.Tests.csproj -v minimal
```

Green result: `Passed: 27, Failed: 0`.

Review note: this slice covers the successful Durable Claim path for unload. Runtime event subscription remains a later adapter slice.

## Slice 3 — Respawn restore failure fallback

Behavior: when respawn restoration fails because assets cannot be placed, unresolved kept Player Assets are durably claimed; if Durable Claim creation fails, they route to Drop fallback.

Red: `dotnet test` failed because `DeathSessionFinalizer.FinalizeRespawnRestoreFailure` did not exist.

Green command:

```bash
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet test ModifiedItemDrop.Domain.Tests/ModifiedItemDrop.Domain.Tests.csproj -v minimal
```

Green result: `Passed: 28, Failed: 0`.

Review note: this models the domain finalization decision for overflow. Actual inventory placement and player notification remain adapter responsibilities.

## Slice 4 — Emergency failure fallback

Behavior: after a death-processing exception, unresolved kept Player Assets must be restored, durably claimed, or dropped before the Death Session ends. If Durable Claim creation fails and immediate restore is unavailable, the assets route to Drop fallback.

Red: `dotnet test` failed because `DeathSessionFinalizer.FinalizeEmergencyFailure` did not exist.

Green command:

```bash
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet test ModifiedItemDrop.Domain.Tests/ModifiedItemDrop.Domain.Tests.csproj -v minimal
```

Green result: `Passed: 29, Failed: 0`.

Review note: this pins the domain finalization behavior for exception handling. Runtime try/catch integration remains an adapter slice.

## Slice 5 — Respawn Grant requires tracked Death Session

Behavior: `AfterDeathRespawn` Grant rules execute only when a tracked Death Session reaches respawn, and only once per session. Revive/respawn without a tracked Death Session produces no grant.

Red: `dotnet test` failed because `DeathSessionRespawnGrantPlanner` did not exist.

Green command:

```bash
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet test ModifiedItemDrop.Domain.Tests/ModifiedItemDrop.Domain.Tests.csproj -v minimal
```

Green result: `Passed: 31, Failed: 0`.

Review note: this pins the domain gating semantics. Runtime revive/respawn event mapping remains an adapter slice.

## Slice 6 — Disconnect successful Durable Claim path

Behavior: when a player disconnects during a Death Session and Durable Claim creation succeeds, the session ends without Drop fallback and the kept Player Assets are represented in the created claim.

Result: this test passed immediately because Slice 1 and Slice 2 share the finalization implementation. It is retained as explicit disconnect success-path coverage.

Verification command:

```bash
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet test ModifiedItemDrop.Domain.Tests/ModifiedItemDrop.Domain.Tests.csproj -v minimal
```

Result: `Passed: 32, Failed: 0`.

## Slice 7 — Death processing creates tracked Death Session

Behavior: death processing converts a Player Asset Outcome plan into a Death Session that tracks only `Keep` outcomes. Terminal Drop/Delete outcomes do not become pending Death Session responsibility.

Red: `dotnet test` failed because `DeathSessionFactory` did not exist.

Green command:

```bash
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet test ModifiedItemDrop.Domain.Tests/ModifiedItemDrop.Domain.Tests.csproj -v minimal
```

Green result: `Passed: 33, Failed: 0`.

Review note: adapter code must translate Rocket/Unturned inventory snapshots into Player Assets, run outcome planning, and create Death Sessions from this factory.

## Slice 8 — Plugin initializes v2 Durable Claim adapter

Behavior: plugin load creates the v2 Durable Claim store at the versioned storage layout and exposes a `V2DurableClaimCreator` adapter for runtime Death Session finalization. The legacy v1 Claim service remains present until later slices replace old pending restore paths.

Verification command:

```bash
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet build ModifiedItemDrop.csproj -v minimal
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet test ModifiedItemDrop.Domain.Tests/ModifiedItemDrop.Domain.Tests.csproj -v minimal
```

Result: plugin build succeeded with `0 Warning(s), 0 Error(s)`; domain tests `Passed: 33, Failed: 0`.

## Slice 9 — PendingRestore uses v2 Durable Claim adapter

Behavior: runtime pending restores can be converted to v2 `DurableClaimRecord` assets and `RestoreManager.SavePendingToClaimOrDrop` prefers the v2 Durable Claim creator when configured. If v2 Durable Claim creation fails, the existing Drop fallback path is used.

Verification command:

```bash
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet build ModifiedItemDrop.csproj -v minimal
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet test ModifiedItemDrop.Domain.Tests/ModifiedItemDrop.Domain.Tests.csproj -v minimal
```

Result: plugin build succeeded with `0 Warning(s), 0 Error(s)`; domain tests `Passed: 33, Failed: 0`.

Review note: v1 `ClaimService` remains available for existing claim commands until the v2 command/Claim Recovery slices replace it.

## Slice 10 — `/mid claim` can recover v2 Durable Claims

Behavior: v2 pending restores written to `claims/v2/claims.json` are recoverable through the existing claim entrypoint while M6 command redesign is still pending. Successfully restored Durable Claim assets are pruned from v2 storage; partially restored claims keep only unresolved assets, and fully restored claims are removed.

Red: `dotnet test` failed because `DurableClaimStore.TryPruneAssets` did not exist.

Green command:

```bash
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet test ModifiedItemDrop.Domain.Tests/ModifiedItemDrop.Domain.Tests.csproj -v minimal
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet build ModifiedItemDrop.csproj -v minimal
```

Result: plugin build succeeded with `0 Warning(s), 0 Error(s)`; domain tests `Passed: 34, Failed: 0`.

Review note: the runtime `V2ClaimRecoveryService` restores v2 Durable Claim assets as normal inventory items and prunes only successfully placed assets. The existing v1 Claim service remains as a fallback for old claims until v2 commands and migration are finalized.

## Slice 11 — Death Session claims preserve Player Asset item data

Behavior: kept Player Assets finalized into a Durable Claim must preserve item id, amount, quality, and serialized state. Claim persistence must not synthesize default amount/quality/state because that can duplicate, weaken, or corrupt assets recovered after disconnect/unload.

Red: `dotnet test` failed because `PlayerAsset` did not expose `amount`, `quality`, or `state` data.

Green command:

```bash
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet test ModifiedItemDrop.Domain.Tests/ModifiedItemDrop.Domain.Tests.csproj -v minimal
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet build ModifiedItemDrop.csproj -v minimal
```

Result: plugin build succeeded with `0 Warning(s), 0 Error(s)`; domain tests `Passed: 35, Failed: 0`.

Review note: this closes a Player Asset Conservation gap before deeper runtime wiring: the canonical domain object now carries the minimum item data required for Durable Claim recovery.

## Slice 12 — Canonical slots cover runtime clothing assets

Behavior: v2 Outcome Rules must be able to target runtime clothing slots such as `Shirt`, not only primary/secondary/hands/backpack. Runtime death integration cannot safely project clothing snapshots into canonical Player Assets while the domain slot vocabulary is narrower than Unturned clothing slots.

Red: `dotnet test` failed because `PlayerAssetSlot` did not contain `Shirt`.

Green command:

```bash
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet test ModifiedItemDrop.Domain.Tests/ModifiedItemDrop.Domain.Tests.csproj -v minimal
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet build ModifiedItemDrop.csproj -v minimal
```

Result: plugin build succeeded with `0 Warning(s), 0 Error(s)`; domain tests `Passed: 36, Failed: 0`.

Review note: `PlayerAssetSlot` now includes `Backpack`, `Vest`, `Shirt`, `Pants`, `Hat`, `Mask`, and `Glasses`, so XML slot targets can cover all clothing assets before runtime adapter wiring.

## Slice 13 — Runtime snapshots project into canonical Player Assets

Behavior: runtime inventory and clothing snapshots have a single adapter path into canonical `PlayerAsset` values before v2 death planning. The projection preserves source identity, slot, item id, amount, quality, state, and clothing-content parentage without adding Rocket/Unturned references to the Domain project.

Red: `dotnet test` failed because `PlayerAssetProjection` did not exist.

Green command:

```bash
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet test ModifiedItemDrop.Domain.Tests/ModifiedItemDrop.Domain.Tests.csproj -v minimal
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet build ModifiedItemDrop.csproj -v minimal
```

Result: plugin build succeeded with `0 Warning(s), 0 Error(s)`; domain tests `Passed: 38, Failed: 0`.

Review note: `ModifiedItemDrop.Domain/PlayerAssetProjection.cs` owns the pure canonical projection contract. `Drop/V2PlayerAssetRuntimeAdapter.cs` is the Rocket/Unturned boundary adapter and keeps runtime types outside the Domain project.

## Slice 14 — Death processing orchestrator creates the v2 runtime seam

Behavior: runtime death processing has one orchestration path from projected Player Assets and Outcome Rules to a `DeathOutcomePlan` plus optional pending `DeathSession`. A Drop outcome produces no pending Death Session responsibility; a Keep outcome creates Death Session responsibility for later respawn, disconnect, unload, or fallback finalization.

Red: `dotnet test` failed because `DeathProcessingOrchestrator` did not exist.

Green command:

```bash
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet test ModifiedItemDrop.Domain.Tests/ModifiedItemDrop.Domain.Tests.csproj -v minimal
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet build ModifiedItemDrop.csproj -v minimal
```

Result: plugin build succeeded with `0 Warning(s), 0 Error(s)`; domain tests `Passed: 40, Failed: 0`.

Additional invariant check:

```bash
rg -n "Rocket|Unturned|Unity|Steamworks|SDG" ModifiedItemDrop.Domain ModifiedItemDrop.Domain.Tests -g'*.cs'
```

Result: no matches.

Review note: `Drop/V2DeathProcessingAdapter.cs` is now the runtime-facing seam that projects Unturned snapshots and delegates to the pure domain orchestrator. The next slice can execute Drop/Delete/Keep effects from the returned plan inside `DropService.HandlePlayerDying`.
