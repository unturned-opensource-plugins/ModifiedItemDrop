# ModifiedItemDrop Reliability & Maintainability PRD

Status: Draft under discussion  
Source audit: `docs/review/2026-06-codebase-audit.md`  
Baseline release: `v1.0.3` / `8a363b9`  
Target release: `v2.0.0` with intentional breaking changes  
Architecture decisions: `docs/adr/0001-v2-breaking-reliability-redesign.md`, `docs/adr/0002-declarative-outcome-rules.md`, `docs/adr/0003-nested-xml-outcome-rule-shape.md`  
Implementation policy: TDD first, then review against this PRD and project standards.

## Product objective

ModifiedItemDrop must preserve Player Asset integrity across death, respawn, disconnect, server restart, configuration reload, and Claim Recovery, while making the codebase testable enough that future changes can be reviewed against explicit behavior rather than tribal knowledge.

## Scope shape

This is a full-program PRD for a v2 redesign. It covers correctness, persistence reliability, configuration semantics, command surface, architecture boundaries, performance, release integrity, and test/review workflow. Work may still be implemented in phases, but the phases are governed by this single PRD. Backward compatibility with v1 configuration and command semantics is not guaranteed; migration guidance is required before release.


## Requirements

### R1 — Player Asset Conservation

Every Player Asset entering death processing must end in exactly one allowed outcome: Drop, Keep/Pending Restore, Claim, Claim Recovery, or configured deletion. No Player Asset may disappear, duplicate, or be reported as safely persisted unless its persistence outcome is durable.

**Acceptance criteria**:

- Death processing partitions Player Assets into allowed outcomes exactly once.
- Clothing Content follows the same conservation rule as top-level Player Assets.
- Configured deletion is explicit and traceable to `DeleteOnDeathItems`.
- Claim persistence failure does not masquerade as a successful Claim outcome.

**TDD expectation**:

- Start with behavior tests for mixed death outcomes: dropped weapon, kept weapon, dropped clothing, kept Clothing Content, deleted configured item.
- Tests must verify observable outcomes, not private implementation structures.


### R2 — Durable Claim Persistence

A Claim exists only after its persisted representation is durably written. Memory-only Claim state is not a valid Claim outcome. If durable persistence fails, affected Player Assets must be immediately restored when possible, otherwise use Drop fallback; they must not be reported as saved.

**Acceptance criteria**:

- Claim creation reports success only after durable persistence succeeds.
- Claim removal during Claim Recovery is durably written before the operation is considered complete.
- A failed write preserves or falls back with the Player Assets rather than silently losing them.
- Corrupted Claim storage is preserved for recovery and must not be silently overwritten by an empty state.
- A previous known-good persisted state can be recovered after partial write or corruption when available.

**TDD expectation**:

- First tests target the public storage/Claim service seam with a temporary filesystem or injectable storage adapter.
- Tests cover successful durable save, write failure, corrupted primary file, backup recovery, and failed removal during Claim Recovery.


### R3 — Persistence Failure Fallback

When a Durable Claim cannot be created or updated, the system must choose an observable fallback rather than keeping Player Assets in an unsafe memory-only state. Drop fallback is the default for unresolved Player Assets unless the player is online and immediate restoration succeeds.

**Acceptance criteria**:

- Disconnect with pending Player Assets: if Durable Claim creation fails, unresolved Player Assets Drop at the death position.
- Plugin unload with pending Player Assets: if Durable Claim creation fails, unresolved Player Assets Drop at the death position.
- Respawn restoration with inventory/clothing overflow: if Durable Claim creation fails, unresolved Player Assets Drop at the death position and the player is notified when possible.
- Emergency restore fallback after death-processing exception: unresolved Player Assets are restored, durably claimed, or dropped; never left only in a local variable.
- Drop fallback must preserve Clothing Content exactly once.

**TDD expectation**:

- Add one failure-mode test at a time: disconnect save failure, unload save failure, respawn overflow save failure, emergency restore failure.
- Each test asserts the final observable Player Asset outcome rather than internal collection contents.


### R4 — Claim Storage Recoverability

The PRD requires recoverable Durable Claims, not a specific long-term storage format. The initial implementation may keep the current single-file `claims.json` storage if it adds atomic writes and a previous-known-good backup. Future implementations may move to per-player files or an append-only journal if scale requires it.

**Acceptance criteria**:

