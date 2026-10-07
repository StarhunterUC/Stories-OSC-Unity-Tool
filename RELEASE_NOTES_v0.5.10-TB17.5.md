# Stories OSC Unity Tool v0.5.10 TB17.5

## Marker metadata contract

TB17.5 replaces compatibility-marker identity parsing from Animator state names with direct Avatar Parameter Driver metadata validation.

- Marker states now use fixed Unity-safe names: `SoY Marker Beacon A`, `SoY Marker Beacon B`, and `SoY Marker INVALID`.
- Current Unity Tool build, Protocol 20, schema-validity, and 117/118 beacon values are validated from the local `VRCAvatarParameterDriver` itself.
- Marker validity also verifies the A→B and B→A transitions and the default marker state.
- Generated state-name sanitization remains enabled and emits diagnostics if a generated name ever needs correction.
- TB17.4 bridge-parameter restoration and explicit schema diagnostics are retained.
- TB17.2 controller backup, orphan-transition cleanup, graph validation, and rollback safeguards are retained.

## Protocol 20 retained

- `SoY_UnityToolTBRevision = 5`
- `SoY_UnityMarkerBeacon` retains the 117/118 heartbeat.
- `SoY_DamageSourceEnemy` remains canonical-only.
- `SoY_ExternalDamageSource` remains isolated to outside Sword / Weapon / Hands compatibility.
- Sam.py is unchanged.

## Compatibility

- Unity Tool: v0.5.10 TB17.5
- OSC Protocol: 20
- Minimum Desktop test client: v0.8.21-prebuild.4
- Sam.py: unchanged

## Validation boundary

Repository verification and release packaging are automated. A real Unity + current VRChat Avatars SDK compile / Build & Test is still required before promotion beyond prerelease testing.
