# ModifiedItemDrop Codebase Audit

Audit baseline: `8a363b9` (`v1.0.3`)  
Audit date: 2026-06-19  
Scope: static review of C# source, configuration XML, README, release workflow, and repository metadata.  
Policy for this pass: documentation-only; no production code changes.

## Executive summary

The plugin is much safer after `v1.0.3`, especially around Claim cleanup, clothing contents, disabled Claim fallback, and external storage pages. The remaining highest risks are now mostly in persistence durability, domain model complexity, documentation drift, and untested edge cases.

The biggest unresolved correctness risk is `claims.json` persistence: writes are not atomic, save failures are logged but not surfaced, and load failures silently start with an empty in-memory list. That creates both player-asset-loss and duplicate-claim scenarios around crashes, partial writes, disk errors, or corrupted JSON.

The biggest architecture risk is the dual representation of kept clothing contents: clothing contents can live both in `PendingRestore.ClothingContentsToRestore` and in `ClothingItemSnapshot.Contents`. The current code avoids some duplicates by convention, but the model itself remains fragile and has already caused bugs.

The biggest documentation/release risk is license drift: `README.md` advertises MIT while `LICENSE` is GPL text. The release workflow now also emits MIT. That must be resolved before further public distribution.

## Domain glossary candidates

The audit uses the root `CONTEXT.md` language:

- **Player Asset**: in-game item/clothing/content controlled by the plugin outcome.
- **Drop**: becomes a world item.
- **Keep**: selected not to drop and scheduled for restoration or claim.
- **Pending Restore**: temporary kept assets waiting for restoration.
- **Claim**: persisted kept assets that survive disconnect/restart or could not immediately restore.
- **Claim Recovery**: returning assets from Claim.
- **Respawn Item**: configured asset granted after respawn.
- **Clothing Content**: asset stored inside a clothing container.

## Finding index

| ID | Severity | Area | Summary |
|---|---:|---|---|
| P0-001 | Critical | Persistence | Claim storage writes are not atomic and save failure is not observable to callers |
| P0-002 | Critical | Persistence | Corrupted `claims.json` is treated as empty, risking later overwrite of recoverable claims |
| P0-003 | High | Domain model | Kept clothing contents have two sources of truth |
| P0-004 | High | Claim Recovery | Claim clothing content fallback is recovered only on a later claim attempt |
| P1-001 | High | Death flow | `RestoreImmediately` fallback can still leave assets pending without persistence/drop fallback |
| P1-002 | Medium | Death flow | Pending restore is removed after restore attempt even if notification/save path later fails |
| P1-003 | Medium | Respawn | Respawn items are granted on every revive with no cause/session guard |
| P2-001 | High | Legal/release | README/release notes say MIT while LICENSE is GPL |
| P2-002 | Medium | Versioning | Assembly/project version remains `1.0.0` while releases are `v1.0.3` |
| P2-003 | Medium | Release | Release notes are generated from static text, not actual changelog |
| P3-001 | Medium | Config | “default” hands slot config is not actually default for all players |
| P3-002 | Medium | Config | Missing clothing slot rules fall back to `GlobalDefaultChance`, contrary to docs language |
| P3-003 | Low | Config | `IgnoreLimit` exists but is absent from user-facing docs |
| P4-001 | Medium | Commands | Runtime override API exists but no command exposes it |
| P4-002 | Low | Commands | `/mid preview` and `/mid dump` can be very chat-spammy |
| P5-001 | High | Architecture | `DropService` is a god coordinator with unrelated responsibilities |
| P5-002 | High | Architecture | `RestoreManager` mixes restoration, persistence fallback, world-drop fallback, Claim commands, and respawn grants |
| P5-003 | Medium | Architecture | `ClothingProcessor` depends on `InventoryProcessor` for clothing content decisions |
| P5-004 | Medium | Architecture | `PendingRestore` is both a workflow state object and a persistence DTO precursor |
| P6-001 | Medium | Performance | Death processing captures all inventory pages before filtering to pages 0-2 |
| P6-002 | Medium | Performance | Claim storage rewrites the whole JSON file on each add/remove |
| P6-003 | Low | Performance | Frequent LINQ sorting in Claim access paths is acceptable now but scales poorly |
| P7-001 | Medium | Dead code | `ClaimService.RestoreItems` is unused and semantically unsafe |
| P7-002 | Medium | Dead code | Override methods/dictionaries are currently tombstone feature surface |
| P7-003 | Low | Hygiene | Unused local `restMgr` remains in `DropService.HandlePlayerDying` |
| P7-004 | Low | Hygiene | Some comments describe old behavior and obscure current boundaries |
| T-001 | High | Testing | No automated tests around player-asset invariants |

