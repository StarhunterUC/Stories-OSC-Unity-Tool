# Changelog

## v0.5.10 TB16 — Evasion & Protocol Compatibility

- Added OSC protocol 19 avatar compatibility markers and Unity Tool build reporting.
- Added `MIGRATE / VALIDATE AVATAR FOR PROTOCOL 19` with fail-closed schema validity publication.
- Added Evasion animation authoring with directional evade/roll states and Generic Evade fallback.
- Added `SoY_EvadeType` and local `SoY_Evading` telemetry.
- Retained TB15 Spell/Technick/Item action gates, approved-action outputs, and Raycast recovery.
- Retained TB15.1 per-avatar Avatar Context persistence and restoration.
- Retained normalized HP/MP/Mist/Curse/Arousal Resource FX with Simple1D Blend Trees.
- Updated the Curse of Diablos staged warning point from 90% to 80%.
- Retained TB12 outside Contact aliases (`Sword`, `Weapon`, `Hands`, `Parry_Detect`).
- Matching Desktop runtime: v0.8.21+, OSC protocol 19.
- Sam.py is unchanged.

## v0.5.10 TB12 — External Contact Compatibility

- Added outside sender aliases: `Sword` and `Weapon` → Average, `Hands` → Weak.
- Added `Parry_Detect` as an additional block/parry-compatible sender tag alongside `Blockable` and `Hit Blocked`.
- Grouped compatible aliases into the existing Weak/Average/block receivers instead of creating competing receivers that write the same Bool.
- External damage aliases drive `SoY_DamageSourceEnemy` so unknown outside hits follow normal hostile/unknown alignment handling.

Older build-by-build history remains available through repository tags/releases.
