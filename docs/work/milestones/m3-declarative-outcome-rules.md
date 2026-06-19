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

## Slice 2 — Equal-priority matching rules are invalid

Behavior: if multiple matching rules share the same highest priority for a Player Asset, the rules are invalid configuration rather than ordered implicitly by collection order.

Red: `dotnet test` failed because `InvalidOutcomeRuleConfigurationException` did not exist.

Green command:

```bash
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet test ModifiedItemDrop.Domain.Tests/ModifiedItemDrop.Domain.Tests.csproj -v minimal
```

Green result: `Passed: 4, Failed: 0`.

Review note: conflict detection happens before probabilistic sampling for a priority group, so ambiguous configuration cannot be hidden behind chance behavior.

## Slice 3 — Missing catch-all is invalid configuration

Behavior: v2 Outcome Rule configuration must include an explicit catch-all rule (`OutcomeTarget.Any()` / XML `Target kind="Any"`). Missing catch-all is an invalid configuration, not a runtime no-match default.

Red: `dotnet test` failed because the planner raised `InvalidOperationException` instead of `InvalidOutcomeRuleConfigurationException` for a ruleset without catch-all.

Green command:

```bash
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet test ModifiedItemDrop.Domain.Tests/ModifiedItemDrop.Domain.Tests.csproj -v minimal
```

Green result: `Passed: 5, Failed: 0`.

Review note: existing tests now include explicit fallback rules where they represent valid v2 rule configuration. This removes the early tracer's temporary one-rule shortcut from final M3 semantics while preserving the original M1 milestone evidence.

## Slice 4 — Hands top-level slot target

Behavior: a top-level Player Asset in the Hands slot can be targeted by an Outcome Rule and resolved independently of the fallback rule.

Red: `dotnet test` failed because `PlayerAssetSlot.Hands` did not exist.

Green command:

```bash
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet test ModifiedItemDrop.Domain.Tests/ModifiedItemDrop.Domain.Tests.csproj -v minimal
```

Green result: `Passed: 6, Failed: 0`.

Review note: this is the v2 Outcome Rule target slot, not the later Inventory Capability implementation for hands slot sizing.

## Slice 5 — Nested XML Outcome Rules parser tracer

Behavior: a nested XML `OutcomeRules` document with `Rule`, `Target`, and `Outcome` elements can be parsed into domain rules and used by the planner. The slice covers `Target kind="Slot"`, `Target kind="Any"`, `Outcome kind="Drop"`, and `Outcome kind="Keep"`.

Red: `dotnet test` failed because `OutcomeRuleXmlParser` did not exist.

Green command:

```bash
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet test ModifiedItemDrop.Domain.Tests/ModifiedItemDrop.Domain.Tests.csproj -v minimal
```

Green result: `Passed: 7, Failed: 0`.

Review note: parser remains in the pure domain layer and produces `OutcomeRule` objects; plugin config loading is still a later adapter step.

## Slice 6 — XML item target with Delete outcome

Behavior: a nested XML rule can target a specific `itemId` and produce a configured `Delete` outcome.

Red: `dotnet test` failed because the parser rejected `Target kind="Item"`.

Green command:

```bash
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet test ModifiedItemDrop.Domain.Tests/ModifiedItemDrop.Domain.Tests.csproj -v minimal
```

Green result: `Passed: 8, Failed: 0`.

Review note: this is the domain-level equivalent of v1 `DeleteOnDeathItems`; migration docs still need an explicit example later in M3.

## Slice 7 — XML ClothingContent target

Behavior: a nested XML rule can target Clothing Content by source clothing slot, distinct from the top-level clothing item itself.

Red: `dotnet test` failed because the parser rejected `Target kind="ClothingContent"`.

Green command:

```bash
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet test ModifiedItemDrop.Domain.Tests/ModifiedItemDrop.Domain.Tests.csproj -v minimal
```

Green result: `Passed: 9, Failed: 0`.

Review note: the parsed rule reuses the canonical `PlayerAsset.ClothingContent` model from M2 rather than creating a side collection.

## Slice 8 — V1 configuration shape rejection

Behavior: a v1-shaped configuration is rejected with explicit v1/v2/migration guidance instead of being parsed, guessed, or failing with an opaque root mismatch.

Red: `dotnet test` failed because the exception message was only `Expected root element OutcomeRules.` and did not mention v1/v2/migration.

Green command:

```bash
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet test ModifiedItemDrop.Domain.Tests/ModifiedItemDrop.Domain.Tests.csproj -v minimal
```

Green result: `Passed: 10, Failed: 0`.

Review note: this is parser-level rejection. Plugin-level safe mode wiring remains a later milestone.

## Slice 9 — Sampled roll retained for explanations

Behavior: when a probabilistic rule is sampled, the resulting Player Asset Outcome retains the sampled roll so `/mid rules explain` can later report both configured chance and sampled roll.

Red: `dotnet test` failed because `PlayerAssetOutcome.SampledRoll` did not exist.

Green command:

```bash
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet test ModifiedItemDrop.Domain.Tests/ModifiedItemDrop.Domain.Tests.csproj -v minimal
```

Green result: `Passed: 11, Failed: 0`.

Review note: deterministic roll inspection stays in the pure domain output; command formatting remains a later adapter concern.

## Slice 10 — Probability boundary: chance zero

Behavior: `chance <= 0` never applies and the planner continues to the explicit fallback rule. No sampled roll is recorded for a never-occurring boundary rule.

Result: this test passed immediately because Slice 1's probability implementation already used the PRD boundary semantics. It is retained as explicit regression coverage for the v2 `chance=0` behavior, which intentionally differs from v1 `roll <= chance` edge behavior.

Verification command:

```bash
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec PATH=/opt/homebrew/opt/dotnet@8/bin:$PATH dotnet test ModifiedItemDrop.Domain.Tests/ModifiedItemDrop.Domain.Tests.csproj -v minimal
```

Result: `Passed: 12, Failed: 0`.