---

## Findings

### P0-001 — Claim storage writes are not atomic and save failure is not observable to callers

- **Severity**: Critical
- **Area**: Persistence / player asset safety
- **Evidence**:
  - `ClaimStorage.SaveInternal` writes directly with `File.WriteAllText(_filePath, json)` at `Claim/ClaimStorage.cs:95-100`.
  - Save exceptions are caught and logged, but no error propagates to `ClaimService.AddClaim`, which still returns a `ClaimRecord` after `_storage.Add(claim)`.
- **Risk**:
  - Crash or disk error during write can corrupt `claims.json`.
  - Caller tells the player that assets were saved even if disk persistence failed.
  - Server restart can lose Claims or resurrect old Claims depending on write timing.
- **Recommended action**:
  - Replace direct write with atomic write: serialize to temp file, flush, replace/move to target, keep `.bak` of previous good file.
  - Make storage `Add/Remove/RemoveRange` return success/failure or throw a domain-specific persistence exception.
  - Treat save failure as “Claim unavailable” so the caller can drop Player Assets rather than pretending they were saved.
- **Regression test**:
  - Simulate `File.WriteAllText` failure and assert pending assets are dropped or operation reports failure.
  - Simulate crash/partial temp write and assert previous valid `claims.json` survives.
- **Refactor phase**: Phase 3 — correctness.

### P0-002 — Corrupted `claims.json` is treated as empty, risking later overwrite of recoverable claims

- **Severity**: Critical
- **Area**: Persistence / recovery
- **Evidence**:
  - `ClaimStorage.Load` catches all exceptions, logs, sets `_claims = new List<ClaimRecord>()`, and rebuilds index at `Claim/ClaimStorage.cs:60-66`.
- **Risk**:
  - A corrupted file may still contain recoverable Claim data, but the plugin discards it in memory.
  - A later save can overwrite the only copy with an empty/new list.
- **Recommended action**:
  - On load failure, move the bad file to `claims.corrupt.<timestamp>.json` and refuse to overwrite it automatically.
  - If a `.bak` file exists, attempt recovery from backup.
  - Surface a loud startup warning indicating Claim Recovery is degraded.
- **Regression test**:
  - Place invalid JSON in storage, start service, add new Claim; assert original corrupt file is preserved and not silently overwritten.
- **Refactor phase**: Phase 3 — correctness.

### P0-003 — Kept clothing contents have two sources of truth

- **Severity**: High
- **Area**: Domain model / player asset safety
- **Evidence**:
  - Kept clothing contents are stored in `PendingRestore.ClothingContentsToRestore` (`Drop/PendingRestore.cs`) and also copied into `ClothingItemSnapshot.Contents` via `BuildKeptClothingSnapshot` at `Drop/ClothingProcessor.cs:147-158`.
  - Persistence skips one source when clothing remains by checking `remainingClothingSlots` in `RestoreManager.SavePendingToClaimOrDrop` at `Drop/RestoreManager.cs:73-96`.
- **Risk**:
  - The current code relies on convention to avoid duplicate persistence/drop.
  - Future maintenance can easily reintroduce duplicate Clothing Content or lost Clothing Content.
- **Recommended action**:
  - Choose a single canonical representation for kept Clothing Content.
  - Recommended: make `PendingRestore` contain explicit `KeptClothing` objects where each clothing item owns its kept contents; remove `ClothingContentsToRestore` as a separate map.
- **Regression test**:
  - Death with clothing kept and mixed contents: one content dropped, one deleted, one kept. Persist Claim and assert only kept content appears exactly once.
- **Refactor phase**: Phase 4 — architecture boundary.
- **ADR?**: Yes, if changing the model. It is hard to reverse, surprising without context, and a real trade-off.

