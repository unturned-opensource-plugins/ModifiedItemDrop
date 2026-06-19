# ModifiedItemDrop v1 to v2 Configuration Migration Guide

Status: draft for v2.0.0 implementation

ModifiedItemDrop v2 does **not** automatically migrate v1 configuration. If v2 detects a v1-shaped configuration, death-processing rules must fail safely until operators intentionally rewrite configuration using v2 nested Outcome Rules.

## Required v2 shape

V2 death behavior is configured under explicit nested Outcome Rules:

```xml
<OutcomeRules>
  <Rule name="Default keep" priority="0">
    <Target kind="Any" />
    <Outcome kind="Keep" />
  </Rule>
</OutcomeRules>
```

Every valid death-processing ruleset must include an explicit catch-all rule such as `Target kind="Any"`. There is no hidden default Drop or Keep behavior.

## Global default chance

V1 intent: globally drop items with a default chance.

V2 example:

```xml
<OutcomeRules>
  <Rule name="Global default drop chance" priority="10">
    <Target kind="Any" />
    <Outcome kind="Drop" chance="0.30" />
  </Rule>

  <Rule name="Default keep" priority="0">
    <Target kind="Any" />
    <Outcome kind="Keep" />
  </Rule>
</OutcomeRules>
```

Probability semantics changed in v2: `chance <= 0` never occurs, `chance >= 1` always occurs, and partial probability uses `roll < chance`.

## Region chance

V1 intent: change drop behavior by region.

V2 status: region targeting is not implemented in the current pure domain milestone. Do not preserve v1 region fields in v2 configuration. If region targeting is accepted later, it must be represented as an explicit `Target` or contextual rule condition with documented priority.

## Custom item chance

V1 intent: item `1382` has custom keep/drop behavior.

V2 example:

```xml
<OutcomeRules>
  <Rule name="Keep night vision" priority="1000">
    <Target kind="Item" itemId="1382" />
    <Outcome kind="Keep" />
  </Rule>

  <Rule name="Default drop" priority="0">
    <Target kind="Any" />
    <Outcome kind="Drop" chance="1.0" />
  </Rule>
</OutcomeRules>
```

Item-specific behavior is not magic in v2; it is just a higher-priority explicit rule.

## Clothing slot chance

V1 intent: apply behavior to a top-level clothing item such as backpack, vest, shirt, pants, hat, mask, or glasses.

V2 example for backpack item itself:

```xml
<OutcomeRules>
  <Rule name="Keep backpack item" priority="100">
    <Target kind="Slot" slot="Backpack" />
    <Outcome kind="Keep" />
  </Rule>

  <Rule name="Default drop" priority="0">
    <Target kind="Any" />
    <Outcome kind="Drop" chance="1.0" />
  </Rule>
</OutcomeRules>
```

## Clothing Content chance

V1 intent: apply behavior to the items inside a clothing container.

V2 example for backpack contents:

```xml
<OutcomeRules>
  <Rule name="Backpack content drop chance" priority="100">
    <Target kind="ClothingContent" slot="Backpack" />
    <Outcome kind="Drop" chance="0.50" />
  </Rule>

  <Rule name="Default keep" priority="0">
    <Target kind="Any" />
    <Outcome kind="Keep" />
  </Rule>
</OutcomeRules>
```

Clothing Content is represented in the canonical Player Asset Outcome graph, not as a separate side collection.

## DeleteOnDeathItems

V1 intent: delete banned item `95` on death.

V2 example:

```xml
<OutcomeRules>
  <Rule name="Delete banned item" priority="2000">
    <Target kind="Item" itemId="95" />
    <Outcome kind="Delete" />
  </Rule>

  <Rule name="Default keep" priority="0">
    <Target kind="Any" />
    <Outcome kind="Keep" />
  </Rule>
</OutcomeRules>
```

Configured deletion is explicit and traceable to a v2 `Delete` outcome rule.

## RespawnItems

V1 intent: grant an item after respawn.

V2 example shape:

```xml
<OutcomeRules>
  <Rule name="Respawn grant medkit" priority="100">
    <Trigger kind="AfterDeathRespawn" />
    <Outcome kind="Grant" itemId="15" amount="1" quality="100" />
  </Rule>
</OutcomeRules>
```

Respawn Grants are event-triggered rules and must execute only for a tracked death session. Runtime grant placement failure must follow immediate restore, Durable Claim, then Drop fallback according to the PRD.

## Claim settings

V1 Claim behavior does not map to a direct v2 rule outcome. In v2, operators configure rules such as `Keep`; Durable Claim is a runtime persistence/fallback projection when kept Player Assets cannot be immediately restored or must survive disconnect/restart.

V1 “not dropped” behavior usually maps to:

```xml
<Rule name="Default keep" priority="0">
  <Target kind="Any" />
  <Outcome kind="Keep" />
</Rule>
```


## V1 Claim data

V2 Claim storage uses `claims/v2/claims.json` and does not import, delete, or rewrite the v1 root `claims.json`. Operators should resolve, claim, or archive existing v1 Claim data before upgrading to v2. Diagnostics may mention that a v1 `claims.json` exists, but it is not active v2 storage.

## Hands slot settings

V1 hands slot size/placement behavior maps to v2 Inventory Capability rules, not Outcome Rules. Outcome Rules may still target `slot="Hands"` for death behavior, but hands slot capacity is a separate capability model.

## Removed command aliases

V2 uses `/mid` as the single command root. V1 flat aliases must not be reintroduced. The old `modifieditemdrop` command alias is removed. V1 flat commands fail with a migration hint instead of dispatching to old handlers.

| v1 command | v2 replacement | Notes |
|------------|----------------|-------|
| `/mid reload` | `/mid config reload` | Requires `modifieditemdrop.config.reload`. |
| `/mid preview [player]` | `/mid rules preview [player]` | Preview now reports v2 Outcome Rule decisions, not legacy chance sources. |
| `/mid dump [player]` | `/mid inventory dump [player]` | Inventory diagnostics moved under the inventory group. |
| `/mid claim` | `/mid claims recover oldest` | v2 Durable Claim recovery is explicit. Use `/mid claims list [player]` to inspect pending v2 Claims and `/mid claims recover all` to recover all pending v2 Claims. |
| `/mid status` | `/mid diagnostics status` | Safe/degraded mode status moved under diagnostics. |
| n/a | `/mid rules explain slot <PlayerAssetSlot>` | Explains the matched rule, configured chance, sampled roll when available, and final Outcome for a synthetic slot target. |
| n/a | `/mid rules explain item <itemId>` | Explains the matched rule for a synthetic ItemID target. Item-specific rules take priority according to configured `priority`. |
| n/a | `/mid diagnostics export` | Non-destructive diagnostic export; reports v2 Claim primary/backup/corrupt paths and does not reset or delete storage. |

`/mid rules explain` distinguishes configured `Keep` from later runtime fallback: `Keep` means the rule decided to preserve the Player Asset; Durable Claim is only used later when immediate restore is unavailable or unsafe.


## License and release metadata

V2.0.0 uses the MIT License as the canonical project license. This resolves the v1 metadata drift where README/release notes and `LICENSE` disagreed. The v2 release workflow checks that README, `LICENSE`, project package metadata, release notes, and the release tag version stay aligned before publishing.
