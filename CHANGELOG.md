## v0.5.10 TB8
- Fixed World Drop release offset: Stories-managed Parent Constraints now keep `Rebake Offsets When Unfrozen` OFF so released spells snap back to the live Raycast result instead of retaining the dropped displacement.

# v0.5.10 TB8

- Fixed World Drop Parent Constraint source assignment on VRChat Avatars SDK 3.10.5-beta.1.
- Uses the documented `VRCParentConstraint` + `VRCConstraintSource` API directly instead of reflecting the `VRCConstraintSourceKeyableList`.
- Verifies the resulting Sources list contains exactly the requested transform before keeping the component.
- Applies the same direct Sources API to normal Stories VRC Parent Constraint attachments.
- Retains TB2-TB6 Raycast/menu/world-drop behavior.

## v0.5.10 TB6

- Fixed VRCParentConstraint discovery on VRChat Avatars SDK 3.10.5-beta.1.
- Added Unity TypeCache and live-component fallbacks for SDK package component discovery.
- Switched Parent Constraint source authoring to the current public Sources API first.
- Explicitly configures all world-drop position/rotation axes and current Freeze/Rebake properties.
- Added clearer constraint diagnostics instead of incorrectly suggesting the SDK is missing.

## v0.5.10 TB5

- Added native VRChat **World Drop** for Spell Ground Placement using `VRCParentConstraint.FreezeToWorld`.
- Regen/ground spells freeze at the confirmed floor position while their Spell button remains held; future toggled actions remain frozen until toggled off.
- Added a real `[SoY Spell Ground Result]` constraint source and enabled `RebakeOffsetsWhenUnfrozen` for repeat-placement safety.
- Once placed, target/floor loss no longer drags the active spell away.
- Releasing the action or entering KO unfreezes and hides the placement.
- Existing TB4 placement hosts migrate under a new `[SoY Spell World Drop] ...` carrier without replacing user-authored children.
- No VRCFury or VRLabs runtime dependency is required; the implementation is native VRChat Constraints.
- Retains TB4 latched placement, TB3 installed-only menu/trigger UX, and TB2 filtered Regen mapping.

## v0.5.10 TB4

- Reworked Spell Ground Placement activation into a latched two-stage Raycast gate.
- Spell buttons now arm a 1.25s placement window instead of requiring selector + player hit + floor hit on one Animator frame.
- Valid placement pulses the managed action host for 0.85s, then waits for selector release.
- Spell targeting crosshair now requires both a remote-player hit and a valid floor hit beneath that target.
- Added clearer notes for `IsLocal` / Gesture Manager preview differences.
- Retains TB2 filtered-action mapping and TB3 installed-only menu/trigger organization.

# Changelog

## [0.5.10 TB3] - 2026-08-22

### Menus

- Generated Spell, Technick, and Item buttons now come only from matching
  Stories-managed Contact actions currently installed in the avatar hierarchy.
- Empty action categories are omitted from the root Stories RP menu.
- Quick Access now ignores favorites whose Contact action is not installed.
- Targeting is only linked when a managed Raycast exists.
- Projectile Fire is only generated for installed Attack/Debuff Direct Impact
  Raycasts that use the manual fire parameter.

### Raycast UX

- Added local unsynchronized `SoY_RaycastTargeting` and a generated **Targeting
  Crosshair** toggle. The crosshair is still restricted by `IsLocal`, valid hit,
  KO state, and cast/fire state.
- Spell/Technick/Item menu buttons are now documented and wired as the actual
  Raycast fire trigger for selector actions.
- Creating/repairing selector Raycasts automatically creates/preserves the
  corresponding managed cast-animation binding and rebuilds its action layer.
- Generated Raycast animation assets now mirror the actual avatar hierarchy
  instead of living in one flat Raycasts folder.

### Preserved fixes