### P0-004 — Claim clothing content fallback is recovered only on a later claim attempt

- **Severity**: High
- **Area**: Claim Recovery UX / correctness
- **Evidence**:
  - `ClaimService.RestoreClothing` adds failed Clothing Content to `fallbackItems` at `Claim/ClaimService.cs:429-467` after `RestoreItemsAndPrune` already ran in `ClaimOldest` / `ClaimAll`.
- **Risk**:
  - A player may see “claimed clothing” but Clothing Content remains in the same Claim as normal items for a later `/mid claim`.
  - This is safe for assets but confusing and easy to report as “missing items”.
- **Recommended action**:
  - After clothing recovery adds fallback items, attempt a second `RestoreItemsAndPrune` pass, or notify explicitly that some content remains claimable.
- **Regression test**:
  - Claim a backpack with contents into a nearly-full inventory where one content fails container placement but could fit general inventory. Assert it is recovered in the same command or message says it remains.
- **Refactor phase**: Phase 3 — correctness.

### P1-001 — `RestoreImmediately` fallback can still leave assets pending without persistence/drop fallback

- **Severity**: High
- **Area**: Death flow failure handling
- **Evidence**:
  - `DropService.HandlePlayerDying` catches processing exceptions and calls `_restoreManager.RestoreImmediately(player, pending)` at `Drop/DropService.cs:145-149`.
  - `RestoreImmediately` only calls inventory/clothing restore inside `SafeExecute`; it does not persist or drop remaining pending assets (`Drop/RestoreManager.cs:172-181`).
- **Risk**:
  - If death processing fails after removing some assets and immediate restore cannot place everything, remaining Player Assets can stay in `pending` but never enter `_pendingRestores`, Claim, or Drop.
- **Recommended action**:
  - Change fallback to: attempt immediate restore, then if `pending` is not empty, call `SavePendingToClaimOrDrop`.
- **Regression test**:
  - Inject failure after one item is removed. Force inventory full on fallback. Assert asset ends in Claim or world drop.
- **Refactor phase**: Phase 3 — correctness.

### P1-002 — Pending restore is removed after restore attempt even if later persistence path fails unexpectedly

- **Severity**: Medium
- **Area**: Death/respawn lifecycle
- **Evidence**:
  - `DropService.HandlePlayerRevived` calls `_restoreManager.RestorePendingItems(player, pending)` then always removes `_pendingRestores[player]` at `Drop/DropService.cs:166-172`.
  - `RestorePendingItems` currently handles normal Claim/drop fallback, but unexpected exceptions are not caught at this boundary.
- **Risk**:
  - A thrown exception from restore/persistence can skip the intended fallback and still lead to handler-level instability.
- **Recommended action**:
  - Wrap restore in a safe boundary that only removes pending after either `pending.IsEmpty` or fallback succeeded/dropped.
- **Regression test**:
  - Inject exception from storage during revive fallback. Assert pending is not lost or assets are dropped.
- **Refactor phase**: Phase 3 — correctness.

### P1-003 — Respawn items are granted on every revive with no cause/session guard

- **Severity**: Medium
- **Area**: Respawn Item semantics
- **Evidence**:
  - `PlayerDeathHandler.OnPlayerRevive` always calls `HandlePlayerRevived`; if no pending death state exists, `DropService.HandlePlayerRevived` calls `GiveRespawnItems` at `Drop/DropService.cs:174-177`.
- **Risk**:
  - Any revive event without pending death state can grant configured Respawn Items, including admin/plugin revives or events outside intended death handling.
- **Recommended action**:
  - Track a death session marker before granting Respawn Items, or explicitly document that every revive grants them.
- **Regression test**:
  - Trigger revive event without preceding death handling; assert Respawn Items are not granted unless intended.
- **Refactor phase**: Phase 3 — correctness / Phase 1 if only documentation.

### P2-001 — README/release notes say MIT while LICENSE is GPL

- **Severity**: High
- **Area**: Legal / distribution
- **Evidence**:
  - `README.md` badge and license section say MIT.
  - `LICENSE` contains GPL text.
  - Release workflow now emits `MIT` in generated notes.
- **Risk**:
  - Consumers receive conflicting license terms.
  - Public releases can be legally ambiguous.