- A successful save cannot leave `claims.json` partially written.
- The previous known-good Claim storage is retained as a backup before replacement.
- On startup, if the primary storage is corrupt, the system attempts backup recovery before accepting an empty Claim state.
- If neither primary nor backup can be loaded, the corrupt artifacts are preserved for operator recovery and the system emits a loud warning.
- The storage format decision remains encapsulated behind the Claim storage boundary.

**TDD expectation**:

- Use temporary files/directories to test atomic replacement and backup recovery.
- Tests should not assume the final storage format beyond public storage behavior.


### R5 — Canonical Player Asset Outcome Model

Death processing must produce a single authoritative Player Asset Outcome model. Restoration, Claim persistence, configured deletion, and Drop fallback must consume that model rather than reconstructing or duplicating Player Asset state through parallel collections.

This is a deliberate full refactor requirement, not a local patch: the current dual representation of kept Clothing Content must be replaced by one canonical outcome graph that can be tested independently of Rocket/Unturned runtime objects.

**Acceptance criteria**:

- Each Player Asset entering death processing receives exactly one Player Asset Outcome.
- Clothing Content is represented in the same outcome model as top-level Player Assets, with its container relationship preserved when relevant.
- Pending Restore, Durable Claim creation, Drop fallback, and configured deletion are projections of the same outcome model.
- No implementation path persists or drops Player Assets from a secondary source of truth.
- The outcome model can be tested without live Rocket/Unturned server state.

**TDD expectation**:

- Start with tests for the outcome model before wiring it into runtime services.
- Use tracer-bullet vertical slices: one death scenario, one outcome model behavior, one runtime integration step.
- Existing behavior must be characterized before replacing the current processors.

**ADR requirement**:

- Create an ADR for the canonical Player Asset Outcome model before implementation begins. The decision is hard to reverse, surprising without context, and trades local patching for a larger model refactor.


## Compatibility policy

ModifiedItemDrop v2 may introduce breaking changes to configuration XML, command syntax, permissions, and documented semantics when doing so improves Player Asset Conservation, Durable Claim behavior, or testability. Compatibility with v1 behavior is a migration concern, not a design constraint.

**Acceptance criteria**:

- Every intentional breaking change is documented in a v1-to-v2 migration guide.
- Deprecated v1 behavior is not preserved unless it supports the v2 domain model.
- v2 release notes clearly state that configuration and command review is required before deployment.
- The review gate treats undocumented compatibility breaks as spec failures.


### R6 — Declarative Outcome Rules

V2 configuration must express death behavior as declarative Outcome Rules. The v1 split across global default chance, region chances, custom item chances, clothing rules, delete-on-death lists, and respawn settings must be replaced or fronted by a coherent rule model aligned with Player Asset Outcomes.

**Acceptance criteria**:

- A rule can target a top-level slot such as primary weapon, secondary weapon, or hands.
- A rule can target a clothing item slot such as backpack, vest, shirt, pants, hat, mask, or glasses.
- A rule can target Clothing Content by source clothing slot.
- A rule can target a specific item ID.
- A rule can express at least these outcomes: Drop by probability, Keep by probability, Delete, and Grant for Respawn Grants. Claim is not a direct rule outcome in v2.0.0.
- Rule priority is explicit and documented; item-specific rules must not be hidden magic.
- V1 configuration fields are not required to remain valid in v2, but migration guidance must map common v1 settings to v2 rules.

**TDD expectation**:

- Start with pure rule-resolution tests independent of Rocket/Unturned runtime.
- Add tests one at a time for slot target, Clothing Content target, item-specific target, Delete outcome, and priority conflict.
- Add at least one migration example test or fixture from v1-style intent to v2 rules.


### R7 — V2 Command Surface

V2 must keep `/mid` as the root command while replacing the flat v1 command surface with grouped subcommands that match the domain model: configuration, rules, inventory diagnostics, Claim Recovery, and diagnostics export.

**Acceptance criteria**:

- `/mid` remains the single root command for plugin operations.
- Configuration operations are grouped under `/mid config ...`.
- Outcome Rule inspection is grouped under `/mid rules ...`.
- Player inventory inspection is grouped under `/mid inventory ...`.
- Claim Recovery operations are grouped under `/mid claims ...`.
- Diagnostic export operations are grouped under `/mid diagnostics ...`.
- V1 flat commands are removed in v2. No compatibility aliases are provided; the migration guide must map each removed v1 command to its v2 replacement.
- Permissions are grouped to match the command hierarchy rather than a single broad preview permission.

