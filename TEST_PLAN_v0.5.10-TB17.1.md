# TB17.1 Test Plan — Marker Self-Healing

Use a copy of an avatar/project. Do not keep an older StoriesOfYggdrasilOSCContactSystem.cs in the project alongside TB17.1.

## 1. Compile / header

- Replace the older Unity Tool source with TB17.1.
- Confirm Unity reports zero compiler errors.
- Confirm the window header reports `v0.5.10 • TB17.1`.

## 2. Marker repair audit

- Use an avatar previously migrated under TB17.
- Run the managed audit.
- Confirm an old/missing marker is reported as **Unity Tool Compatibility Marker — Repairable**.
- Run **Safe Repair All**.
- Confirm it rebuilds marker parameters/layer instead of saying no safe repairs are required.

## 3. Migrate / Validate

- Run **MIGRATE / VALIDATE AVATAR FOR PROTOCOL 20**.
- Confirm `SoY_UnityToolTB = 17`, `SoY_UnityToolTBRevision = 1`, `SoY_ProtocolVersion = 20`, and `SoY_UnitySchemaValid = true`.
- Confirm `SoY_UnityMarkerBeacon` exists as a local/unsynced Int.

## 4. Periodic marker beacon

- Enter Play Mode / Build & Test.
- Observe `SoY_UnityMarkerBeacon`.
- Confirm it alternates between 117 and 118 approximately every two seconds.
- Confirm the marker layer itself does not activate gameplay Contacts or actions.

## 5. Late Desktop startup

- Load the avatar first.
- Wait until the avatar is fully active.
- Start Desktop v0.8.21-prebuild.4 afterward.
- Confirm Desktop recovers `v0.5.10 TB17.1 / Protocol 20 / Schema valid` from the beacon without requiring an avatar reload.

## 6. Desktop restart

- With the avatar still loaded, close and restart Desktop.
- Confirm compatibility returns to Supported after the next beacon edge.

## 7. Protocol 20 alignment migration

- Confirm `SoY_DamageSourceEnemy` listens only to `SoY Caster Enemy`.
- Confirm `SoY_ExternalDamageSource` receives `Sword`, `Weapon`, and `Hands`.
- On an older mixed receiver, run Safe Repair All and confirm the split occurs automatically while preserving transform/contact volume.

## 8. Friendly / Enemy combat regression

- Friendly Stories attack → Friendly player: must not be reclassified as NPC solely because `Sword` / `Weapon` / `Hands` exists.
- Enemy/NPC canonical attack: `SoY_DamageSourceEnemy` must still classify hostile source correctly.
- External compatibility hit: must remain external/unknown and follow normal attribution.

## 9. Automated action authoring regression

- Rebuild installed Spell/Technick/Item actions with no presentation clips assigned.
- Confirm generated timer presentation is used.
- Confirm functional Contact/Raycast gate timing remains operational.
- Confirm presentation layers do not own `SoY_*Approved`.

## 10. Raycast / world-drop regression

- Confirm Raycast Ready → Armed → Cast → Wait For Release → Recovery → Ready.
- Confirm `SoY_RaycastApproved` only pulses during approved Cast.
- Confirm World Drop still controls `FreezeToWorld` correctly.

## 11. Evasion regression

- Confirm directional evades/rolls remain available.
- Confirm Generic Evade can supply any unassigned direction/roll.

## Known boundary

Unity-local recovery state can still reset when the avatar is reset/reloaded. Persistent authoritative cooldown enforcement remains a later Desktop/Sam.py responsibility. Sam.py is not modified by TB17.1.
