# Stories OSC Unity Tool v0.5.10 TB8

TB8 fixes World Drop release offsets.

## Fixed

- World-dropped Spell Ground Placement objects now return directly to the live `[SoY Spell Ground Result]` when the spell Button is released or Toggle is disabled.
- `Rebake Offsets When Unfrozen` is intentionally **disabled** on Stories-managed World Drop Parent Constraints.
- The original zero offset is preserved across Freeze To World cycles, preventing the carrier from continuing to follow the Raycast with the displacement from the first world drop.
- The TB7 direct `VRCParentConstraint` / `VRCConstraintSource` API remains unchanged.
- TB2 filtered-action mapping, TB3 installed-only menus, TB4 latched placement, and TB5–TB7 World Drop behavior are retained.

## Expected Regen behavior

1. Aim at a valid target/floor and activate Regen.
2. The World Drop carrier freezes in place.
3. Move away: the placement remains fixed.
4. Release Regen: Freeze To World turns off.
5. The carrier snaps back onto the current `[SoY Spell Ground Result]` with zero offset and is ready for the next cast.

A full Unity + VRChat SDK compile/Build & Test is still required before stable promotion.
