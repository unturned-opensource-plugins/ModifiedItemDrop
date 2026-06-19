# Declarative outcome rules for v2 configuration

Status: accepted

ModifiedItemDrop v2 configuration will use declarative Outcome Rules instead of preserving the v1 split between region chances, custom item chances, clothing slot rules, and death settings. This aligns configuration with the canonical Player Asset Outcome model: every rule answers how a Player Asset should resolve, with explicit targets, outcomes, probabilities, and priority.

## Considered Options

- Keep the v1 `RuleSet` shape and only rename or normalize fields.
- Introduce v2 declarative Outcome Rules and provide migration guidance from v1 configuration.

## Consequences

The v2 XML configuration will be a breaking change. The implementation must include migration documentation and behavior tests for rule priority, item-specific overrides, slot targets, Clothing Content targets, and configured deletion.
