# Stories OSC Unity Tool v0.5.10 TB17.3

## Marker state-name hotfix

Live Unity testing exposed the remaining Protocol 20 marker failure: Unity rejects `/` inside Animator state names. TB17.1/TB17.2 generated slash-delimited marker labels, so the marker could fail to converge even though the controller itself remained intact.

- Added a shared `SanitizeAnimatorStateName` path for all Stories-generated Animator states.
- Protocol marker states now use Unity-safe ` - ` separators.
- Marker status now distinguishes missing, present/outdated, and installed/schema-invalid states.
- Managed repair reports any safe repair that survives its own transaction instead of claiming a clean success.
- Audit logging now prints the exact remaining repairable target.
- TB17.2 controller backup, orphan-transition cleanup, graph validation, and rollback safeguards are retained.

## Protocol 20 retained

- `SoY_UnityToolTBRevision = 3`
- `SoY_UnityMarkerBeacon` retains the 117/118 heartbeat.
- `SoY_DamageSourceEnemy` remains canonical-only.
- `SoY_ExternalDamageSource` remains isolated to outside Sword / Weapon / Hands compatibility.
- Sam.py is unchanged.

## Compatibility

- Unity Tool: v0.5.10 TB17.3
- OSC Protocol: 20
- Minimum Desktop test client: v0.8.21-prebuild.4
- Sam.py: unchanged

## Validation boundary

Repository verification and release packaging are automated. A real Unity + current VRChat Avatars SDK compile / Build & Test is still required before promotion beyond prerelease testing.