**Proposed command shape**:

```text
/mid config reload
/mid rules preview [player]
/mid rules explain <target>
/mid inventory dump [player]
/mid claims list [player]
/mid claims recover [oldest|all]
/mid diagnostics export [player]
```

**TDD expectation**:

- Command tests should verify authorization, argument validation, and domain operation dispatch through public command entrypoints.
- Rules preview/explain tests must assert behavior against Outcome Rules, not legacy chance resolver internals.


### R8 — No V1 Command Aliases

V2 must not provide compatibility aliases for v1 flat commands. Removed commands must fail with usage guidance rather than silently dispatching through old names.

**Acceptance criteria**:

- `/mid reload`, `/mid preview`, `/mid dump`, and `/mid claim` are not accepted as successful v2 commands.
- When possible, invalid v1 command usage returns a concise migration hint pointing to the new command.
- The migration guide maps each removed command to its v2 replacement.
- The implementation must not duplicate old command handlers under deprecated names.

**TDD expectation**:

- Add command tests proving v1 commands fail and v2 commands dispatch correctly.


### R9 — Manual V1 to V2 Configuration Migration

V2 must not automatically migrate v1 configuration. Operators must intentionally rewrite configuration using the v2 Outcome Rules format based on migration documentation. If v2 detects a v1 configuration shape, it must refuse to run death-processing rules rather than guessing a migration.

**Acceptance criteria**:

- V2 ships with a v1-to-v2 migration guide.
- Startup/config reload detects known v1-only configuration shapes and returns a clear error.
- No automatic rewrite of operator configuration occurs.
- A refused v1 configuration must fail safely: the plugin must not partially apply ambiguous rules.
- The migration guide includes examples for global default chance, region chance, custom item chance, clothing slot chance, Clothing Content chance, delete-on-death item, Respawn Grant, Claim settings, and hands slot settings.

**TDD expectation**:

- Config loader tests cover valid v2 config, rejected v1 config, and rejected mixed v1/v2 config.
- Migration examples should be checked as documentation fixtures where practical.


### R10 — TDD Test Architecture

V2 implementation must be test-first. The core test suite must target a pure domain model for Player Assets, Outcome Rules, and Player Asset Outcomes. A smaller characterization suite may cover selected v1 behavior only to prevent accidental regressions during migration, not to freeze the v1 architecture.

**Acceptance criteria**:

- The first tracer bullet test uses a pure domain input: one primary weapon Player Asset and one always-drop Outcome Rule, producing a Drop Player Asset Outcome.
- Core outcome tests do not require a live Rocket/Unturned server.
- Tests verify public behavior and domain outputs, not private collections or method call order.
- Characterization tests are explicitly marked as migration safety tests and may be deleted after v2 behavior is fully specified.
- Every PRD requirement implemented in code has at least one behavior test before or alongside the implementation.

**Initial tracer bullet**:

```text
Given a Player Asset in the primary weapon slot
And an Outcome Rule targeting primary weapon with Drop chance 1.0
When death outcome planning runs
Then the Player Asset Outcome is Drop
```

**Review expectation**:

- Review must reject implementation PRs that add v2 behavior without corresponding behavior tests unless the PR is documentation-only.


### R11 — Review Gates

Every implementation milestone must pass a review gate, and v2.0.0 must pass a final release review. Reviews must evaluate both Spec compliance and Standards compliance.

**Acceptance criteria**:

- Each milestone identifies its fixed diff base before implementation begins.
- Each milestone produces TDD evidence: failing test before implementation where practical, passing test after implementation, and no untested v2 behavior.
- Spec review uses this PRD as the source of truth.
- Standards review uses `CONTEXT.md`, accepted ADRs, README/migration docs, and any future repository standards files.
- Final v2 release review verifies migration docs, versioning, license consistency, release notes, and all P0/P1 audit findings are resolved or explicitly deferred.

**Review commands/process**:

- For each milestone, review the diff from the milestone base to HEAD.
- Run a two-axis review: Standards and Spec.
- Treat undocumented breaking changes as Spec failures.
- Treat behavior without tests as Spec failures unless explicitly documented as untestable with justification.


## Implementation milestones

### M1 — Test infrastructure and pure domain tracer bullet

