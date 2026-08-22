# Current Test Build: v0.5.10 TB8

TB5 adds native World-Dropped Spell Ground Placement using VRChat `VRCParentConstraint.FreezeToWorld`. See `RAYCAST_GUIDE_v0.5.10-TB8.md`.

# Stories Of Yggdrasil OSC Unity Tool

Current prerelease: **v0.5.10 TB8**

This repository contains the Unity Editor tool used to author and repair the
Stories Of Yggdrasil OSC Contact System for VRChat avatars.

## Install

### Built-in updater asset

Download both files from the GitHub Release:

- `StoriesOfYggdrasilOSCContactSystem.cs`
- `StoriesOfYggdrasilOSCContactSystem.cs.sha256`

The tool's updater verifies the checksum before replacing the current script.

### Manual Unity install

Import:

- `Stories-OSC-Unity-Tool-v0.5.10-TB8.unitypackage`

Or copy the canonical script to:

```text
Assets/Stories Of Yggdrasil/Editor/StoriesOfYggdrasilOSCContactSystem.cs
```

Only one copy of `StoriesOfYggdrasilOSCContactSystem` may exist inside the
Unity project's `Assets` folder.

## v0.5.10 TB3 highlights


### Installed-only menu generation

Generated action menus now reflect the avatar that is actually being authored.
Spell, Technick, and Item buttons are discovered from Stories-managed Contact
objects already installed in the avatar hierarchy. Catalog actions that do not
have matching Contacts are omitted, and empty action categories are not linked
into the live `Stories RP` menu.

Quick Access follows the same rule: an old favorite can remain in the local
profile, but it is not emitted into the VRChat menu until that action exists on
the avatar again.

### Raycast targeting and firing

Raycast installs now add a local unsynchronized `SoY_RaycastTargeting` toggle.
The generated **Targeting** menu appears only when a Raycast is installed and
contains **Targeting Crosshair**. Turn it on to see the local-only crosshair.

Selector Raycasts do not need a second Fire button:

- Spell button -> `SoY_SpellType` -> Raycast + Spell cast animation
- Technick button -> `SoY_TechnickType` -> Raycast + Technick animation
- Item button -> `SoY_ItemType` -> Raycast + Item animation
- Attack/Debuff Direct Impact -> generated `Projectile Fire` button when needed

Creating/repairing a selector Raycast automatically creates or preserves its
managed animation binding. Generated Raycast animation assets mirror the actual
avatar hierarchy under `<Avatar>/Animations/Raycasts/...`, making it clear which
clip belongs to which generated object. Edit the generated `Cast_*.anim` clip
when an avatar-specific casting motion is desired.

### TB2 selection-mapping hotfix

Search-filtered Spell, Technick, and Item selections now use one canonical
resolver for the Editor popup, Raycast gate/prefix, and generated Contact bus.
This fixes the live-reported case where a Regen Raycast was named/triggered as
Spell ID 8 but generated Cure ID 1 Contact children.

### Raycast Studio

Raycast authoring is now separated into two purpose-built modes:

- **Direct Impact** — bullets, arrows, beams, projectile-like Technicks/Items,
  and debuffs.
- **Spell Ground Placement** — aim at a remote player, display a local-only
  targeting icon, then place the spell Contact/VFX anchor on world geometry
  beneath the target with a dedicated downward floor probe.

Spell Ground Placement reuses one player-target ray and one downward ground ray
instead of generating two `VRCRaycast` components for every spell.

Remote-player targeting prefers VRChat's `Player` layer only and excludes
`PlayerLocal`. If a future/alternate SDK build does not expose the custom layer
mask member, the tool falls back to `Hit Players` and reports a warning rather
than creating a non-functional custom-layer raycast.

See `RAYCAST_GUIDE_v0.5.10-TB3.md` for the complete authoring workflow.

### Generated avatar workspace

New managed assets are grouped by avatar/model name:

```text
Assets/Stories Of Yggdrasil/<Avatar>/
├─ FX/
├─ Menus/
├─ Animations/
├─ Profiles/
└─ Backups/
```

Legacy generated paths are still recognized and are **not** moved automatically,
avoiding broken Unity asset references during an upgrade.

### Menu cleanup

The generated `Stories RP` menu is organized as:

- Combat
- Spells
- Actions
- Targeting
- Status
- Quick Access (when configured)

The in-tool Help page now explains the intended use of Contacts, Direct Impact
Raycasting, Spell Ground Placement, the local targeting icon, safe FX copies,
and generated asset locations.

## Safety guarantees

- Existing third-party health Animator systems are not replaced.
- Managed repairs are restricted to strict Stories-owned signatures.
- Repair transactions preserve hierarchy, transforms, constraints, prefab
  changes, and unrelated avatar systems.
- Critical attacks do not carry the `Blockable` tag.
- Weak, Average, and Strong attacks carry `Blockable`.
- Incoming hit I-Frames are one second.
- Original FX controllers are never edited by managed changes.
- Existing legacy generated assets are not automatically relocated.

See `RELEASE_NOTES_v0.5.10-TB3.md`, the Raycast guide, and the generated
contract/registry files for the complete interface snapshot.


## TB7 Constraint Sources hotfix

TB7 replaces the TB6 reflected/keyable-list source assignment with VRChat's documented public C# API:
`VRCParentConstraint.Sources.Add(new VRCConstraintSource(transform, weight))`. This targets the failure seen on Avatars SDK 3.10.5-beta.1 where the component resolved correctly but TB6 could not populate the public Sources list through reflection.
