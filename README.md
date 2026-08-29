# Stories OSC Unity Tool v0.5.10 TB12

Current prerelease: **Stories OSC Unity Tool v0.5.10 TB12 — External Contact Compatibility**.

This repository contains the Unity Editor authoring/repair tool for the Stories Of Yggdrasil OSC Contact System used by VRChat avatars.

## Install

Download the release `Stories-OSC-Unity-Tool-v0.5.10-TB12.unitypackage`, or copy the canonical script to:

```text
Assets/Stories Of Yggdrasil/Editor/StoriesOfYggdrasilOSCContactSystem.cs
```

Only one `StoriesOfYggdrasilOSCContactSystem` script should exist inside a Unity project's `Assets` folder.

## TB12 — outside Contact compatibility

TB12 accepts additional exact, case-sensitive Contact Sender tags from other avatar systems and normalizes them into the existing Stories OSC parameters:

| Sender tag | Stories interpretation |
|---|---|
| `Sword` | Average physical hit → `SoY_HitAverage` |
| `Weapon` | Average physical hit → `SoY_HitAverage` |
| `Hands` | Weak physical hit → `SoY_HitWeak` |
| `Blockable` | block/parry compatible |
| `Hit Blocked` | block/parry compatible |
| `Parry_Detect` | block/parry compatible |

External `Sword`, `Weapon`, and `Hands` contacts also mark the source as hostile/unknown through `SoY_DamageSourceEnemy`, because outside systems do not carry the Stories caster-alignment tag. They still use the normal Desktop → Sam.py authority path; the Sam.py DM Gate is not bypassed.

> `Hands` is broad/default VRChat contact vocabulary. If an incoming body receiver is active, ordinary hand contact can register as a Weak hit. Gate incoming combat receivers when passive touching should not count as combat.

## Current Raycast feature set

- Direct Impact raycasts for bullets, arrows, projectiles, items and debuffs.
- Spell World / Ground Placement with remote-player targeting and a downward world-floor probe.
- Technick World / Ground Placement using the same targeting/world-drop system.
- Local-only Targeting Crosshair toggle.
- Native `VRCParentConstraint` World Drop using `FreezeToWorld`.
- World Drop release returns to the live raycast at zero offset (`RebakeOffsetsWhenUnfrozen = false`).
- Flat, globally unique generated Raycast animation names under `<Avatar>/Animations/Raycasts/`.
- Menus are install-aware: only actions with matching managed Contacts are emitted.

See `RAYCAST_GUIDE_v0.5.10-TB12.md` for the current workflow.

## Repository layout

```text
Assets/Stories Of Yggdrasil/Editor/   Unity-installable editor source
contracts/                            Current Unity/contact contract only
registries/                           Current Spell/Technick/Item registries
.github/workflows/                    Release workflow
tools/                                Repository verification and release builder
```

Old TB release notes, old Raycast guides, old source audits, obsolete contract snapshots, committed `dist/`, caches and duplicate root registry copies are intentionally not kept in the current working tree. Git history/releases preserve those versions.

## Safety

- Existing third-party health Animator systems are not replaced.
- Managed repairs are restricted to Stories-owned signatures.
- Foreign Contact components are not modified.
- Weak/Average/Strong Stories attacks remain Blockable; Critical remains non-blockable.
- Incoming hit I-Frames remain one second.
- Original FX controllers are not edited; managed changes use the safe FX copy workflow.
