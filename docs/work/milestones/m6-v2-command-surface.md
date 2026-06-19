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

## Slice 2 — Rules preview uses Outcome Rules; diagnostics export has its own route

Behavior: `/mid rules preview [player]` now formats preview lines from `PlayerAssetOutcome` decisions produced by v2 Outcome Rules. It no longer reports legacy `ChanceResolver` source lines or clothing slot chance fields. `/mid diagnostics export` routes separately from `/mid diagnostics status` and reports non-destructive diagnostics, including v2 Claim storage primary/backup/corrupt paths.

Red:

```bash
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet test ModifiedItemDrop.Domain.Tests/ModifiedItemDrop.Domain.Tests.csproj -v minimal --filter OutcomeRuleExplanationFormatterTests
# failed: OutcomeRuleExplanationFormatter did not exist

DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet test ModifiedItemDrop.Domain.Tests/ModifiedItemDrop.Domain.Tests.csproj -v minimal --filter MidCommandRouterTests
# failed: MidCommandRouteKind.DiagnosticsExport did not exist
```

Green command:

```bash
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet test ModifiedItemDrop.Domain.Tests/ModifiedItemDrop.Domain.Tests.csproj -v minimal
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet build ModifiedItemDrop.csproj -v minimal
rg -n "Rocket|Unturned|Unity|Steamworks|SDG" ModifiedItemDrop.Domain ModifiedItemDrop.Domain.Tests -g'*.cs' || true
```

Result: plugin build succeeded with `0 Warning(s), 0 Error(s)`; domain tests `Passed: 56, Failed: 0`; domain boundary scan returned no runtime API matches.

Review note: legacy `ChanceResolver` still exists for old runtime collaborators that have not yet been deleted, but preview no longer calls `DropService.PeekChance`, `ResolveClothingRule`, or `CurrentRuleSet`-based clothing preview logic. Later cleanup slices should remove unused public v1 chance inspection APIs once callers are gone.

## Slice 3 — Rules explain command and command migration docs

Behavior: `/mid rules explain slot <PlayerAssetSlot>` and `/mid rules explain item <itemId>` now route through the v2 command surface. The explanation output is based on `PlayerAssetOutcome.RuleEvaluations`, so it can report matched/missed probabilistic rules, configured chance, sampled roll when present, final Outcome, and the distinction between configured `Keep` and later Durable Claim fallback.

Red:

```bash
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet test ModifiedItemDrop.Domain.Tests/ModifiedItemDrop.Domain.Tests.csproj -v minimal --filter MidCommandRouterTests
# failed: MidCommandRouteKind.RulesExplain did not exist

DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet test ModifiedItemDrop.Domain.Tests/ModifiedItemDrop.Domain.Tests.csproj -v minimal --filter OutcomeRuleExplanationFormatterTests
# failed: OutcomeRuleExplanationFormatter.FormatExplain did not exist

DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet test ModifiedItemDrop.Domain.Tests/ModifiedItemDrop.Domain.Tests.csproj -v minimal --filter MidRulesExplainTargetParserTests
# failed: MidRulesExplainTargetParser did not exist; item target then failed until implemented
```

Green command:

```bash
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet test ModifiedItemDrop.Domain.Tests/ModifiedItemDrop.Domain.Tests.csproj -v minimal
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet build ModifiedItemDrop.csproj -v minimal
rg -n "Rocket|Unturned|Unity|Steamworks|SDG" ModifiedItemDrop.Domain ModifiedItemDrop.Domain.Tests -g'*.cs' || true
```

Result: plugin build succeeded with `0 Warning(s), 0 Error(s)`; domain tests `Passed: 60, Failed: 0`; domain boundary scan returned no runtime API matches.

Docs: README command reference now uses v2 grouped commands, and `docs/migration/v1-to-v2-configuration.md` maps each removed v1 flat command to its v2 replacement. The migration guide explicitly states that diagnostics export is non-destructive and that configured `Keep` is distinct from later Durable Claim fallback.

## Slice 4 — Command authorization policy is testable and grouped

Behavior: command permissions are represented by a pure `MidCommandPermissionPolicy`, mapping every accepted v2 route to a grouped permission. The Rocket command handler reuses the policy constants instead of duplicating string literals.

Red:

```bash
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet test ModifiedItemDrop.Domain.Tests/ModifiedItemDrop.Domain.Tests.csproj -v minimal --filter MidCommandPermissionPolicyTests
# failed: MidCommandPermissionPolicy did not exist
```

Green command:

```bash
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet test ModifiedItemDrop.Domain.Tests/ModifiedItemDrop.Domain.Tests.csproj -v minimal
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet build ModifiedItemDrop.csproj -v minimal
```

Result: domain tests `Passed: 67, Failed: 0`; plugin build succeeded with `0 Warning(s), 0 Error(s)`.

## Slice 5 — Claims list command completes the proposed claims group

Behavior: `/mid claims list [player]` routes independently from Claim Recovery and uses the grouped `modifieditemdrop.claims.list` permission. The runtime command reads v2 Durable Claim records non-destructively and reports a short Claim/Player Asset summary for the target player.

Red:

```bash
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet test ModifiedItemDrop.Domain.Tests/ModifiedItemDrop.Domain.Tests.csproj -v minimal --filter "MidCommandRouterTests|MidCommandPermissionPolicyTests"
# failed: MidCommandRouteKind.ClaimsList did not exist
```

Green command:

```bash
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet test ModifiedItemDrop.Domain.Tests/ModifiedItemDrop.Domain.Tests.csproj -v minimal
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet build ModifiedItemDrop.csproj -v minimal
```

Result: domain tests `Passed: 69, Failed: 0`; plugin build succeeded with `0 Warning(s), 0 Error(s)`.
