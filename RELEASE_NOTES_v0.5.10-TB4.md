# Stories OSC Unity Tool v0.5.10 TB4

TB4 is a focused Raycast activation reliability update built on TB3.

## Fixed: Spell placement object did not enable

TB3 required the menu selector value, the player-target Raycast hit, and the
downward ground Raycast hit to all be true on the same Animator transition.

That was unnecessarily timing-sensitive for a two-stage Raycast.

TB4 now uses a latched gate:

1. Pressing the installed Spell button arms the cast.
2. The gate remains armed for 1.25 seconds.
3. During that window it waits for both `SoY_SpellTarget_Hit` and
   `SoY_SpellGround_Hit`.
4. Once both are valid, the managed Spell Placement object is enabled for
   a 0.85 second contact/VFX pulse.
5. The layer then waits for the menu parameter to return to zero before it can
   fire again.

This means the inactive `[SoY Spell Placement] Spell_<ID>_<Name>` object no
longer depends on a same-frame coincidence between the menu button and the
Raycast outputs.

## Targeting crosshair means "cast-ready"

For Spell Ground Placement, the local targeting icon now requires both:
- a valid remote-player target, and
- a valid world-floor hit beneath that target.

So the crosshair represents a place where the spell can actually be placed,
not merely a player hit.

## Unity/Gesture Manager note

VRChat documents that VRCRaycast can be tested in avatar Play Mode, but the
local-only targeting icon additionally depends on VRChat's built-in `IsLocal`
Animator parameter. Some editor preview tools do not reproduce `IsLocal`
exactly. In-game behavior remains the authoritative test for the local-only
crosshair.

## Retained fixes

- TB2 filtered Spell/Technick/Item mapping fix.
- Regen remains Spell ID 8 and creates Regen contacts.
- TB3 installed-only menus.
- Explicit Targeting Crosshair toggle.
- Selector buttons as Raycast fire triggers.
- Hierarchy-based Raycast animation asset folders.
- Safe per-avatar generated workspace.

## Validation

Repository/static verifier: PASS.
Canonical source and Assets copy: identical.
A full Unity + current VRChat Avatars SDK compile/test must still be performed
inside Unity before stable promotion.
