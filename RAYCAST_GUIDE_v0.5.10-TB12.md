# Raycast Guide — v0.5.10 TB12

TB12 does not change the current Raycast transport; it adds outside Contact compatibility on top of it.

## Direct Impact

Use Direct Impact for bullets, arrows, beams, projectile-like Technicks/Items and impact debuffs.

1. Select the muzzle/weapon/focus object used as the origin.
2. Choose the action and `Raycast` delivery.
3. Choose `Direct Impact` where the action supports multiple modes.
4. Create / Repair Raycast.
5. Rebuild Stories RP menus.
6. Enable `Targeting Crosshair` when required and trigger the installed action.

## Spell World / Ground Placement

Spell placement uses a shared remote-player target ray and a downward world-only floor probe.

1. Create / Repair the Spell Raycast.
2. Rebuild Stories RP menus.
3. Turn `Stories RP → Targeting → Targeting Crosshair` ON.
4. Aim at a remote player standing over valid world geometry.
5. Press/hold the installed Spell button.
6. The cast arms, waits for both target and floor hits, then activates the placement/world-drop carrier.
7. While held/toggled, `FreezeToWorld` keeps the placed effect fixed in world space.
8. Releasing/turning off the action unfreezes it and returns it to the current ground result at zero offset.

## Technick World / Ground Placement

Technicks can use either Direct Impact or the same World / Ground Placement system as Spells. Switching modes disables the matching old Stories-managed delivery rig so one selector does not drive both.

## Generated animation assets

Raycast clips are kept flat under:

```text
Assets/Stories Of Yggdrasil/<Avatar>/Animations/Raycasts/
```

Stories-generated clip asset names and internal `AnimationClip.name` values are globally unique (`SOY_Raycast_*`).

## TB12 outside-contact interaction

A Raycast-generated action still emits the normal Stories Contact bus. TB12's new `Sword`, `Weapon`, `Hands`, and block/parry aliases affect incoming compatibility only; they do not change Spell/Technick/Item IDs or the Sam.py/Desktop action buses.
