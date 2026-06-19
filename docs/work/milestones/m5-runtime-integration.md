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

## Slice 15 — Death Outcome execution plan maps outcomes to runtime actions

Behavior: v2 death processing must expose explicit runtime actions for each Player Asset Outcome so `DropService.HandlePlayerDying` can execute the canonical plan instead of reconstructing decisions in legacy processors. `Drop` maps to `Drop`, `Delete` maps to `Delete`, and `Keep` maps to `KeepForRestore`.

Red: `dotnet test` failed because `DeathOutcomeExecutionPlanner` and `DeathOutcomeExecutionActionKind` did not exist.

Green command:

```bash
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet test ModifiedItemDrop.Domain.Tests/ModifiedItemDrop.Domain.Tests.csproj -v minimal
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet build ModifiedItemDrop.csproj -v minimal
```

Result: plugin build succeeded with `0 Warning(s), 0 Error(s)`; domain tests `Passed: 41, Failed: 0`.

Review note: `DeathProcessingResult` now includes both the canonical `DeathOutcomePlan` and the runtime-oriented `DeathOutcomeExecutionPlan`. This keeps action selection in the pure domain model while leaving actual Unturned mutation inside adapter/runtime code.

## Slice 16 — Quick-slot execution adapter consumes v2 actions

Behavior: v2 quick-slot runtime execution can consume `DeathOutcomeExecutionPlan` without parsing string asset ids. Inventory-projected `PlayerAsset` values preserve source page/index; the quick-slot adapter removes the original runtime item exactly once, then performs the selected action: Drop to world, Delete silently, or Keep into `PendingRestore` for respawn/Claim responsibility.

Red: `dotnet test` failed because `PlayerAsset` did not expose `InventoryPage`/`InventoryIndex`.

Green command:

```bash
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet test ModifiedItemDrop.Domain.Tests/ModifiedItemDrop.Domain.Tests.csproj -v minimal
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet build ModifiedItemDrop.csproj -v minimal
```

Result: plugin build succeeded with `0 Warning(s), 0 Error(s)`; domain tests `Passed: 41, Failed: 0`.

Additional invariant check:

```bash
rg -n "Rocket|Unturned|Unity|Steamworks|SDG" ModifiedItemDrop.Domain ModifiedItemDrop.Domain.Tests -g'*.cs'
```

Result: no matches.

Review note: `Drop/V2QuickSlotExecutionAdapter.cs` is the first concrete runtime mutator for v2 `ExecutionPlan`. `DropService` now has a seam for invoking it; the next slice should replace the primary quick-slot branch in `HandlePlayerDying` with this adapter once v2 rules are available from configuration.

## Slice 17 — Quick-slot death handling uses configured v2 Outcome Rules

Behavior: runtime configuration exposes v2 Outcome Rules XML with an explicit default catch-all rule, and `DropService.HandlePlayerDying` routes quick-slot inventory snapshots through `V2DeathProcessingAdapter` and `V2QuickSlotExecutionAdapter` before legacy processors run. This replaces legacy quick-slot ChanceResolver decisions with canonical `DeathOutcomePlan`/`ExecutionPlan` decisions while leaving clothing migration for later slices.

Red: `dotnet test` failed because `DefaultOutcomeRules` did not exist.

Green command:

```bash
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet test ModifiedItemDrop.Domain.Tests/ModifiedItemDrop.Domain.Tests.csproj -v minimal
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet build ModifiedItemDrop.csproj -v minimal
```

Result: plugin build succeeded with `0 Warning(s), 0 Error(s)`; domain tests `Passed: 42, Failed: 0`.

Review note: `ModifiedItemDropConfiguration.OutcomeRulesXml` defaults to `DefaultOutcomeRules.Xml`; `ConfigurationLoader.CurrentOutcomeRules` parses it once on reload. Invalid-rule safe-mode behavior is still pending and must be completed before M5/M7 release readiness.

## Slice 18 — Invalid Outcome Rules enter safe mode

Behavior: invalid v2 Outcome Rules produce an explicit safe-mode configuration state instead of falling back to hidden Drop/Keep defaults or throwing out of plugin configuration load. Safe mode disables death processing before any player assets are mutated while leaving Claim Recovery command paths outside this death-processing guard.

Red: `dotnet test` failed because `OutcomeRuleConfigurationState` did not exist.

Green command:

```bash
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet test ModifiedItemDrop.Domain.Tests/ModifiedItemDrop.Domain.Tests.csproj -v minimal
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet build ModifiedItemDrop.csproj -v minimal
```

