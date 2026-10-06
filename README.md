# Stories OSC Unity Tool v0.5.10 TB16

Current prerelease: **Stories OSC Unity Tool v0.5.10 TB16 — Evasion & Protocol Compatibility**.

This repository contains the Unity Editor authoring/repair tool for the Stories Of Yggdrasil OSC Contact System used by VRChat avatars.

## Install

Download the release `Stories-OSC-Unity-Tool-v0.5.10-TB16.unitypackage`, or copy the canonical script to:

```text
Assets/Stories Of Yggdrasil/Editor/StoriesOfYggdrasilOSCContactSystem.cs
```

Only one `StoriesOfYggdrasilOSCContactSystem` script should exist inside a Unity project's `Assets` folder.

## TB16 — protocol 19 migration and authoring marker

TB16 is the first Unity Tool build that publishes a fail-closed Stories avatar compatibility marker for the Desktop OSC bridge.

After **MIGRATE / VALIDATE AVATAR FOR PROTOCOL 19** succeeds, the avatar publishes these local/unsynced parameters:

- `SoY_UnityToolPresent`
- `SoY_UnityToolMajor`
- `SoY_UnityToolMinor`
- `SoY_UnityToolPatch`
- `SoY_UnityToolTB`
- `SoY_UnityToolTBRevision`
- `SoY_ProtocolVersion`
- `SoY_UnitySchemaValid`

For this build the expected authoring marker is **v0.5.10 TB16 / Protocol 19**. `SoY_UnitySchemaValid` is only published as true after the current managed schema validates.

The matching Desktop line is **v0.8.21+**. Older, unmarked, invalid, or future protocol Stories-generated gameplay input is handled fail-closed by the Desktop runtime. Sam.py is not changed by TB16.

## Evasion animation authoring

TB16 adds a dedicated Evasion animation builder with:

- Forward / Backward / Left / Right evade
- Forward / Backward / Left / Right roll
- Generic Evade fallback
- synced selector `SoY_EvadeType`
- local telemetry `SoY_Evading`

`SoY_Evading` is animation/OSC telemetry only. It does not grant gameplay invulnerability by itself.

## Action gates retained from TB15

Spell, Technick, Item, and Raycast authoring retain the TB15 gate/recovery architecture and local approved-action outputs:

- `SoY_SpellApproved`
- `SoY_TechnickApproved`
- `SoY_ItemApproved`
- `SoY_RaycastApproved`

The generated gates prevent another selector from directly replacing an active managed action. Desktop/Sam.py remain the eventual authority for gameplay timing and damage acceptance.

## Resource FX

HP, MP, Mist, Curse of Diablos, and Arousal authoring use normalized Float parameters and Simple1D Blend Trees. The default Curse warning point is 80% rather than the older 90% point.

## Outside Contact compatibility retained

TB16 retains the exact, case-sensitive compatibility aliases introduced in TB12:

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

See `RAYCAST_GUIDE_v0.5.10-TB16.md`.

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
