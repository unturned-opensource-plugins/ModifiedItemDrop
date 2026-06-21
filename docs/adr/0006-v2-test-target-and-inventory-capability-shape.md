# ADR 0006 — V2 test target and Inventory Capability shape

Status: accepted

## Context

The PRD open questions asked for the exact Outcome Rules XML shape, Inventory Capability XML shape, test framework, and assembly target strategy.

Outcome Rules XML was already decided by ADR 0003. The implementation has used xUnit for the pure domain test suite since M1, and the plugin remains a Rocket/Unturned runtime assembly targeting `net48` while testable domain behavior lives in `ModifiedItemDrop.Domain` and `ModifiedItemDrop.Domain.Tests`.

## Decision

- Outcome Rules XML remains the nested ADR 0003 shape.
- The v2 test framework is xUnit for pure domain and repository metadata tests.
- The runtime plugin remains `net48` for v2.0.0; testability is achieved through the separate pure domain project rather than multi-targeting the runtime plugin.
- Inventory Capability configuration for v2.0.0 uses the existing separate XML surface:

```xml
<HandsSlotSettings>
  <Configurations>
    <HandsConfig permission="default" width="5" height="3" />
    <HandsConfig permission="vip" width="8" height="8" />
  </Configurations>
</HandsSlotSettings>
```

This shape is explicitly not an Outcome Rule. It configures inventory capacity/layout, not a Player Asset Outcome.

## Consequences

- Hands slot configuration stays outside `OutcomeRulesXml`.
- The domain `InventoryCapabilityPolicy` owns permission selection, default fallback behavior, minimum dimension clamping, and diagnostics.
- Runtime adapters may translate existing `HandsSlotSettings` into domain `HandsSlotCapabilityRule` objects.