Result: plugin build succeeded with `0 Warning(s), 0 Error(s)`; domain tests `Passed: 43, Failed: 0`.

Review note: `ConfigurationLoader.IsDeathProcessingEnabled` and `SafeModeReason` now reflect v2 Outcome Rules state. `DropService.HandlePlayerDying` returns before `ForceUnequipCurrentItem` or any runtime mutation when safe mode is active. Claim Recovery methods do not use this death-processing guard, preserving recovery availability when storage is healthy.

## Slice 19 — Corrupt Claim storage enters degraded mode

Behavior: if v2 Claim primary storage is corrupt and no trustworthy backup can be loaded, storage artifacts are preserved and the plugin enters Claim storage degraded mode. Degraded mode disables death processing and Claim Recovery rather than trusting an empty in-memory claim list or overwriting corrupt storage.

Red: `dotnet test` failed because `DurableClaimLoadResult` did not expose `IsDegraded` or `ClaimRecoveryEnabled`.

Green command:

```bash
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet test ModifiedItemDrop.Domain.Tests/ModifiedItemDrop.Domain.Tests.csproj -v minimal
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet build ModifiedItemDrop.csproj -v minimal
```

Result: plugin build succeeded with `0 Warning(s), 0 Error(s)`; domain tests `Passed: 44, Failed: 0`.

Review note: plugin load now checks v2 Claim storage health once, wires disabled Claim Recovery with a diagnostic reason when degraded, and prevents death processing through `DropService.SetClaimStorageHealth`. Legacy diagnostics/config commands remain outside this guard.

## Slice 20 — Diagnostics expose safe/degraded runtime state

Behavior: operators can see whether v2 Outcome Rules safe mode or Claim storage degraded mode is active from command feedback, not only server logs. Reload feedback reports death-processing safe-mode state; `/mid status` reports Outcome Rules validity, Claim storage health, and Claim Recovery availability.

Verification command:

```bash
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet test ModifiedItemDrop.Domain.Tests/ModifiedItemDrop.Domain.Tests.csproj -v minimal
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet build ModifiedItemDrop.csproj -v minimal
```

Result: plugin build succeeded with `0 Warning(s), 0 Error(s)`; domain tests `Passed: 44, Failed: 0`.

Review note: this is a temporary M5 diagnostics bridge on the existing `/mid` command. M6 must still replace the v1 flat command surface with the accepted v2 grouped commands and remove v1 aliases before release.

## Slice 21 — Clothing and clothing contents execute v2 outcome actions

Behavior: clothing slots and their contents now use the same v2 `DeathOutcomePlan`/`DeathOutcomeExecutionPlan` as quick-slot inventory. Parent clothing assets and clothing-content assets keep independent runtime actions: each content item can Drop, Delete, or KeepForRestore independently of the parent clothing action. Kept content is restored with the parent clothing when the parent is kept; otherwise it is preserved as inventory pending restore.

Verification command:

```bash
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet test ModifiedItemDrop.Domain.Tests/ModifiedItemDrop.Domain.Tests.csproj -v minimal
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet build ModifiedItemDrop.csproj -v minimal
```

Result: plugin build succeeded with `0 Warning(s), 0 Error(s)`; domain tests `Passed: 45, Failed: 0`.

Review note: `Drop/V2ClothingExecutionAdapter.cs` processes contents before clearing the clothing slot, so content assets are explicitly routed by v2 execution actions instead of being implicitly lost or forced to follow the parent clothing outcome.

## Slice 22 — Respawn Grants execute from v2 Outcome Rules

Behavior: runtime respawn grants now come from v2 `OutcomeRule` entries with `Trigger kind="AfterDeathRespawn"` instead of legacy `DeathSettings.RespawnItems`. `DropService` tracks a death session from death to revive, executes grants only for that tracked session, and uses the domain `DeathSessionRespawnGrantPlanner` to prevent duplicate grants. Failed grant placement is converted to pending restore and routed through v2 Durable Claim or Drop fallback.

Verification command:

```bash
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet test ModifiedItemDrop.Domain.Tests/ModifiedItemDrop.Domain.Tests.csproj -v minimal
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet build ModifiedItemDrop.csproj -v minimal
```

Result: plugin build succeeded with `0 Warning(s), 0 Error(s)`; domain tests `Passed: 45, Failed: 0`.

Review note: `DropService.HandlePlayerRevived` now executes v2 grants after pending restore or, when there are no kept assets, after a lightweight tracked death session created for Grant-trigger rules. Legacy `RestoreManager.GiveRespawnItems` remains in code but is no longer called by the revive path.