- **Recommended action**:
  - Decide canonical license. Update `LICENSE`, README, badges, and release workflow consistently.
- **Regression test**:
  - CI check greps README/release workflow for the canonical license string and validates `LICENSE` header.
- **Refactor phase**: Phase 1 — no behavior change.

### P2-002 — Assembly/project version remains `1.0.0` while releases are `v1.0.3`

- **Severity**: Medium
- **Area**: Versioning / supportability
- **Evidence**:
  - `ModifiedItemDrop.csproj` has `<Version>1.0.0</Version>`.
  - `ModifiedItemDropPlugin.Load` logs `Assembly.GetName().Version.ToString(3)` at `Plugin/ModifiedItemDropPlugin.cs:53`.
- **Risk**:
  - Server logs can show an older version than the installed GitHub Release.
  - Support/debug reports become unreliable.
- **Recommended action**:
  - Drive assembly/package version from tag in CI or update project version for each release.
- **Regression test**:
  - Build from tag `vX.Y.Z` and assert assembly version/product version matches `X.Y.Z`.
- **Refactor phase**: Phase 1.

### P2-003 — Release notes are generated from static text, not actual changelog

- **Severity**: Medium
- **Area**: Release process
- **Evidence**:
  - `.github/workflows/release.yml` builds `RELEASE_NOTES.md` from hard-coded PowerShell strings.
- **Risk**:
  - Release notes drift from reality, as already happened with `/mid set` and license content.
- **Recommended action**:
  - Maintain `CHANGELOG.md` or generate notes from PR labels/commits. Keep workflow minimal.
- **Regression test**:
  - Release workflow uses existing changelog section for tag and fails if missing.
- **Refactor phase**: Phase 1.

### P3-001 — “default” hands slot config is not actually default for all players

- **Severity**: Medium
- **Area**: Configuration semantics
- **Evidence**:
  - Config comments call `<HandsConfig permission="default" ... />` “默认配置：所有玩家”.
  - Code only applies a config if `player.HasPermission("ModifiedItemDrop.Hands.{permission}")` at `Drop/DropService.cs:277-291`.
- **Risk**:
  - Operators expect all players to get 5x3 hands slots but only players with `ModifiedItemDrop.Hands.default` do.
- **Recommended action**:
  - Either treat `permission="default"` as unconditional fallback, or fix docs to require a permission grant.
- **Regression test**:
  - Player without hands permission joins; assert whether default is applied according to chosen semantics.
- **Refactor phase**: Phase 3 if behavior changes, Phase 1 if doc-only.

### P3-002 — Missing clothing slot rules fall back to `GlobalDefaultChance`, contrary to docs language

- **Severity**: Medium
- **Area**: Configuration semantics
- **Evidence**:
  - Docs say clothing contents are controlled by `ClothingRules` and independent of `GlobalDefaultChance`.
  - `DropRuleSet.ResolveClothingRule` returns `SlotDropChance = GlobalDefaultChance` and `ContentsDropChance = GlobalDefaultChance` when a slot rule is missing.
- **Risk**:
  - Removing a clothing rule may unexpectedly make clothing obey global probability.
- **Recommended action**:
  - Decide canonical behavior: require all clothing rules, fallback to built-in defaults, or fallback to global. Document and test it.
- **Regression test**:
  - Omit backpack rule and assert expected slot/content chances.
- **Refactor phase**: Phase 3.

### P3-003 — `IgnoreLimit` exists but is absent from user-facing docs

- **Severity**: Low
- **Area**: Configuration docs
- **Evidence**:
  - `OverLimitBehavior.IgnoreLimit` exists in `Configuration/ClaimSettings.cs`.
  - README only documents `DeleteOldest` / `DropToGround`.
- **Risk**:
  - Operators cannot discover supported behavior.
- **Recommended action**:
  - Document `IgnoreLimit` or remove it if unsupported.
- **Regression test**:
  - Config deserialization of `IgnoreLimit` and behavior at max claim count.
- **Refactor phase**: Phase 1.

### P4-001 — Runtime override API exists but no command exposes it