Establish the test project and the first pure domain behavior test for Player Asset Outcome planning. This milestone proves the TDD path without depending on Rocket/Unturned runtime.

**Exit criteria**:

- Test project exists and runs in CI/local build.
- Initial tracer bullet from R10 passes after red-green cycle.
- Review gate passes against R1, R5, R10, and R11.

### M2 — Canonical Player Asset Outcome model

Build the canonical outcome graph for top-level Player Assets and Clothing Content. Replace parallel kept-content representations in new code paths.

**Exit criteria**:

- ADR for Player Asset Outcome model is accepted.
- Outcome model covers Drop, Keep, Delete, Durable Claim eligibility, and restoration projection.
- Tests prove Player Asset Conservation for mixed top-level and Clothing Content scenarios.

### M3 — Declarative Outcome Rules configuration

Implement v2 Outcome Rules parsing, validation, priority, and explanation. Reject v1 or mixed v1/v2 configuration shapes.

**Exit criteria**:

- Rule resolution tests cover slot, clothing slot, Clothing Content, item-specific, Delete, and priority conflicts.
- V1 config rejection tests pass.
- Migration guide draft includes common v1-to-v2 examples.

### M4 — Durable Claim persistence

Implement Durable Claim semantics, atomic write, backup recovery, corrupted storage preservation, and failure signaling.

**Exit criteria**:

- Claim creation succeeds only after durable write.
- Claim Recovery removal is durable.
- Corrupt primary storage can recover from backup or is preserved with loud warning.
- Write failure routes Player Assets to immediate restore or Drop fallback.

### M5 — Runtime integration for death, respawn, disconnect, unload

Wire the outcome model, rule engine, restoration, Durable Claims, and Drop fallback into Rocket/Unturned event flow.

**Exit criteria**:

- Death, revive, disconnect, unload, and emergency fallback flows satisfy Player Asset Conservation.
- Respawn Item semantics are explicitly implemented and tested.
- External storage pages are not touched by death processing.

### M6 — V2 command surface and migration documentation

Replace v1 flat commands with grouped v2 commands and complete operator migration docs.

**Exit criteria**:

- `/mid config`, `/mid rules`, `/mid inventory`, `/mid claims`, and `/mid diagnostics` command groups exist.
- V1 flat commands fail with migration hints.
- Permissions match command hierarchy.
- Migration guide is complete enough for v1.0.3 operators.

### M7 — Performance, release, and final review hardening

Optimize where needed, clean release metadata, resolve license/version drift, and perform final v2 review.

**Exit criteria**:

- License is consistent across `LICENSE`, README, and release workflow.
- Assembly/project version matches v2 release.
- Release notes come from changelog or explicit release docs.
- Final Spec and Standards review passes.


### R12 — Respawn Grant Rules

Respawn Grants must be modeled as Outcome Rules with an explicit `AfterDeathRespawn` trigger. They execute once per tracked death session and are subject to Player Asset Conservation when grant placement fails.

**Acceptance criteria**:

- Respawn Grants are configured through v2 Outcome Rules, not a separate legacy `RespawnItems` section.
- A Respawn Grant triggers only after a tracked death session reaches respawn.
- A Respawn Grant does not trigger for unrelated revive events without a tracked death session.
- Failed Respawn Grant placement uses immediate restore if possible, then Durable Claim, then Drop fallback according to R2/R3.
- Rules preview/explain can show why a Respawn Grant would be granted.

**TDD expectation**:

- Test one tracked death session producing exactly one Respawn Grant.
- Test revive without tracked death session producing no Respawn Grant.
- Test failed Respawn Grant placement following Durable Claim / Drop fallback semantics.


### R13 — Inventory Capability Rules

V2 must keep the hands slot size feature, but it must be modeled as an Inventory Capability rather than a Player Asset Outcome. Inventory Capability configuration must be separate from Outcome Rules so capacity/layout changes cannot be confused with death outcomes.

**Acceptance criteria**:

- Hands slot size rules remain supported in v2.
- Hands slot size rules are configured outside Outcome Rules.
- Hands slot dimensions are validated and clamped or rejected with a clear error.
- The semantics of a default/fallback hands slot rule are explicit.
- Diagnostics can explain which hands slot rule applied to a player.

**TDD expectation**:

- Test permission-based hands slot rule selection.
- Test default/fallback behavior.
- Test invalid dimensions.


### R14 — Release Metadata Integrity

