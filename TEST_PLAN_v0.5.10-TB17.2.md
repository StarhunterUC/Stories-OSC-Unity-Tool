# TB17.2 Test Plan — Animator Integrity Hotfix

Use a copy of an avatar/project first. Do not keep an older StoriesOfYggdrasilOSCContactSystem.cs in the project alongside TB17.2.

## 1. Compile / header

- Replace the older Unity Tool source with TB17.2.
- Confirm Unity reports zero compiler errors.
- Confirm the window header reports `v0.5.10 • TB17.2`.
- Confirm Protocol remains `20`.

## 2. TB17.1 damaged-controller recovery

- Open an avatar whose FX controller reports `Broken ext PPtr` / missing local file identifiers after TB17.1 marker repair.
- Run **REPAIR ANIMATOR INTEGRITY** before Safe Repair All.
- Confirm a full controller backup is written under `Backups/Animator Integrity`.
- Confirm only unreachable `AnimatorStateTransition` / `AnimatorTransition` subassets are removed.
- Confirm the live graph passes validation before SaveAssets.
- Confirm the previous `Broken ext PPtr` errors disappear after reimport.

## 3. Integrity refusal / rollback

- Introduce or use a test controller with a live transition whose destination state is genuinely missing.
- Run the integrity repair.
- Confirm TB17.2 refuses to commit when a live graph error remains.
- Confirm Undo rollback occurs and the controller backup remains available.

## 4. Managed marker repair transaction

- Use an avatar with an outdated/missing Unity compatibility marker.
- Run the managed audit and confirm the marker is reported **Repairable**.
- Run **Safe Repair All**.
- Confirm marker parameter repair occurs first.
- Confirm marker layer publication is deferred until the core managed audit passes.
- Confirm the marker is rebuilt once inside the transaction.
- Confirm no orphan transition subassets remain afterward.

## 5. Migrate / Validate

- Run **MIGRATE / VALIDATE AVATAR FOR PROTOCOL 20**.
- Confirm `SoY_UnityToolTB = 17`, `SoY_UnityToolTBRevision = 2`, `SoY_ProtocolVersion = 20`, and `SoY_UnitySchemaValid = true`.
- Confirm `SoY_UnityMarkerBeacon` remains a local/unsynced Int.
- Confirm the 117/118 alternating beacon behavior is retained.

## 6. Late Desktop startup / restart

- Load the avatar first, then start the supported Desktop runtime.
- Confirm the Protocol 20 marker is rediscovered after the beacon changes.
- Restart Desktop while the avatar remains loaded and confirm compatibility returns without an avatar reload.

## 7. Protocol 20 alignment regression

- Confirm `SoY_DamageSourceEnemy` listens only to `SoY Caster Enemy`.
- Confirm `SoY_ExternalDamageSource` receives `Sword`, `Weapon`, and `Hands`.
- Confirm Safe Repair All preserves transform/contact volume when repairing older mixed receivers.

## 8. Automated action / Raycast / Evasion regression

- Rebuild installed Spell/Technick/Item actions with no presentation clips assigned and confirm generated timer presentation is used.
- Confirm functional Contact/Raycast gates remain the owners of `SoY_*Approved`.
- Confirm Raycast Ready → Armed → Cast → Wait For Release → Recovery → Ready.
- Confirm World Drop still controls `FreezeToWorld`.
- Confirm directional evades/rolls and Generic Evade remain available.

## Known boundary

Repository verification and release packaging are automated. A real Unity + current VRChat Avatars SDK compile / Build & Test is still required before promotion beyond prerelease testing. Sam.py is not modified by TB17.2.
