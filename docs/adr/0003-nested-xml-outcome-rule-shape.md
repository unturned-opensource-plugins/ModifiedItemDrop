# Nested XML outcome rule shape

Status: accepted

V2 Outcome Rules will use a nested XML shape with `Rule`, `Target` or `Trigger`, and `Outcome` elements rather than a flat attribute-only rule. This makes rule priority, target selection, event triggers, and final Player Asset Outcomes explicit while staying compatible with RocketMod-style XML configuration.

## Consequences

The v2 configuration is more verbose than v1 and requires migration documentation, but `/mid rules explain` and pure rule-resolution tests can reason about the same explicit structure operators see in XML.
