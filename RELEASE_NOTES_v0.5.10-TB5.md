# Stories OSC Unity Tool v0.5.10 TB5

TB5 adds native **World Drop** behavior to Spell Ground Placement.

## Main change

Regen and other ground-placement spells no longer need to remain attached to the moving Raycast result after they are fired. Once player + floor targeting resolves, the generated placement carrier enables VRChat `VRCParentConstraint.FreezeToWorld` and remains locked at that world position until the Spell button is released (or a future Toggle is disabled).

## Why this implementation

- Uses VRChat's native constraint system; VRCFury and VRLabs are **not required dependencies**.
- The generated constraint always has `[SoY Spell Ground Result]` as a real source.
- `RebakeOffsetsWhenUnfrozen` is enabled for clean repeat placements.
- The world-dropped carrier remains active while only the actual spell payload is shown/hidden.
- Losing the target after placement does not drag the already-cast spell away.
- KO or action release resets/unfreezes the placement.

## TB4/TB3/TB2 retained

- TB4 latched player+ground confirmation.
- TB3 installed-only menus, explicit Targeting Crosshair toggle, selector-triggered Raycasts, and hierarchy-based cast animation assets.
- TB2 filtered-selection fix, including Regen ID 8 generating Regen ID 8 Contacts.

## Migration

Import TB5 and run **Create / Repair** for each existing Spell Ground Placement action. TB5 migrates the existing generated placement object beneath a new `[SoY Spell World Drop] ...` carrier without replacing its user-authored children.

## Validation limit

Repository/static verification is included. A full Unity Editor + current VRChat Avatars SDK compile and Build & Test remains required before stable promotion.