The PRD does not choose the project license. V2 must not ship while license, version, changelog, README, and release workflow metadata contradict each other.

**Acceptance criteria**:

- `LICENSE`, README license text/badge, release notes, and package metadata use the same chosen license before v2 release.
- Assembly/project version matches the v2 release tag.
- Release notes are generated from explicit changelog/release documentation rather than stale hard-coded workflow text.
- CI or release checklist fails if license/version metadata drift is detected.

**TDD/review expectation**:

- This is primarily a release review requirement. Add lightweight script/check tests if practical.


### R15 — XML Configuration Format

V2 must continue using RocketMod-style XML configuration as the primary operator configuration format. The configuration semantics may change significantly, but the deployment model remains a plugin XML configuration file.

**Acceptance criteria**:

- V2 Outcome Rules are expressible in XML.
- V2 Inventory Capability rules are expressible in XML.
- Invalid XML or invalid v2 schema fails safely with clear diagnostics.
- The migration guide shows v1 XML examples next to v2 XML replacements.

**TDD expectation**:

- Config loader tests use XML fixtures for valid v2, invalid v2, known v1, and mixed v1/v2 shapes.


## Non-goals

- V2 does not guarantee that v1 configuration can be loaded directly.
- V2 does not guarantee that v1 flat commands remain usable.
- V2 does not automatically migrate operator configuration.
- V2 does not model Inventory Capability rules, including hands slot size, as Player Asset Outcomes.
- V2.0.0 does not require a large-scale Claim storage format migration such as per-player files or an append-only journal, as long as Durable Claim and recoverability requirements are satisfied.
- V2 does not preserve undocumented v1 behavior simply because old code happened to behave that way.

## Migration guide requirements

V2 must include an operator-facing migration guide before release. The guide must cover:

- v1 command to v2 command mapping, including removed commands.
- v1 RuleSet fields to v2 Outcome Rules examples.
- v1 clothing rules to v2 clothing item and Clothing Content targets.
- v1 DeleteOnDeathItems to v2 Delete outcome rules.
- v1 RespawnItems to v2 Respawn Grant rules.
- v1 Claim settings to v2 Durable Claim settings.
- v1 hands slot settings to v2 Inventory Capability rules.
- License/version/release metadata changes if any.

## Definition of Done for v2.0.0

- R1 through R15 are implemented or explicitly deferred in an accepted ADR.
- All P0/P1 findings from `docs/review/2026-06-codebase-audit.md` are resolved or explicitly deferred in an accepted ADR.
- The TDD suite includes pure domain tests for Outcome Rules and Player Asset Outcomes.
- The TDD suite includes persistence tests for Durable Claim behavior and corrupted storage recovery.
- V1 configuration is rejected safely with migration guidance.
- V1 flat commands are rejected with migration hints.
- Migration guide exists and is reviewed.
- License and version metadata are consistent.
- Final two-axis review passes: Spec against this PRD; Standards against `CONTEXT.md`, ADRs, README, migration docs, and repository standards.

## Open questions

- Resolved by ADR 0005: v2 canonical license is MIT.
- What exact XML shape should Outcome Rules use?
- What exact XML shape should Inventory Capability rules use?
- What test framework should be used for .NET Framework/RocketMod-adjacent tests?
- Should v2 keep the assembly target as `net48` only, or multi-target for testability?


## Proposed v2 XML shape

Outcome Rules use nested `Rule`, `Target`/`Trigger`, and `Outcome` elements with explicit priority.

```xml
<OutcomeRules>
  <Rule name="Primary weapon drop" priority="100">
    <Target kind="Slot" slot="PrimaryWeapon" />
    <Outcome kind="Drop" chance="0.30" />
  </Rule>

  <Rule name="Keep night vision" priority="1000">
    <Target kind="Item" itemId="1382" />
    <Outcome kind="Keep" />
  </Rule>

  <Rule name="Delete banned item" priority="2000">
    <Target kind="Item" itemId="95" />
    <Outcome kind="Delete" />
  </Rule>

  <Rule name="Backpack content drop" priority="100">
    <Target kind="ClothingContent" slot="Backpack" />
    <Outcome kind="Drop" chance="0.50" />
  </Rule>

  <Rule name="Respawn grant medkit" priority="100">
    <Trigger kind="AfterDeathRespawn" />
    <Outcome kind="Grant" itemId="15" amount="1" quality="100" />
  </Rule>
</OutcomeRules>
```

