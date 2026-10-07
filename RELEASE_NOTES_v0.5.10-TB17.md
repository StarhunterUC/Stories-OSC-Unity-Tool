# Stories OSC Unity Tool v0.5.10 TB17

## Automated Action Authoring

TB17 removes the requirement to create or assign a unique avatar AnimationClip for every Spell, Technick, and Item. Installed managed actions are now discovered from the avatar hierarchy and can be synchronized/rebuilt automatically.

### Functional action automation

- Spell, Technick, and Item Contacts remain controlled by the hardened action gates introduced in TB15/TB16.
- TB17 automatically creates/repairs profile bindings for installed actions.
- Missing presentation clips now receive a generated deterministic timer motion instead of an empty placeholder that looks like required authoring work.
- Managed action hosts receive an `FX — <Kind> Visuals (Place Here)` child. Put optional action VFX below that holder and the existing generated functional gate toggles the parent action automatically.
- Raycast/world-drop functional clips remain generated automatically, including object Active state and `FreezeToWorld` where required.

### Presentation is optional

Functional delivery and avatar presentation are now separate concerns.

Spell presentation resolution order:

1. per-spell override
2. spell-school preset
3. spell-purpose/category preset
4. shared Spell default
5. generated automatic timer

Technick and Item presentation resolution order:

1. per-action override
2. shared kind default
3. generated automatic timer

A blank presentation configuration is valid. The action still works.

### Legacy TB16 animation repair

Empty TB16-generated Raycast `__Cast.anim`/managed placeholder clips are treated as legacy placeholders instead of custom animations. If one of those generated clips was actually edited and contains animation curves, TB17 preserves it as a deliberate override.

### Approval ownership cleanup

Spell/Technick/Item presentation layers no longer write `SoY_*Approved`. Functional Contact/Raycast gate layers are the owners of approval pulses, preventing optional avatar pose layers from fighting the gameplay-facing gate parameter.

### Evasion QoL

`Generic Evade` can now act as the presentation fallback for every unassigned directional evade and roll. Specific directional clips still override the generic fallback.

### Compatibility

- Unity Tool: v0.5.10 TB17
- OSC Protocol: 19
- Minimum Desktop: v0.8.21
- Protocol 20 is unchanged because TB17 changes authoring/presentation behavior, not the Desktop transport contract.
- Sam.py is unchanged.

### Validation boundary

The repository verifier performs structural/source checks and release packaging validation. A real Unity + current VRChat Avatars SDK compile and Build & Test remains required before promotion beyond prerelease testing.

## Protocol 20 alignment repair

- `SoY_DamageSourceEnemy` is now canonical-only and listens only to `SoY Caster Enemy`.
- New unsynced `SoY_ExternalDamageSource` isolates `Sword`, `Weapon`, and `Hands` compatibility.
- Safe Repair All detects the Protocol 19 mixed receiver, preserves its transform/volume, restricts it to canonical alignment, and creates the external-source receiver automatically.
- Repair Existing Action Alignment performs the same migration.
- Protocol 19 avatars are intentionally rejected by the matching Desktop test client until migrated.
- Sam.py is unchanged.
