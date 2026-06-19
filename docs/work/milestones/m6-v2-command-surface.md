# M6 — V2 Command Surface and Migration Documentation

- Fixed diff base: `33e3d74`
- PRD: `docs/prd/2026-06-reliability-maintainability-program.md`

## Scope

M6 replaces the v1 flat `/mid` command surface with grouped v2 commands:

- `/mid config ...`
- `/mid rules ...`
- `/mid inventory ...`
- `/mid claims ...`
- `/mid diagnostics ...`

V1 flat commands must fail with migration hints. No v1 compatibility aliases are allowed.

## Slice 1 — V1 flat commands are rejected with migration hints

Behavior: `/mid reload`, `/mid preview`, `/mid dump`, and `/mid claim` are no longer successful commands. They return a concise migration hint pointing to the v2 replacement command group.

Red: `dotnet test` failed because `MidCommandRouter` and `MidCommandRouteKind` did not exist; then v2 grouped command tests failed until route parsing was added.

Green command:

```bash
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet test ModifiedItemDrop.Domain.Tests/ModifiedItemDrop.Domain.Tests.csproj -v minimal
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet build ModifiedItemDrop.csproj -v minimal
```

Result: plugin build succeeded with `0 Warning(s), 0 Error(s)`; domain tests `Passed: 54, Failed: 0`.

Review note: Rocket `/mid` now uses the pure `MidCommandRouter`. The old `modifieditemdrop` alias list is empty, and flat v1 forms no longer dispatch to handlers. Initial grouped commands route to existing implementation while later slices replace legacy preview internals with Outcome Rules explain/preview semantics.