**Design rules**:

- `priority` is explicit and resolves conflicts; higher numeric priority wins.
- A rule has either `Target` or `Trigger`, depending on whether it applies to an existing Player Asset or an event such as Respawn Grant.
- `Outcome` expresses the Player Asset Outcome or grant.
- `chance` appears only on probabilistic outcomes.
- Rule explanation must cite the matched rule name, priority, target/trigger, and outcome. If multiple rules match, the highest numeric priority wins; ties are invalid configuration unless a later ADR changes this.


## Probability semantics

V2 probability outcomes use explicit boundary semantics:

- `chance <= 0`: outcome never occurs.
- `chance >= 1`: outcome always occurs.
- `0 < chance < 1`: outcome occurs when `roll < chance`.

The outcome planner must use an injectable roll provider so probability behavior can be tested without nondeterminism.

**Acceptance criteria**:

- Tests cover chance `0`, chance `1`, roll below chance, roll equal to chance, and roll above chance.
- `/mid rules explain` can report the rule chance and sampled roll when explaining a probabilistic decision in debug/diagnostic contexts.
- V2 does not preserve v1's `roll <= chance` edge behavior at `chance=0`.


## Keep and Claim semantics

Outcome Rules do not directly produce Claim as a configured outcome in v2.0.0. A rule may Keep a Player Asset; restoration is attempted when appropriate, and Durable Claim is used only as a fallback when kept Player Assets cannot be immediately restored or must survive disconnect/restart.

**Acceptance criteria**:

- Supported v2 rule outcomes are Drop, Keep, Delete, and Grant.
- Claim is a persistence/fallback outcome produced by runtime flow, not direct operator rule configuration.
- `/mid rules explain` distinguishes configured Keep from later Durable Claim fallback.
- Migration documentation explains that v1 “not dropped” behavior maps to Keep, not direct Claim.


## Death Session semantics

A Death Session begins when player death is processed and ends only when all kept Player Assets have reached a terminal allowed outcome: restored to the player, Durable Claim, Drop fallback, or configured deletion. A player disconnecting before respawn does not end responsibility for kept Player Assets.

**Acceptance criteria**:

- Keep means “do not Drop during death processing”; it does not guarantee immediate restoration.
- If a player disconnects during a Death Session, kept Player Assets become a Durable Claim or Drop fallback before the session is forgotten.
- Plugin unload/server shutdown with active Death Sessions follows the same Durable Claim or Drop fallback rule.
- Death Session state must not be the only copy of kept Player Assets after the player session disappears.


## Fallback rule semantics

V2 must not use hidden default behavior for death-processed Player Assets. Configuration must include an explicit catch-all Outcome Rule for unmatched Player Assets. If no valid catch-all rule exists, the configuration is invalid and death-processing rules must fail safely.

**Acceptance criteria**:

- A target kind such as `Any` exists for catch-all death-processed Player Assets.
- Missing catch-all rule is a configuration error.
- The catch-all rule participates in normal priority resolution.
- The migration guide includes example catch-all Keep and catch-all Drop configurations.
- `/mid rules explain` can identify when the catch-all rule was used.

**Example**:

```xml
<Rule name="Default keep" priority="0">
  <Target kind="Any" />
  <Outcome kind="Keep" />
</Rule>
```


## Invalid configuration safe mode

If v2 Outcome Rule configuration is invalid, the plugin must enter safe mode rather than guessing behavior or fully disappearing. Safe mode disables death processing while preserving operator diagnostics and Durable Claim Recovery where possible.

**Acceptance criteria**:

- Invalid Outcome Rules disable death processing.
- Existing Durable Claim Recovery remains available if Claim storage can be loaded safely.
- Config reload and diagnostics commands remain available.
- The server log receives a loud warning explaining why death processing is disabled.
- Players/admins receive concise command feedback when an operation is unavailable due to safe mode.
- Safe mode never applies hidden default Drop/Keep rules.

**TDD expectation**:

- Config loader test for invalid rules produces safe-mode state.
- Death processing test under safe mode proves no Player Assets are modified by the plugin.
- Claim Recovery command test under safe mode remains available when storage is healthy.


## Claim storage degraded mode

If Claim storage cannot be safely loaded from primary storage or backup, the plugin must enter a stricter degraded mode. This mode preserves storage artifacts for operator recovery and disables both death processing and Claim Recovery.

