# Stories OSC Unity Tool v0.5.10 TB17.1

Current prerelease: **Stories OSC Unity Tool v0.5.10 TB17.1 — Automated Action Authoring**.

This repository contains the Unity Editor authoring/repair tool for the Stories Of Yggdrasil OSC Contact System used by VRChat avatars.

## Install

Download the release `Stories-OSC-Unity-Tool-v0.5.10-TB17.1.unitypackage`, or copy the canonical script to:

```text
Assets/Stories Of Yggdrasil/Editor/StoriesOfYggdrasilOSCContactSystem.cs
```

Only one `StoriesOfYggdrasilOSCContactSystem` script should exist inside a Unity project's `Assets` folder.

## TB17.1 — automated authoring + marker self-healing on protocol 20

TB17 retains the protocol 20 fail-closed marker introduced in TB16 and adds automatic Spell/Technick/Item authoring without changing the Desktop transport contract.

After **MIGRATE / VALIDATE AVATAR FOR PROTOCOL 20** succeeds, the avatar publishes these local/unsynced parameters:

- `SoY_UnityToolPresent`
- `SoY_UnityToolMajor`
- `SoY_UnityToolMinor`
- `SoY_UnityToolPatch`
- `SoY_UnityToolTB`
- `SoY_UnityToolTBRevision`
- `SoY_ProtocolVersion`
- `SoY_UnitySchemaValid`
- `SoY_UnityMarkerBeacon` — encoded TB17.1 heartbeat (117/118) for late Desktop discovery

For this build the expected authoring marker is **v0.5.10 TB17.1 / Protocol 20**. `SoY_UnitySchemaValid` is only published as true after the current managed schema validates. TB17.1 periodically alternates `SoY_UnityMarkerBeacon` between `117` and `118`, allowing the Desktop client to rediscover the current marker even when it starts after the avatar.

The matching Desktop line is **v0.8.21-prebuild.4+**. Older, unmarked, invalid, or future protocol Stories-generated gameplay input is handled fail-closed by the Desktop runtime. Sam.py is not changed by TB17.1.

### TB17.1 marker repair

Safe Repair All now treats the Unity compatibility marker as a managed repair target. Missing/outdated marker parameters or marker layers are reported as Repairable and rebuilt automatically. Migrate / Validate performs the same marker repair before publishing schema validity.

## Evasion animation authoring

TB17 retains the dedicated Evasion animation builder and adds automatic Generic Evade fallback for any unassigned direction/roll:

- Forward / Backward / Left / Right evade
- Forward / Backward / Left / Right roll
- Generic Evade fallback
- synced selector `SoY_EvadeType`
- local telemetry `SoY_Evading`

`SoY_Evading` is animation/OSC telemetry only. It does not grant gameplay invulnerability by itself.

## TB17 automated action authoring

Installed Spell, Technick, and Item actions are discovered automatically. TB17 generates the functional Contact/Raycast gates, active windows, reset clips, recovery timing, and a safe timer motion when no avatar presentation clip is assigned.

Presentation is optional and separate from functional delivery. Spells can use a per-spell override, school preset, purpose/category preset, or one shared default. Technicks and Items can use a per-action override or one shared default. Blank presentation fields are valid.

Managed action hosts also receive an `FX — <Kind> Visuals (Place Here)` child so optional VFX placed below the generated host follow the functional action Active window without hand-authored toggle clips. Empty TB16-generated Raycast Cast placeholders are treated as legacy placeholders; edited non-empty clips are preserved.

Functional gates remain the owner of:

- `SoY_SpellApproved`
- `SoY_TechnickApproved`
- `SoY_ItemApproved`
- `SoY_RaycastApproved`

The generated gates prevent another selector from directly replacing an active managed action. Desktop/Sam.py remain the eventual authority for persistent gameplay timing and damage acceptance.

## Resource FX

HP, MP, Mist, Curse of Diablos, and Arousal authoring use normalized Float parameters and Simple1D Blend Trees. The default Curse warning point is 80% rather than the older 90% point.

## Outside Contact compatibility retained

TB17 retains the exact, case-sensitive compatibility aliases introduced in TB12:

| Sender tag | Stories interpretation |
|---|---|
| `Sword` | Average physical hit → `SoY_HitAverage` |
| `Weapon` | Average physical hit → `SoY_HitAverage` |
| `Hands` | Weak physical hit → `SoY_HitWeak` |
| `Blockable` | block/parry compatible |
| `Hit Blocked` | block/parry compatible |
| `Parry_Detect` | block/parry compatible |

These aliases remain compatibility inputs. They do not bypass the existing Desktop/Sam.py authority path.

> `Hands` is broad/default VRChat contact vocabulary. If an incoming body receiver is active, ordinary hand contact can register as a Weak hit.

## Current Raycast feature set

- Direct Impact raycasts for bullets, arrows, projectiles, items and debuffs.
- Spell and Technick World / Ground Placement.
- Local Targeting Crosshair toggle.
- Native `VRCParentConstraint` World Drop using `FreezeToWorld`.
- Recovery state before returning to Ready.
- Flat, globally unique generated Raycast animation names under `<Avatar>/Animations/Raycasts/`.
- Menus are install-aware: only actions with matching managed Contacts are emitted.

See `RAYCAST_GUIDE_v0.5.10-TB17.1.md`.

## Repository layout

```text
Assets/Stories Of Yggdrasil/Editor/   Unity-installable editor source
contracts/                            Current Unity/contact contract
registries/                           Current Spell/Technick/Item registries
.github/workflows/                    Release workflow
tools/                                Repository verification and release builder
```

## Safety

- Existing third-party health Animator systems are not replaced.
- Managed repairs are restricted to Stories-owned signatures.
- Foreign Contact components are not modified.
- Original FX controllers are not edited; managed changes use the safe FX copy workflow.
- Unity Tool/protocol markers are local and unsynced.
- Schema validity is only published after managed validation succeeds.

### Protocol 20 damage-source separation

TB17 now isolates legacy `Sword` / `Weapon` / `Hands` compatibility on `SoY_ExternalDamageSource`. `SoY_DamageSourceEnemy` is reserved for the canonical `SoY Caster Enemy` tag. Use **Safe Repair All** or **Repair Existing Action Alignment** to migrate an older mixed receiver automatically.
