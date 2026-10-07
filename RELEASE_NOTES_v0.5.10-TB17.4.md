# Stories OSC Unity Tool v0.5.10 TB17.4

## Marker convergence hotfix

TB17.4 closes the remaining compatibility-marker repair loop exposed after TB17.3.

- Current INVALID marker states are recognized correctly even after the legal state-name separator change.
- Marker repair now restores missing Stories bridge Animator and Expression parameters before deciding whether the schema is valid.
- Exact core-schema failures are logged when the marker must remain INVALID.
- TB17.3 legal Animator state names are retained.
- TB17.2 full-controller backup, orphan-transition cleanup, graph validation, and rollback protections are retained.

## Protocol 20 retained

- `SoY_UnityToolTBRevision = 4`
- `SoY_UnityMarkerBeacon` retains the 117/118 heartbeat.
- `SoY_DamageSourceEnemy` remains canonical-only.
- `SoY_ExternalDamageSource` remains isolated to outside Sword / Weapon / Hands compatibility.
- Sam.py is unchanged.

## Compatibility

- Unity Tool: v0.5.10 TB17.4
- OSC Protocol: 20
- Minimum Desktop test client: v0.8.21-prebuild.4
- Sam.py: unchanged

## Validation boundary

Repository verification and release packaging are automated. A real Unity + current VRChat Avatars SDK compile / Build & Test is still required before promotion beyond prerelease testing.