**Acceptance criteria**:

- Corrupted or unreadable Claim storage is preserved and never overwritten automatically.
- If neither primary nor backup storage can be safely loaded, death processing is disabled.
- Claim Recovery is disabled when Claim storage cannot be trusted.
- Config and diagnostics commands remain available.
- Diagnostics clearly identify the Claim storage problem and artifact paths.
- The plugin resumes normal operation only after storage is repaired, replaced, or explicitly reset by an operator action defined in documentation.

**TDD expectation**:

- Storage loader test for corrupt primary and missing/bad backup yields degraded mode.
- Death processing under Claim storage degraded mode modifies no Player Assets.
- Claim Recovery under Claim storage degraded mode fails with a clear diagnostic response.


## Claim storage diagnostics only

V2.0.0 must not provide an in-game command that resets or deletes Claim storage. When Claim storage is degraded, commands may diagnose the problem and identify relevant artifact paths, but destructive repair remains an explicit operator file-system action documented in the migration/operations guide.

**Acceptance criteria**:

- No `/mid` command deletes, resets, or overwrites Claim storage as a repair action in v2.0.0.
- Diagnostics identify primary, backup, and corrupt artifact paths.
- Operations documentation explains how to manually archive/reset storage if an operator chooses to do so.
- Review treats any hidden automatic reset path as a Spec failure.


## V2 Claim storage layout

V2 Claim storage must use a versioned storage directory so it does not overwrite or reinterpret v1 `claims.json` by accident.

**Required layout**:

```text
Rocket/Plugins/ModifiedItemDrop/claims/v2/claims.json
Rocket/Plugins/ModifiedItemDrop/claims/v2/claims.json.bak
Rocket/Plugins/ModifiedItemDrop/claims/v2/corrupt/claims.<timestamp>.json
```

**Acceptance criteria**:

- V2 does not write to the v1 root `claims.json` path.
- V2 diagnostics report the v2 primary, backup, and corrupt storage paths.
- V1 `claims.json` presence does not count as valid v2 Claim storage.
- Migration documentation explains that v1 Claims are not automatically migrated into v2 storage.


## V1 Claim data migration policy

V2.0.0 does not migrate v1 Claim data. Existing v1 `claims.json` remains untouched and is not imported into v2 Claim storage. Operators are responsible for resolving or archiving v1 Claims before upgrading.

**Acceptance criteria**:

- V2 startup does not import v1 `claims.json`.
- V2 startup does not delete or rewrite v1 `claims.json`.
- Migration documentation warns operators to resolve v1 Claims before upgrade.
- Diagnostics may mention that a v1 `claims.json` exists, but must not treat it as active v2 storage.


## Branching policy

V2 work will happen directly on `main`. Because v2 is intentionally breaking, `main` may be unstable during implementation and must not be released as v1.x while v2 work is in progress.

**Acceptance criteria**:

- Release workflow or release checklist prevents accidental v1.x releases from v2-in-progress commits.
- Milestone review bases are recorded before each implementation milestone starts.
- If emergency v1 maintenance is needed, it must branch from the `v1.0.3` tag or another known v1 release tag.
- Documentation clearly states when `main` has entered v2 development.

## Test project architecture

M1 must introduce a separate pure domain project and xUnit test project before v2 runtime integration begins.

```text
ModifiedItemDrop.Domain
ModifiedItemDrop.Domain.Tests
ModifiedItemDrop
```

**ModifiedItemDrop.Domain**:

- Contains the pure v2 domain model for Player Assets, Outcome Rules, Player Asset Outcomes, and Death Outcome planning.
- Must not reference Rocket, RocketMod, Unturned, Unity, Steamworks, or plugin runtime APIs.
- Must expose public behavior-oriented interfaces that tests can exercise without a live server.

**ModifiedItemDrop.Domain.Tests**:

- Uses xUnit.
- Targets modern .NET where practical for fast local and CI execution.
- Starts with the M1 tracer bullet: a primary weapon Player Asset plus a Drop chance `1.0` Outcome Rule produces a Drop Player Asset Outcome.
- Adds tests one behavior at a time using red-green-refactor.

**ModifiedItemDrop**:

- Remains the RocketMod plugin project.
- Integrates the Domain project only after domain behavior is specified by tests.
- Must not pull Rocket/Unturned concepts back into the Domain project.
