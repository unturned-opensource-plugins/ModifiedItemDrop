# M4 Review Gate — Durable Claim Persistence

- Review base: `dec7cdf`
- Reviewed head: `f1714f2`
- Spec sources:
  - `docs/prd/2026-06-reliability-maintainability-program.md`
- Standards sources:
  - `CONTEXT.md`
  - `docs/adr/0004-canonical-player-asset-outcome-model.md`
  - `docs/migration/v1-to-v2-configuration.md`
  - `docs/work/milestones/m4-durable-claim-persistence.md`

Note: review was performed locally rather than by spawned sub-agents because this session did not have an explicit user instruction to delegate to sub-agents.

## Evidence

Commands:

```bash
git diff --stat dec7cdf...HEAD
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet test ModifiedItemDrop.Domain.Tests/ModifiedItemDrop.Domain.Tests.csproj -v minimal
grep -R "Rocket\|SDG\|Unity\|Steamworks\|RocketMod" -n ModifiedItemDrop.Domain || true
```

Results:

- Domain tests: `Passed: 25, Failed: 0`.
- Domain runtime dependency grep: no Rocket/RocketMod/Unturned/Unity/Steamworks references found.
- M4 diff includes pure domain durable claim storage, fallback planning, tests, migration doc update, and milestone evidence.

## Standards review

Pass.

- `CONTEXT.md` language is followed: Durable Claim, Claim Recovery, Player Asset, Clothing Content, Drop fallback, and immediate restore are used as canonical concepts.
- ADR 0004 is followed: Durable Claim and Drop fallback are projections from Player Asset Outcomes; fallback tests use `PlayerAssetOutcome` inputs rather than reconstructing secondary runtime collections.
- Domain boundary is preserved: new durable claim code is in `ModifiedItemDrop.Domain` and has no Rocket/Unturned/Unity/Steamworks dependency.
- TDD evidence is recorded in `docs/work/milestones/m4-durable-claim-persistence.md` for each slice. A few tests are documented as green-on-add regression coverage after the prior slice already implemented the branch.
- Migration documentation now warns that v1 root `claims.json` is not imported, deleted, or rewritten by v2 storage.

Judgement notes:

- `DurableClaimStore` uses Newtonsoft.Json in the pure domain project. This is acceptable for the current repository because the plugin already uses Newtonsoft.Json and the dependency is not a Rocket/Unturned runtime API.
- `File.Replace` is used for replacing an existing primary file with backup. First-write creation still uses `File.Move` from a temp file, which is appropriate for the initial no-primary case.

## Spec review

Pass for M4 exit criteria.

M4 / R2 / R3 evidence:

- **Claim creation succeeds only after durable write**: `CreateClaimWritesV2StorageWithoutTouchingV1ClaimsJson` verifies `claims/v2/claims.json` exists and reloads the claim; `CreateClaimReturnsFailureWhenDurableWriteCannotComplete` verifies write failure returns `Created=false` and no primary file exists.
- **Claim Recovery removal is durable**: `RemoveClaimDurablyUpdatesV2Storage` removes a claim, reloads storage, and verifies the removed claim does not return.
- **Corrupt primary backup recovery/preservation**: `CorruptPrimaryStorageIsPreservedAndBackupIsLoaded` corrupts primary, verifies backup recovery, preserved corrupt artifact under `claims/v2/corrupt/claims.<timestamp>.json`, and warning text.
- **Write failure routes Player Assets to restore/drop fallback**: `DurableClaimFallbackPlannerTests` cover Drop fallback when immediate restore is unavailable and ImmediateRestore when available. The Drop fallback test includes Clothing Content exactly once.
- **Versioned v2 storage layout**: `V2ClaimStoragePathsExposePrimaryBackupAndCorruptDiagnosticsPaths` verifies primary, backup, and corrupt paths. `CreateClaimWritesV2StorageWithoutTouchingV1ClaimsJson` verifies v1 root `claims.json` remains untouched.
- **V1 Claim migration policy**: `docs/migration/v1-to-v2-configuration.md` now states v2 does not import/delete/rewrite v1 root `claims.json` and operators should resolve/archive v1 Claim data before upgrade.

Non-blocking follow-up items for later PRD milestones:

- Runtime integration still must execute `DropFallback`/`ImmediateRestore` in death, disconnect, unload, respawn overflow, and emergency exception flows.
- Safe/degraded mode still must consume `DurableClaimLoadResult` warnings/recovery state and decide whether Claim Recovery remains available.
- Plugin adapter must map Rocket/Unturned item state into `DurableClaimAsset` without duplicating Player Asset state.

## Review conclusion

M4 passes review gate for the pure domain durable persistence milestone. Continue to M5 runtime integration for death, respawn, disconnect, unload, and emergency fallback flows.
