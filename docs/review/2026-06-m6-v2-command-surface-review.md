# M6 Review Gate — V2 Command Surface and Migration Documentation

Date: 2026-06-19
Fixed diff base: `33e3d74`
Reviewed range: `33e3d74..HEAD`
Milestone evidence: `docs/work/milestones/m6-v2-command-surface.md`
PRD: `docs/prd/2026-06-reliability-maintainability-program.md`
Standards sources: `CONTEXT.md`, `docs/adr/0001-v2-breaking-reliability-redesign.md`, `docs/adr/0002-declarative-outcome-rules.md`, `docs/adr/0003-nested-xml-outcome-rule-shape.md`, `docs/adr/0004-canonical-player-asset-outcome-model.md`, `README.md`, `docs/migration/v1-to-v2-configuration.md`

## Verification commands

```bash
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet test ModifiedItemDrop.Domain.Tests/ModifiedItemDrop.Domain.Tests.csproj -v minimal
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet build ModifiedItemDrop.csproj -v minimal
rg -n "Rocket|RocketMod|Unturned|Unity|Steamworks|SDG" ModifiedItemDrop.Domain ModifiedItemDrop.Domain.Tests -g'*.cs' || true
rg -n "Aliases =>|case \"reload\"|case \"preview\"|case \"dump\"|case \"claim\"|/mid reload|/mid preview|/mid dump|/mid claim|modifieditemdrop\.reload|modifieditemdrop\.preview|modifieditemdrop\.claim" Plugin ModifiedItemDrop.Domain README.md docs/migration/v1-to-v2-configuration.md -g'*.cs' -g'*.md'
```

Results:

- Domain tests: `Passed: 69, Failed: 0`.
- Plugin build: `0 Warning(s), 0 Error(s)`.
- Domain boundary scan: no runtime API matches in `ModifiedItemDrop.Domain` or `ModifiedItemDrop.Domain.Tests`.
- V1 flat command scan found only documented migration examples/hints, the empty alias property, and the v2 `claims` substring false positives.

## Spec review — PRD R7/R8/M6

Pass.

- `/mid` remains the single root command: `Plugin/ReloadConfigCommand.cs` keeps `Name => "mid"` and `Aliases => new List<string>()`.
- Grouped command surface exists through `MidCommandRouter`:
  - `/mid config reload`
  - `/mid rules preview [player]`
  - `/mid rules explain slot <PlayerAssetSlot>`
  - `/mid rules explain item <itemId>`
  - `/mid inventory dump [player]`
  - `/mid claims list [player]`
  - `/mid claims recover [oldest|all]`
  - `/mid diagnostics status`
  - `/mid diagnostics export`
- V1 flat commands are rejected by `MidCommandRouter.RemovedV1Commands` with migration hints for `reload`, `preview`, `dump`, `claim`, and `status`.
- Permission hierarchy is grouped and test-covered by `MidCommandPermissionPolicyTests`.
- Argument validation is test-covered for `/mid rules explain` by `MidRulesExplainTargetParserTests`.
- Outcome Rule inspection no longer uses legacy chance preview behavior: `/mid rules preview` calls `DropService.BuildOutcomeRulePreviewLines`, which formats `PlayerAssetOutcome` decisions; `/mid rules explain` formats `RuleEvaluations`, including chance and sampled roll when available.
- Claim Recovery operations are grouped under `/mid claims`; `/mid claims list` is non-destructive, and `/mid claims recover` supports `oldest` and `all`.
- Diagnostics operations are grouped under `/mid diagnostics`; export reports safe/degraded mode and v2 Claim storage paths without reset/delete behavior.
- README and migration guide map removed v1 commands to v2 replacements.

## Standards review — CONTEXT/ADR/docs

Pass.

- Uses project language from `CONTEXT.md`: Player Asset, Player Asset Outcome, Durable Claim, Claim Recovery, Keep, Drop.
- Follows ADR 0001 by intentionally breaking v1 flat command compatibility and documenting migration.
- Follows ADR 0002/0003 by making command inspection explain declarative Outcome Rules rather than v1 `RuleSet` chance internals.
- Follows ADR 0004 by explaining `Keep` as configured Player Asset Outcome and Durable Claim as a later runtime fallback, not a direct configured rule outcome.
- Keeps domain project pure: new command router, permission policy, target parser, and explanation formatter do not reference Rocket/RocketMod/Unturned/Unity/Steamworks/SDG.
- README command reference now documents v2 grouped commands. Migration guide includes a concrete v1-to-v2 command mapping table.

## Residual risks / follow-up

- `DropService` still contains legacy public chance override APIs and old processors because broader v1 cleanup is not part of M6; later milestones should delete or quarantine tombstone runtime code once no release path depends on it.
- README still has project-level release/license metadata drift inherited from the audit; this is tracked by PRD R14 and must be resolved before final v2.0.0 release, not treated as an M6 blocker.
- `/mid diagnostics export [player]` currently exports global plugin/storage diagnostics; if a future operator need requires player-specific export payloads, add a new TDD slice rather than overloading current global export semantics.

## Gate decision

M6 review gate passes. Proceed to the next milestone focused on remaining v2 cleanup/release-readiness work.