- Includes TB2 filtered-action mapping repair, including Regen ID 8 producing
  Regen Contact children rather than Cure ID 1 when search filters are active.

## [0.5.10 TB2] - 2026-08-22

### Fixed

- Fixed filtered Spell selection creating Contacts from the same numeric index
  in the unfiltered school list. Live example: Regen (ID 8) could generate Cure
  (ID 1) Contact children when `Regen` was the active Search filter.
- Centralized Spell selection so Raycast gate, generated prefix, UI selection,
  and Contact bus all resolve the same Spell definition.
- Applied the same filtered-selection consistency fix to Technicks and Items.
- Added verifier coverage preventing action creation from returning to raw
  unfiltered selection arrays.
- Create / Repair now removes mismatched Stories-managed Spell/Technick/Item
  children from the dedicated managed Raycast action host before rebuilding.

## [0.5.10 TB1] - 2026-08-22

### Raycast fixes

- Reworked Raycast Studio around VRChat's official `VRCRaycast` behavior.
- Added dedicated **Direct Impact** and **Spell Ground Placement** delivery modes.
- Spell placement now aims at a remote player, then casts a second world-only
  ray straight downward to place the effect on the floor beneath the target.
- Ground results use `Apply Rotation` with +Y aligned to the hit normal and a
  generated `FX — Faces Down (Place Particle Here)` child for downward-facing VFX.
- Added a local-only targeting icon driven by `IsLocal`; it disappears during
  the cast/fire action, on target loss, on KO, or on remote avatar instances.
- Remote-player targeting prefers custom Player layer 9 while excluding
  PlayerLocal layer 10.
- Added safe fallback to `Hit Players` when an SDK variation does not expose the
  custom collision-layer member, preventing silent dead raycasts.
- Added a Unity `SerializedObject` fallback for Raycast settings when SDK C# member
  names change, reducing silent configuration failures across SDK revisions.
- Enforced one shared managed Spell Aim Origin per avatar; Create / Repair moves it
  to the newly selected hand/focus and disables exact managed duplicates with Undo support.
- Added targeted TB3 Raycast migration guards that disable matching legacy managed
  origin/result objects before the replacement rig is created, without touching foreign objects.
- Preserved legacy Raycast collision enum preference values when upgrading.

### Organization and usability

- New generated assets are grouped under
  `Assets/Stories Of Yggdrasil/<Avatar>/{FX,Menus,Animations,Profiles,Backups}`.
- Legacy generated asset locations remain recognized without automatic moves.
- Reorganized the Stories RP menu into Combat, Spells, Actions, Targeting,
  Status, and optional Quick Access.
- Rewrote the Help/Raycast instructions around concrete authoring workflows.
- Added `RAYCAST_GUIDE_v0.5.10-TB1.md`.

### Parameter safety

- Required synced expression parameters are now created local-only when the
  256-bit synced parameter budget is exhausted instead of being omitted
  completely. Local menus/OSC/Raycast gating therefore remain functional while
  remote cosmetics are clearly reported as degraded.

### Repository consistency

- Re-synchronized the root canonical C# source with the copy under
  `Assets/Stories Of Yggdrasil/Editor/`.
- Expanded repository verification for Raycast, local-targeting, generated
  workspace, and canonical-source parity.

## [0.5.9 TB3] - 2026-08-05

### Fixed

- Corrected managed repair classification for the compact Spell Contact bus.
- `SoY Spell Active` and `SoY Spell Bit 0-7` are no longer treated as retired
  numeric `SoY Spell <ID>` receivers.
- Legacy spell migration is now limited to numeric suffix tags from 1-255.

### Included

- Managed-system self-healing audit and repair center.
- Transactional migration snapshots and Undo rollback.
- Transform, hierarchy, constraint, and prefab-preservation protections.
- Safe FX copies, official raycast delivery, weapon attachment, action
  animation builders, accessible menu generation, diagnostics, backups, and
  SHA-256-verified updates.
