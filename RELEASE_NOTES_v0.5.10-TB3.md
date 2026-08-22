# Stories OSC Unity Tool v0.5.10 TB3

TB3 is the menu/trigger usability pass for the v0.5.10 Raycast test line.

## Installed-only menus

The Menu Builder no longer exposes the complete Spell/Technick/Item catalog by
default. It scans the selected avatar for the tool's managed Contact action
hosts:

- `Stories Spell - <ID> <Name>`
- `Stories Technick - <ID> <Name>`
- `Stories Item - <ID> <Name>`

Only matching installed IDs are emitted as live VRChat action buttons. Empty
Spells/Actions/Targeting/Status sections are omitted from the root menu when
there is nothing usable to put inside them.

## How to fire a Raycast now

1. Install/create the desired Contact or Raycast action.
2. Generate/repair the Stories RP menus.
3. Open **Stories RP -> Targeting** and enable **Targeting Crosshair**.
4. Aim until the local crosshair appears.
5. Fire using the action itself:
   - Spell: press its Spell menu button.
   - Technick: press its Technick menu button.
   - Item: press its Item menu button.
   - Attack/Debuff Direct Impact: press **Projectile Fire** when that button is
     generated.

The targeting toggle is local-only (`SoY_RaycastTargeting`, unsynchronized).
The crosshair also requires VRChat `IsLocal`, a valid Raycast hit, non-KO state,
and an idle action state.

## Hierarchy-aware animations

Raycast Animator clips are generated under a path that mirrors the actual
generated avatar hierarchy:

```text
Assets/Stories Of Yggdrasil/<Avatar>/Animations/Raycasts/<Hierarchy...>/<Action...>/
```

Spell/Technick/Item Raycasts also receive a `Cast_<ID>_<Name>.anim` binding if
that action did not already have a custom animation assigned. The same selector
parameter that fires the Raycast triggers this managed cast-animation state.
Existing custom bindings are preserved.

## TB2 hotfix retained

Filtered selection remains canonical. Searching Regen and generating Spell ID 8
continues to produce Regen ID 8 Contacts, not Cure ID 1.

## Validation limit

Repository/static verification passes. A full Unity Editor + current VRChat SDK
compile/Build & Test is still required before promoting this test build to a
stable release.
