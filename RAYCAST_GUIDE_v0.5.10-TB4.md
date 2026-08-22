# Raycast Guide — v0.5.10 TB4

## Spell Ground Placement

Spell Ground Placement uses two native VRCRaycast components:

1. **Spell Target Ray** — finds the remote player.
2. **Downward Ground Probe** — starts above the tracked player hit and casts
   downward into the world.

The generated spell result is rotated so local +Y follows the floor normal.
Place your particle/VFX beneath:

`[SoY Spell Placement] Spell_<ID>_<Name>/FX — Faces Down (Place Particle Here)`

## Firing a spell

1. Install/Create the Spell Raycast.
2. Generate/repair the Stories RP menus.
3. Turn on **Stories RP → Targeting → Targeting Crosshair**.
4. Aim at a remote player.
5. The crosshair appears only when both the player and a valid floor beneath
   that player are detected.
6. Press the installed Spell button, such as **Regen**.

TB4 latches that button press for 1.25 seconds. The placement action does not
need the menu button and both Raycast hits to occur on the exact same Animator
frame. Once both hits are valid, the placement object is enabled for a 0.85
second pulse.

## If the crosshair appears but the spell still does not pulse

Inspect the FX Animator parameters while testing:
- `SoY_SpellTarget_Hit`
- `SoY_SpellGround_Hit`
- `SoY_SpellType`

For Regen, `SoY_SpellType` should briefly become `8`.

## Unity preview note

VRChat documents that VRCRaycast itself can be exercised in avatar Play Mode.
The Stories targeting icon also requires the built-in `IsLocal` parameter.
Gesture Manager or other editor emulation may not perfectly reproduce that
local-only condition. If the crosshair works in VRChat but not Gesture Manager,
that alone is not treated as a runtime Raycast failure.

## Direct Impact

Attack, projectile Technick, Item, and Debuff Direct Impact rays remain
single-stage and continue to use their existing generated action/menu trigger.
