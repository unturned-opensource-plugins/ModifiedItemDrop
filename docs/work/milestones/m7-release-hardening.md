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
