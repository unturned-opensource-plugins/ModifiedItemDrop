# M4 — Durable Claim Persistence

- Fixed diff base: `dec7cdf`
- PRD: `docs/prd/2026-06-reliability-maintainability-program.md`

## Scope

M4 implements v2 Durable Claim semantics: versioned storage layout, durable creation/removal, backup/corrupt recovery, and failure signaling so runtime can choose immediate restore or Drop fallback.

## Slice 1 — Durable create writes v2 storage only

Behavior: creating a Durable Claim succeeds only after `claims/v2/claims.json` exists and contains the claim. A v1 root `claims.json` is not imported, overwritten, or treated as active v2 storage.

### Slice 1 TDD Evidence

Red: `dotnet test` failed because `V2ClaimStoragePaths`, `DurableClaimStore`, `DurableClaimRecord`, and `DurableClaimAsset` did not exist.

Green command:

```bash
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet test ModifiedItemDrop.Domain.Tests/ModifiedItemDrop.Domain.Tests.csproj -v minimal
```

Green result: `Passed: 19, Failed: 0`.

Review note: v2 storage layout starts at `<pluginDirectory>/claims/v2/claims.json` and intentionally does not read or write `<pluginDirectory>/claims.json`.

## Slice 2 — Durable Claim Recovery removal

Behavior: removing a recovered claim durably updates `claims/v2/claims.json`; reloading storage after removal must not resurrect the removed claim.

Red: `dotnet test` failed because `DurableClaimStore.TryRemove` did not exist.

Green command:

```bash
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet test ModifiedItemDrop.Domain.Tests/ModifiedItemDrop.Domain.Tests.csproj -v minimal
```

Green result: `Passed: 20, Failed: 0`.

## Slice 3 — Corrupt primary preservation and backup recovery

Behavior: if `claims/v2/claims.json` is corrupt, the corrupt primary file is copied to `claims/v2/corrupt/claims.<timestamp>.json`; if `claims.json.bak` exists, loading recovers from backup and reports `RecoveredFromBackup=true`.

Red: `dotnet test` failed because `DurableClaimLoadResult.RecoveredFromBackup` did not exist and corrupt primary storage was not handled.

Green command:

```bash
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet test ModifiedItemDrop.Domain.Tests/ModifiedItemDrop.Domain.Tests.csproj -v minimal
```

Green result: `Passed: 21, Failed: 0`.

## Slice 4 — Durable Claim creation failure fallback

Behavior: when Durable Claim creation fails and immediate restoration is unavailable, every unresolved kept Player Asset is routed to Drop fallback. Clothing Content is represented as a first-class Player Asset and appears exactly once in fallback decisions.

Red: `dotnet test` failed because `DurableClaimFallbackPlanner` and `DurableClaimFallbackKind` did not exist.

Green command:

```bash
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet test ModifiedItemDrop.Domain.Tests/ModifiedItemDrop.Domain.Tests.csproj -v minimal
```

Green result: `Passed: 22, Failed: 0`.

Review note: this is the pure domain fallback decision. Runtime execution of Drop fallback remains part of the death/disconnect/unload integration milestone.
