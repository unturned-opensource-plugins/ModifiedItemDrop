# M7 Review Gate — Performance, Release, and Final Review Hardening

Date: 2026-06-19
Fixed diff base: `f386b23a69f168fc687824d3144fd526925f0d1d`
Reviewed range: `f386b23..HEAD`
Milestone evidence: `docs/work/milestones/m7-release-hardening.md`
PRD: `docs/prd/2026-06-reliability-maintainability-program.md`
Standards sources: `CONTEXT.md`, ADRs 0001-0006, README, migration guide, release notes.

## Verification commands

```bash
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet test ModifiedItemDrop.Domain.Tests/ModifiedItemDrop.Domain.Tests.csproj -v minimal
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet build ModifiedItemDrop.csproj -v minimal
rg -n "Rocket|RocketMod|Unturned|Unity|Steamworks|SDG" ModifiedItemDrop.Domain ModifiedItemDrop.Domain.Tests -g'*.cs' || true
rg -n "ChanceResolver|DropRuleSet|DeathSettings|GiveRespawnItems|InventoryProcessor|ClothingProcessor|CurrentRuleSet|RuleSet missing|RegionEntries|CustomItemEntries|ClothingEntries" Drop Configuration Plugin -g'*.cs' || true
rg -n "GNU GENERAL PUBLIC LICENSE|GPL|license-GPL|v1\.0\.0|modifieditemdrop\.reload|modifieditemdrop\.preview|modifieditemdrop\.claim|/mid reload|/mid preview|/mid dump|/mid claim" README.md LICENSE .github/workflows/release.yml ModifiedItemDrop.csproj ModifiedItemDrop.configuration.xml docs/release docs/migration docs/prd/2026-06-reliability-maintainability-program.md -g'*'
```

Results:

- Domain tests: `Passed: 75, Failed: 0`.
- Plugin build: `0 Warning(s), 0 Error(s)`.
- Domain boundary scan: no runtime API matches in domain source/tests.
- Legacy runtime tombstone scan: no live runtime references to `ChanceResolver`, `DropRuleSet`, `DeathSettings`, `GiveRespawnItems`, `InventoryProcessor`, or `ClothingProcessor`.
- License/version scan: remaining v1 flat command references are only PRD/README/migration documentation explaining removed commands, or `/mid claims...` substring matches; no runtime v1 command permissions or aliases remain.

## Spec review

Pass.

- R13 Inventory Capability: `InventoryCapabilityPolicyTests` cover permission selection, default fallback, invalid dimension clamping, and diagnostics. Runtime hands slot resize now uses the pure domain policy and diagnostics export can explain the applied hands slot rule for a target player.
- R14 Release Metadata Integrity: ADR 0005 chooses MIT. `ReleaseMetadataIntegrityTests` enforce MIT license text, README badge/text, project package license, version `2.0.0`, v2-only release workflow defaults, no stale workflow v1 command notes, and explicit release notes path.
- M7 release workflow: `.github/workflows/release.yml` only accepts `v2.x.y` tags, verifies project version/license/release notes, packages README/LICENSE/migration docs, and uses `docs/release/v2.0.0.md` as explicit release notes.
- Packaged configuration: `PackagedConfigurationTests` prove `ModifiedItemDrop.configuration.xml` ships v2 `OutcomeRulesXml`, no `<RuleSet>`, no v1 flat commands, and parseable rules with catch-all fallback.
- Tombstone cleanup: runtime no longer depends on legacy chance/ruleset/death settings processors.

## Standards review

Pass.

- ADR 0005 records the MIT license decision and its release metadata consequences.
- ADR 0006 resolves stale PRD open questions: Outcome Rules XML via ADR 0003, Inventory Capability XML via `HandsSlotSettings/HandsConfig`, xUnit test framework, and runtime `net48` target strategy.
- README, migration guide, and release notes describe v2 command/config migration and MIT license consistently.
- Domain boundary remains pure.

## Residual risks / final review focus

- A final v2.0.0 review still needs to audit R1-R15 as a complete set and explicitly map P0/P1 audit findings to evidence or accepted deferrals.
- The release workflow is not executed locally because GitHub Actions runtime is external; local tests and static workflow checks cover the expected metadata invariants.

## Gate decision

M7 review gate passes. Proceed to final v2.0.0 release review.