- **Severity**: Medium
- **Area**: Commands / tombstone feature
- **Evidence**:
  - `ChanceResolver` and `DropService` expose `SetRegionOverride`, `SetItemOverride`, and clear methods.
  - `ReloadConfigCommand` only implements `reload`, `preview`, `dump`, `claim`.
- **Risk**:
  - Maintainers believe an override feature exists because code and preview mention active overrides, but operators cannot use it.
- **Recommended action**:
  - Either implement `/mid set` / `/mid clear`, or remove override API and preview override output.
- **Regression test**:
  - If kept: command sets item override and preview shows it. If removed: no override code remains.
- **Refactor phase**: Phase 1 or Phase 3 depending on choice.
- **ADR?**: No; reversible feature cleanup.

### P4-002 — `/mid preview` and `/mid dump` can be very chat-spammy

- **Severity**: Low
- **Area**: Commands / UX
- **Evidence**:
  - Commands send each line via chat in loops at `Plugin/ReloadConfigCommand.cs:126-149`.
- **Risk**:
  - Large inventories can flood chat/logs.
- **Recommended action**:
  - Add pagination or console/file dump for large inventories.
- **Regression test**:
  - Dump player with many clothing contents; assert output is paginated or bounded.
- **Refactor phase**: Phase 5.

### P5-001 — `DropService` is a god coordinator with unrelated responsibilities

- **Severity**: High
- **Area**: Architecture
- **Evidence**:
  - `DropService` owns probability overrides, pending restore map, death handling, revive handling, disconnect handling, Claim delegation, config refresh, hands slot resizing, random initialization, and fallback logic.
- **Risk**:
  - Changes to one behavior easily affect another.
  - Testing narrow flows is difficult without constructing the whole service graph.
- **Recommended action**:
  - Split into: `DeathDropCoordinator`, `PendingRestoreRegistry`, `HandsSlotService`, `DropRuleRuntimeOverrides` if overrides remain.
- **Regression test**:
  - Characterization tests around death/respawn/disconnect before splitting.
- **Refactor phase**: Phase 4.
- **ADR?**: Maybe, if introducing a formal death-flow coordinator model.

### P5-002 — `RestoreManager` mixes restoration, persistence fallback, world-drop fallback, Claim commands, and respawn grants

- **Severity**: High
- **Area**: Architecture
- **Evidence**:
  - `RestoreManager` contains `RestorePendingItems`, `SavePendingToClaimOrDrop`, `DropPendingToGround`, `ClaimPending`, `ClaimAllPending`, and `GiveRespawnItems`.
- **Risk**:
  - It is unclear whether `RestoreManager` owns Claim Recovery, fallback policy, or respawn grants.
  - Future fixes can duplicate player assets or suppress notifications by crossing responsibilities.
- **Recommended action**:
  - Split into `PendingRestoreApplier`, `ClaimRecoveryPresenter`, `PendingAssetFallbackPolicy`, `RespawnItemGrantService`.
- **Regression test**:
  - Characterization tests per extracted service.
- **Refactor phase**: Phase 4.

### P5-003 — `ClothingProcessor` depends on `InventoryProcessor` for clothing content decisions

- **Severity**: Medium
- **Area**: Architecture
- **Evidence**:
  - `ClothingProcessor` calls `_inventoryProcessor.HandleClothingContents` at `Drop/ClothingProcessor.cs:50-51`.
- **Risk**:
  - Clothing content behavior is not local to clothing logic.
  - `InventoryProcessor` handles both inventory pages and clothing content, despite different rules.
- **Recommended action**:
  - Extract `ClothingContentProcessor`.
- **Regression test**:
  - Clothing content probability tests remain unchanged after extraction.
- **Refactor phase**: Phase 4.

### P5-004 — `PendingRestore` is both workflow state and persistence DTO precursor

- **Severity**: Medium
- **Area**: Architecture / model clarity
- **Evidence**:
  - `PendingRestore` holds `PendingInventoryItem`, `ClothingItemSnapshot`, and `ClothingContentsToRestore`; later services reinterpret this into Claim records or drops.
- **Risk**:
  - State shape is optimized for both restore and persistence, creating accidental coupling.
- **Recommended action**:
  - Introduce explicit domain outcome models: `KeptAssetSet`, `RestorationPlan`, `ClaimPayload`.
