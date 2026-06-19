# V2 breaking reliability redesign

Status: accepted

ModifiedItemDrop will use the audit findings as the basis for a v2 reliability redesign rather than a v1-compatible patch series. We accept breaking changes to configuration and command semantics because the current shape has accumulated ambiguous behavior, tombstone feature surface, and fragile Player Asset state modeling; the v2 work should favor explicit Player Asset Outcomes, Durable Claims, and testable behavior over backward compatibility.

## Considered Options

- Keep v1 compatibility and only patch correctness issues.
- Allow small semantic breaks while preserving most configuration and commands.
- Redesign configuration and commands for v2, with migration guidance.

## Consequences

Existing server operators may need to migrate configuration and command usage. The PRD must include a migration section, explicit compatibility non-goals, and release gating that prevents accidental v1-style behavior from being preserved only because old code happened to work that way.
