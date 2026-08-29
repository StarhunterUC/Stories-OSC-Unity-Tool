# Changelog

## v0.5.10 TB12 — External Contact Compatibility

- Added outside sender aliases: `Sword` and `Weapon` → Average, `Hands` → Weak.
- Added `Parry_Detect` as an additional block/parry-compatible sender tag alongside `Blockable` and `Hit Blocked`.
- Grouped compatible aliases into the existing Weak/Average/block receivers instead of creating competing receivers that write the same Bool.
- External damage aliases drive `SoY_DamageSourceEnemy` so unknown outside hits follow normal hostile/unknown alignment handling.
- Managed repair upgrades older Stories receivers in place and consolidates redundant managed receivers.
- No Sam.py or Desktop transport change is required; aliases normalize into the existing OSC contract.
- Cleaned the repository working tree so historical TB artifacts live in Git history/releases instead of the current head.

## Current v0.5.10 test-line features retained

- Raycast Direct Impact and Spell/Technick World / Ground Placement.
- Native VRChat Parent Constraint World Drop with zero-offset reset on release.
- Installed-only generated menus and local Targeting Crosshair.
- Flat, globally unique generated Raycast animation assets.
- Status gauge bridge parameters for Mist, Curse of Diablos, and Arousal.
- Managed-system audit/repair and safe FX-copy protection.

Older build-by-build history remains available through repository tags/releases.
