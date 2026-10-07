# Stories OSC Unity Tool v0.5.10 TB17.2

## Animator integrity hotfix

TB17.2 repairs the Animator-controller corruption path exposed by TB17.1 managed marker repair.

- Added **REPAIR ANIMATOR INTEGRITY** for controllers already affected by TB17.1.
- Creates a full FX controller backup before Animator cleanup.
- Detects unreachable `AnimatorStateTransition` / `AnimatorTransition` subassets embedded in the controller.
- Removes only transition subassets that are not reachable from any live Animator layer/state machine.
- Validates live default states, transition destinations, child state machines, and orphan transitions before saving.
- Refuses to commit and rolls back when the live Animator graph is still invalid.
- Safe layer removal now destroys only graph subassets that became unreachable because the targeted Stories-managed layer was removed.
- Managed marker publication is deferred until the complete core repair transaction passes its audit.
- The compatibility marker is no longer rebuilt twice during one managed repair transaction.

## Protocol 20 retained

- OSC Protocol remains **20**.
- `SoY_DamageSourceEnemy` remains canonical-only and listens only to `SoY Caster Enemy`.
- `SoY_ExternalDamageSource` remains isolated to `Sword`, `Weapon`, and `Hands` compatibility.
- `SoY_UnityMarkerBeacon` retains the 117/118 alternating heartbeat.
- TB17.2 publishes `SoY_UnityToolTBRevision = 2`.

## Existing TB17 systems retained

- Automated Spell/Technick/Item authoring.
- Generated timer presentation fallback.
- Functional Contact/Raycast approval gates.
- Raycast/world-ground placement.
- Evasion authoring and Generic Evade fallback.
- Resource FX and existing external Contact compatibility.

## Compatibility

- Unity Tool: v0.5.10 TB17.2
- OSC Protocol: 20
- Minimum Desktop test client: v0.8.21-prebuild.4
- Sam.py: unchanged

## Validation boundary

Repository verification and release packaging are automated. A real Unity + current VRChat Avatars SDK compile / Build & Test is still required before promotion beyond prerelease testing.
