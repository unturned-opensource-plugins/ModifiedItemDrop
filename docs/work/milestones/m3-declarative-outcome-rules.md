# M3 — Declarative Outcome Rules

- Fixed diff base: `7689c75`
- PRD: `docs/prd/2026-06-reliability-maintainability-program.md`
- ADRs:
  - `docs/adr/0002-declarative-outcome-rules.md`
  - `docs/adr/0003-nested-xml-outcome-rule-shape.md`

## Scope

M3 implements v2 Outcome Rule resolution semantics in vertical TDD slices: probability roll provider, chance boundaries, priority resolution/conflicts, explicit catch-all fallback validation, XML parsing/rejection, and migration examples.

## Slice 1 — Injectable probability roll provider

Behavior: for a probabilistic Drop rule with `0 < chance < 1`, a sampled roll below chance selects the configured Drop outcome rather than the explicit catch-all Keep rule.

### Slice 1 TDD Evidence

Red: `dotnet test` failed because `DeathOutcomePlanner(IRollProvider)` and `FixedRollProvider` did not exist.

Green command:

```bash
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet test ModifiedItemDrop.Domain.Tests/ModifiedItemDrop.Domain.Tests.csproj -v minimal
```

Green result: `Passed: 3, Failed: 0`.

Review note: the roll provider is injected through the public domain planner API; no Rocket/Unturned runtime dependency was introduced.
