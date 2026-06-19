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
