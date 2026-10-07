# Changelog

## v0.5.10 TB17.1 — Marker Self-Healing

- Added `SoY_UnityMarkerBeacon`, an encoded local heartbeat alternating between 117/118.
- Desktop v0.8.21-prebuild.4 can recover Tool/Protocol/schema identity even when it starts after the avatar.
- Safe Repair All now audits the Unity compatibility marker and reports missing/outdated marker state as Repairable.
- Migrate / Validate rebuilds the marker before publishing schema validity.
- Marker repair installs required Animator + Expression parameters and the periodic marker layer.
- Retains Protocol 20 canonical/external damage-source separation from TB17.
- Sam.py is unchanged.


## v0.5.10 TB17 — Automated Action Authoring

- Installed Spell/Technick/Item actions are now auto-discovered and synchronized into the animation profile.
- Functional Contact/Raycast authoring is separated from optional avatar presentation animation.
- Missing presentation clips use generated deterministic timer motions instead of empty required-looking placeholders.
- Added shared Spell, Technick, and Item presentation defaults.
- Added Spell school and purpose/category presentation presets plus per-spell overrides.
- Added managed `FX — <Kind> Visuals (Place Here)` holders so generated action gates toggle optional child VFX automatically.
- Empty TB16-generated Raycast Cast placeholders are migrated away; edited non-empty generated clips are preserved as intentional overrides.
- Presentation layers no longer write `SoY_*Approved`; functional Contact/Raycast gate layers own approval pulses.
- `Generic Evade` can automatically supply every unassigned directional evade/roll animation.
- Retains OSC protocol 20 / Desktop v0.8.21-prebuild.2 compatibility.
- Sam.py is unchanged.

## v0.5.10 TB16 — Evasion & Protocol Compatibility

- Added OSC protocol 20 avatar compatibility markers and Unity Tool build reporting.
- Added `MIGRATE / VALIDATE AVATAR FOR PROTOCOL 20` with fail-closed schema validity publication.
- Added Evasion animation authoring with directional evade/roll states and Generic Evade fallback.
- Added `SoY_EvadeType` and local `SoY_Evading` telemetry.
- Retained TB15 Spell/Technick/Item action gates, approved-action outputs, and Raycast recovery.
- Retained TB15.1 per-avatar Avatar Context persistence and restoration.
- Retained normalized HP/MP/Mist/Curse/Arousal Resource FX with Simple1D Blend Trees.
- Updated the Curse of Diablos staged warning point from 90% to 80%.
- Retained TB12 outside Contact aliases (`Sword`, `Weapon`, `Hands`, `Parry_Detect`).
- Matching Desktop runtime: v0.8.21+, OSC protocol 20.
- Sam.py is unchanged.

## v0.5.10 TB12 — External Contact Compatibility

- Added outside sender aliases: `Sword` and `Weapon` → Average, `Hands` → Weak.
- Added `Parry_Detect` as an additional block/parry-compatible sender tag alongside `Blockable` and `Hit Blocked`.
- Grouped compatible aliases into the existing Weak/Average/block receivers instead of creating competing receivers that write the same Bool.
- External damage aliases drive `SoY_DamageSourceEnemy` so unknown outside hits follow normal hostile/unknown alignment handling.

Older build-by-build history remains available through repository tags/releases.
