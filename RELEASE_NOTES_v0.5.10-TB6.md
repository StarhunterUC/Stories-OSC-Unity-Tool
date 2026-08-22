# Stories OSC Unity Tool v0.5.10 TB6

TB6 is a compatibility hotfix for VRChat SDK 3.10.5-beta.1 Parent Constraints.

## Fixed

- Resolves `VRCParentConstraint` through the documented public namespace, Unity `TypeCache`, existing SDK components, and a resilient assembly scan.
- Configures the current public `Sources` API first instead of assuming a serialized Unity array.
- Supports current `VRCConstraintSource` constructors/properties and older source entry forms through reflection.
- Retains a serialized-property fallback for SDK revisions that expose compatible serialized data.
- Explicitly enables all parent position/rotation axes so `FreezeToWorld` locks the complete placement transform.
- Applies `GlobalWeight`, `IsActive`, `Locked`, `SolveInLocalSpace`, `FreezeToWorld`, and `RebakeOffsetsWhenUnfrozen` through the public component members.
- Calls `ApplyConfigurationChanges()` after configuration as required by the current VRChat Constraint API.
- Improves failure diagnostics: an installed SDK component is no longer incorrectly reported as simply “SDK missing.”

## Retained

- TB5 world-dropped Spell Ground Placement.
- TB4 latched two-stage Raycast placement.
- TB3 installed-only menus and targeting UX.
- TB2 filtered action mapping / Regen ID 8 fix.

## Live report addressed

The tester could manually add a VRChat Parent Constraint in Avatars SDK `3.10.5-beta.1`, including `Freeze To World`, while TB5 reported that it could not create the component. TB6 targets that editor-tool type/source compatibility failure.

A full Unity import/compile and in-game test is still required before stable promotion.
