# M3 Review Gate — Declarative Outcome Rules

- Review base: `7689c75`
- Reviewed head: `f7c0af5`
- Spec sources:
  - `docs/prd/2026-06-reliability-maintainability-program.md`
  - `docs/adr/0002-declarative-outcome-rules.md`
  - `docs/adr/0003-nested-xml-outcome-rule-shape.md`
- Standards sources:
  - `CONTEXT.md`
  - `docs/adr/0002-declarative-outcome-rules.md`
  - `docs/adr/0003-nested-xml-outcome-rule-shape.md`
  - `docs/migration/v1-to-v2-configuration.md`
  - `docs/work/milestones/m3-declarative-outcome-rules.md`

Note: review was performed locally rather than by spawned sub-agents because this session did not have an explicit user instruction to delegate to sub-agents.

## Evidence

Commands:

```bash
git diff --stat 7689c75...HEAD
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet test ModifiedItemDrop.Domain.Tests/ModifiedItemDrop.Domain.Tests.csproj -v minimal
grep -R "Rocket\|SDG\|Unity\|Steamworks\|RocketMod" -n ModifiedItemDrop.Domain || true
```

Results:

- Domain tests: `Passed: 18, Failed: 0`.
- Domain runtime dependency grep: no Rocket/RocketMod/Unturned/Unity/Steamworks references found.
- M3 diff includes pure domain parser/planner code, xUnit tests, milestone evidence, and migration guide draft.

## Standards review

Pass.

- `CONTEXT.md` language is followed: tests and domain types use Player Asset, Player Asset Outcome, Clothing Content, Respawn Grant, Keep, Drop, Durable Claim eligibility vocabulary.
- ADR 0002 is followed: v2 rules are declarative, explicit, prioritized, and tested as domain behavior rather than legacy chance resolver internals.
- ADR 0003 is followed: XML uses nested `Rule`, `Target`/`Trigger`, and `Outcome`; parser rejects rules containing both `Target` and `Trigger`.
- TDD evidence is recorded in `docs/work/milestones/m3-declarative-outcome-rules.md` as vertical slices. Two boundary tests were green-on-add because earlier slices had already implemented the behavior; they are documented as regression coverage rather than claimed as fresh red-green cycles.
- Migration guide draft exists and documents v1-to-v2 examples for global default chance, region chance status, custom item chance, clothing slot chance, Clothing Content chance, DeleteOnDeathItems, RespawnItems, Claim settings, hands slot settings, catch-all rules, and removed command aliases.

Judgement notes:

- `OutcomeRuleXmlParser` is intentionally simple and domain-local. It may later be wrapped by plugin config loading/safe-mode code rather than becoming the plugin loader itself.
- `RespawnGrantPlanner` currently models configured grant projection only. Tracked Death Session gating belongs to runtime integration, not this pure parser milestone.

## Spec review

Pass for M3 exit criteria.

M3 PRD exit criteria:

- **Outcome Rules parsing/validation/priority/explanation**: implemented through `OutcomeRuleXmlParser`, `DeathOutcomePlanner`, priority conflict validation, explicit fallback validation, Target/Trigger shape validation, mixed v1/v2 rejection, chance attribute validation, and `OutcomeRuleEvaluation` trace for explanation.
- **Rule resolution tests cover slot**: primary weapon and hands slot tests pass; top-level backpack clothing item is covered by canonical outcome model and migration docs.
- **Clothing Content target**: domain and XML tests pass for `Target kind="ClothingContent" slot="Backpack"`.
- **Item-specific target + Delete**: XML test passes for `Target kind="Item" itemId="95"` + `Outcome kind="Delete"`.
- **Priority conflicts**: equal-priority matching rules throw `InvalidOutcomeRuleConfigurationException`.
- **Probability semantics**: tests cover chance `0`, chance `1` via the original always-drop tracer, roll below chance, roll equal to chance, and roll above chance. Roll equal/above preserve missed-rule sampled roll in evaluation trace.
- **V1 config rejection**: known v1 shape is rejected with v1/v2/migration guidance; mixed v1/v2 under `OutcomeRules` is rejected.
- **Migration guide draft**: `docs/migration/v1-to-v2-configuration.md` covers the PRD-required common mappings.

Non-blocking follow-up items for later PRD milestones:

- Plugin config loader still needs to route invalid rules into safe mode and keep diagnostics/Claim Recovery available.
- `/mid rules explain` command formatting is not implemented yet; M3 only provides domain-level explanation data.
- Respawn Grant execution must later be gated by a tracked Death Session and handle grant placement failure with restore/Durable Claim/Drop fallback.
- Region targeting is documented as not implemented in this pure domain milestone; if accepted later, it needs an explicit target/condition model and tests.

## Review conclusion

M3 passes review gate for the pure domain/config milestone. Continue to M4 Durable Claim Persistence.
