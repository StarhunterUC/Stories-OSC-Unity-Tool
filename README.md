# Stories Of Yggdrasil OSC Contact System — Unity Tool v0.5.1

## Install

Place `StoriesOfYggdrasilOSCContactSystem.cs` in an Editor folder, preferably:

```text
Assets/Stories Of Yggdrasil/Editor/StoriesOfYggdrasilOSCContactSystem.cs
```

Open:

```text
Tools → Stories Of Yggdrasil → OSC Contact System
```

Assign the avatar descriptor, press **Load From Avatar**, then press **PREPARE AVATAR FOR STORIES OSC**.

The tool creates and assigns a copy of the avatar FX controller under:

```text
Assets/Stories Of Yggdrasil/FX/
```

The original FX controller is not edited.

## v0.5.1 additions

- Replaces the direct combat toggle with a generated **Stories RP** submenu.
- Adds **RP Combat** and **Enemy Mode** toggles.
- Adds Status Gauges for Mist Charge and Curse Of Diablos.
- Adds Curse warning Animator states at 25%, 50%, 90%, and 98%.
- Adds one-second incoming-hit invincibility frames by disabling only the generated damage-receiver child.
- Adds White Magick and Black Magick spell registries through one `SoY_SpellType` Int.
- Generates Ally and Enemy spell-sender variants selected by `SoY_IsEnemy`.
- Adds incoming spell receivers that write exact spell IDs into `SoY_SpellType`.
- Adds a permission-based GitHub Release updater.

## Spell setup

Open **Outgoing Contacts → Spells**, choose a school and spell, preview the volume, position it, then finalize it. The tool creates both alignment variants and rebuilds the generated Spell Alignment layer.

Incoming spell receivers can be created under **Incoming Contacts**. White and Black Magick receivers are optional independently.

## Enemy healing rule

The Unity side provides:

- `SoY_IsEnemy` on the target.
- `SoY_HealingSourceEnemy` from the caster-alignment contact tag.
- `SoY_SpellType` to identify the spell.
- `SoY_HealingRejected` for bridge feedback.

The desktop/Sam.py bridge must apply the final rule from `OSC_CONTRACT_v7.json`. Unity Contacts cannot directly edit Sam.py HP.

## Updater

The updater checks the latest published release from:

```text
StarhunterUC/Stories-OSC-Unity-Tool
```

It never updates silently. It asks first, downloads either the canonical `.cs` release asset or the Unity-tool ZIP, creates a backup under:

```text
Assets/Stories Of Yggdrasil/Backups/Unity Tool/
```

and then refreshes Unity.

The repository must have a published GitHub Release. A normal commit without a Release will return “No published GitHub Release exists yet.”
