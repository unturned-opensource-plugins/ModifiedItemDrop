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
