# Stories OSC Unity Tool v0.5.10 TB17.1

## Marker self-healing

TB17.1 fixes the Protocol 20 compatibility-marker gap exposed during live Desktop testing.

- Added local unsynced `SoY_UnityMarkerBeacon` as an encoded heartbeat.
- The marker alternates between values `117` and `118` while the avatar is running.
- Desktop v0.8.21-prebuild.4 can reconstruct the current Unity Tool / Protocol / schema identity from that beacon even when Desktop starts after the avatar.
- Safe Repair All now treats the Unity Tool compatibility marker as a managed repair target.
- Missing/outdated marker parameters or marker layer are reported as **Repairable** instead of incorrectly returning “No safe automatic repairs are required.”
- Migrate / Validate rebuilds the marker and beacon before publishing schema validity.

## Protocol 20 alignment repair retained

- `SoY_DamageSourceEnemy` remains canonical-only and listens only to `SoY Caster Enemy`.
- `SoY_ExternalDamageSource` remains isolated to `Sword`, `Weapon`, and `Hands` compatibility.
- Safe Repair All and Repair Existing Action Alignment continue to split older mixed receivers automatically while preserving their transform/contact volume.

## TB17 automated authoring retained

- Spell, Technick, and Item presentation remains optional.
- Installed managed actions remain auto-discovered.
- Generated timer presentation remains the fallback when no presentation clip is assigned.
- Raycast/world-drop functional clips remain generated automatically.
- Functional Contact/Raycast gate layers remain the owners of approved-action pulses.
- Generic Evade remains available as the fallback for unassigned directional evades and rolls.

## Compatibility

- Unity Tool: v0.5.10 TB17.1
- OSC Protocol: 20
- Minimum Desktop test client: v0.8.21-prebuild.4
- Sam.py: unchanged

## Validation boundary

Repository verification and release packaging are automated. A real Unity + current VRChat Avatars SDK compile / Build & Test is still required before promotion beyond prerelease testing.