- **Regression test**:
  - Round-trip tests from death outcome to restoration and Claim payload.
- **Refactor phase**: Phase 4.

### P6-001 — Death processing captures all inventory pages before filtering to pages 0-2

- **Severity**: Medium
- **Area**: Performance / safety
- **Evidence**:
  - `PlayerExtensions.CaptureInventory` scans `PlayerInventory.PAGES` for all pages.
  - `InventoryProcessor.ProcessInventory` then ignores pages `> 2` at `Drop/InventoryProcessor.cs:52-60`.
- **Risk**:
  - Unnecessary allocations and iteration on death.
  - The generic capture method encourages future accidental external-storage handling.
- **Recommended action**:
  - Add `CaptureQuickSlots()` for pages 0-2 and use that in death processing.
- **Regression test**:
  - External storage open during death: no external item appears in snapshots used by death processing.
- **Refactor phase**: Phase 5 or Phase 1 if purely additive.

### P6-002 — Claim storage rewrites the whole JSON file on each add/remove

- **Severity**: Medium
- **Area**: Performance / persistence
- **Evidence**:
  - `ClaimStorage.ForceSave` serializes `_claims` and writes entire file.
  - Adds/removes now force-save for safety.
- **Risk**:
  - Safe but increasingly expensive as Claim count grows.
  - Large server can see IO spikes on mass death/claim events.
- **Recommended action**:
  - Keep whole-file atomic writes for now, but add metrics and max claim count guidance.
  - Longer term: per-player files or append-only journal with compaction.
- **Regression test**:
  - Benchmark 1k/10k claims add/remove on CI or local perf script.
- **Refactor phase**: Phase 5.
- **ADR?**: Yes if moving from single JSON to per-player/journal storage.

### P6-003 — Frequent LINQ sorting in Claim access paths is acceptable now but scales poorly

- **Severity**: Low
- **Area**: Performance
- **Evidence**:
  - `GetBySteamId` sorts by `CreatedAt`; `GetOldest` sorts by expiration then created time.
- **Risk**:
  - Repeated allocations/sorts if players accumulate many Claims.
- **Recommended action**:
  - Maintain sorted lists in the index or sort only on mutation if this becomes measurable.
- **Regression test**:
  - Benchmark ClaimAll/ClaimOldest for large per-player Claim lists.
- **Refactor phase**: Phase 5.

### P7-001 — `ClaimService.RestoreItems` is unused and semantically unsafe

- **Severity**: Medium
- **Area**: Dead code
- **Evidence**:
  - `RestoreItems` at `Claim/ClaimService.cs:325-349` restores but does not prune items and lacks null item guards.
  - Current callers use `RestoreItemsAndPrune`.
- **Risk**:
  - Future caller can accidentally restore duplicate items.
- **Recommended action**:
  - Delete `RestoreItems`.
- **Regression test**:
  - Compile-only after removal; Claim Recovery tests cover replacement path.
- **Refactor phase**: Phase 1.

### P7-002 — Override methods/dictionaries are currently tombstone feature surface

- **Severity**: Medium
- **Area**: Dead code / feature drift
- **Evidence**:
  - `ChanceResolver` override dictionaries and `DropService` methods exist.
  - No command or config uses them.
- **Risk**:
  - Maintainers may preserve/test a feature no user can exercise.
- **Recommended action**:
  - Decide keep-or-delete with P4-001.
- **Regression test**:
  - None if deleted; command tests if retained.
- **Refactor phase**: Phase 1 / Phase 3.

### P7-003 — Unused local `restMgr` remains in `DropService.HandlePlayerDying`

- **Severity**: Low
- **Area**: Hygiene
- **Evidence**:
  - `var restMgr = _restoreManager;` at `Drop/DropService.cs:127` is never used.
- **Risk**:
  - Minor confusion; suggests old concurrency plan was abandoned.
- **Recommended action**:
  - Remove it.
- **Regression test**:
  - Compile-only.
- **Refactor phase**: Phase 1.

### P7-004 — Some comments describe old behavior and obscure current boundaries

- **Severity**: Low
- **Area**: Documentation / maintainability
- **Evidence**:
  - Several comments say “claim storage” even when fallback now can drop to ground.
  - README architecture is broad but not precise about current `DeathSettings` and hands permission semantics.
