# Stories OSC Unity Tool v0.5.10 TB16

## Evasion & Protocol Compatibility

TB16 coordinates the Unity authoring side with OSC Desktop v0.8.21 and introduces **OSC protocol 19**.

### Avatar authoring marker

Migrated/validated avatars publish the Unity Tool build and protocol through local, unsynced Avatar Parameters:

- `SoY_UnityToolPresent`
- `SoY_UnityToolMajor` / `Minor` / `Patch`
- `SoY_UnityToolTB` / `TBRevision`
- `SoY_ProtocolVersion`
- `SoY_UnitySchemaValid`

Expected marker for this release: **v0.5.10 TB16 / Protocol 19**.

`SoY_UnitySchemaValid` is not treated as valid until the current managed Contacts/Animator parameters pass the TB16 validation pass. This lets Desktop v0.8.21 reject legacy or incomplete Stories-generated combat/action OSC instead of silently accepting outdated layouts.

### Migration

Use **MIGRATE / VALIDATE AVATAR FOR PROTOCOL 19** on existing avatars. The tool can audit and repair recognized Stories-managed legacy systems, preserve its repair snapshot/rollback workflow, rebuild bridge hooks, and then publish the current compatibility marker.

### Evasion animation builder

Added a dedicated Evasion layer supporting:

1. Evade Forward
2. Evade Backward
3. Evade Left
4. Evade Right
5. Roll Forward
6. Roll Backward
7. Roll Left
8. Roll Right
9. Generic Evade

`SoY_EvadeType` is the synced selector used for remote animation playback. `SoY_Evading` is local telemetry only and does not itself grant invulnerability.

### TB15/TB15.1 systems retained

- Per-avatar Avatar Context persistence.
- Spell, Technick, and Item action gates.
- `SoY_*Approved` local action outputs.
- Raycast recovery state.
- Normalized HP/MP/Mist/Curse/Arousal Resource FX with Simple1D Blend Trees.

### Compatibility retained

TB12 external aliases remain available: `Sword`, `Weapon`, `Hands`, `Blockable`, `Hit Blocked`, and `Parry_Detect`.

### Matching Desktop

Use **Stories Of Yggdrasil OSC Desktop v0.8.21 or newer** for protocol 19 reporting/enforcement. Sam.py is unchanged by this Unity Tool release.

### Validation note

Repository verification checks source parity, TB16 version/protocol metadata, Unity marker parameters, migration UI, Evasion authoring, TB15 approved-action gates, Resource FX, external Contact compatibility, and the current registries. A full Unity + current VRChat Avatars SDK compile/Build & Test is still required before stable promotion.