- **Risk**:
  - Future maintainers use comments as truth and reintroduce old bugs.
- **Recommended action**:
  - Update comments after behavior decisions are finalized.
- **Regression test**:
  - Documentation review checklist.
- **Refactor phase**: Phase 1.

### T-001 — No automated tests around player-asset invariants

- **Severity**: High
- **Area**: Testing
- **Evidence**:
  - Repo contains no test project.
  - Logic is mostly coupled to Rocket/Unturned types, making it hard to test without seams.
- **Risk**:
  - Regressions around Drop/Keep/Claim are likely and high-impact.
- **Recommended action**:
  - Add a testable pure domain layer for death outcome calculation.
  - Add characterization tests first using thin fake adapters where possible.
- **Regression test**:
  - Core invariant suite: no duplicate Player Asset IDs/states across Drop + Pending Restore + Claim; no Player Asset disappears unless configured delete applies.
- **Refactor phase**: Phase 2.

---

## Recommended refactor roadmap

### Phase 1 — No-behavior-change cleanup

Goal: reduce drift and remove hazards without changing runtime semantics.

Checklist:

- [ ] Resolve license truth: GPL or MIT, then update `LICENSE`, README, release workflow.
- [ ] Sync project/assembly version with release tags.
- [ ] Delete unused `ClaimService.RestoreItems`.
- [ ] Remove unused `restMgr` local.
- [ ] Decide whether override API is kept; if not, remove tombstone methods and preview output.
- [ ] Document `IgnoreLimit` or remove it.
- [ ] Add `CHANGELOG.md` and stop generating static release notes.

### Phase 2 — Test harness / characterization tests

Goal: freeze current intended behavior before deeper refactors.

Checklist:

- [ ] Create test project targeting domain logic where possible.
- [ ] Introduce adapters/fakes around Unturned `Item`, inventory pages, clothing slots.
- [ ] Add invariant tests: Drop + Keep + Delete outcomes partition Player Assets exactly once.
- [ ] Add Claim storage tests with fake filesystem or temp directory.
- [ ] Add config normalization tests.

### Phase 3 — Correctness hardening

Goal: fix remaining player-asset safety issues.

Checklist:

- [ ] Atomic Claim storage with backup and load-recovery path.
- [ ] Propagate storage save failures to fallback policy.
- [ ] Harden `RestoreImmediately` to persist/drop remaining assets.
- [ ] Guard pending removal on restore/fallback success.
- [ ] Decide respawn item semantics and implement death-session guard if needed.
- [ ] Decide hands `default` semantics and implement/document.
- [ ] Decide missing clothing-rule fallback semantics and implement/document.

### Phase 4 — Architecture boundaries

Goal: make the death flow understandable and testable.

Checklist:

- [ ] Split `DropService` responsibilities.
- [ ] Split `RestoreManager` responsibilities.
- [ ] Extract `ClothingContentProcessor`.
- [ ] Replace `PendingRestore` dual source of truth with a single kept-asset model.
- [ ] Consider ADR for the new kept-asset model.

### Phase 5 — Performance and operational polish

Goal: make behavior scale and support large servers.

Checklist:

- [ ] Add `CaptureQuickSlots()` and stop scanning unused pages on death.
- [ ] Benchmark Claim storage at realistic player/claim counts.
- [ ] Consider per-player Claim files or append-only journal if whole-file JSON becomes expensive.
- [ ] Add pagination/file output for `/mid dump` and `/mid preview`.
- [ ] Add release workflow checks for license/version/changelog consistency.

## Suggested ADR candidates

Only create these if the corresponding decision is actually made:

1. **Kept Player Asset model** — whether to unify Pending Restore and Claim payloads around one canonical `KeptAssetSet`.
2. **Claim persistence strategy** — single atomic JSON vs per-player files vs append-only journal.
3. **Hands default semantics** — permission-gated defaults vs unconditional fallback, only if changing behavior would affect existing servers.

## What not to do yet

- Do not perform a large architecture refactor before tests exist.
- Do not add more death-flow features until storage durability is fixed.
- Do not expose `/mid set` unless runtime overrides are intentionally supported and tested.
- Do not publish another release before license and version drift are resolved.
