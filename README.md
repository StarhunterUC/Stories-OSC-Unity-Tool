# Stories Of Yggdrasil OSC Contact System — Unity Tool v0.5.2

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

## v0.5.2 additions

- Expands the stable registry from White/Black Magick to **144 spell IDs across 12 schools**.
- Preserves every v0.5.1 spell ID.
- Adds Green, Time, Arcane, Synergist, Illusion, Dream, Nature, Chaos, Abyssal, and Yggdrasil Light spell menus.
- Groups the generated spell menu into **Core Magick**, **Specialized Magick**, and **Forbidden & Custom** so each VRChat menu stays within its control limit.
- Adds spell categories: Offensive, Healing, Revival, Cleanse, Support, Status, and Utility.
- Adds category Contact tags for future Sam.py routing.
- Lets creators select exactly which incoming spell schools are installed.
- Deduplicates shared spell IDs when several selected schools contain the same spell.
- Warns when an unusually large incoming receiver set is selected.

## Spell setup

Open **Outgoing Contacts → Spells**, choose a school and spell, preview the volume, position it, then finalize it. The tool creates Ally and Enemy alignment variants and rebuilds the generated Spell Alignment layer.

Incoming spell receivers are selected by school under **Incoming Contacts**. White and Black remain enabled by default for compatibility; additional schools can be enabled individually.

Registry and contract:

```text
SPELL_ID_REGISTRY_v2.json
OSC_CONTRACT_v8.json
```

## Generated submenu layout

```text
Stories RP
├─ RP Combat
├─ Enemy Mode
├─ Spells
│  ├─ Core Magick
│  │  ├─ White
│  │  ├─ Black
│  │  ├─ Green
│  │  ├─ Time
│  │  └─ Arcane
│  ├─ Specialized Magick
│  │  ├─ Synergist
│  │  ├─ Illusion
│  │  ├─ Dream
│  │  └─ Nature
│  └─ Forbidden & Custom
│     ├─ Chaos
│     ├─ Abyssal Curses
│     └─ Yggdrasil Light
└─ Status Gauges
```

## Enemy healing rule

The Unity side provides:

- `SoY_IsEnemy` on the target.
- `SoY_HealingSourceEnemy` from the caster-alignment Contact tag.
- `SoY_SpellType` to identify the spell.
- `SoY_HealingRejected` for bridge feedback.

The desktop/Sam.py bridge must apply the final rule from `OSC_CONTRACT_v8.json`. Unity Contacts cannot directly edit Sam.py HP.

## Updater

The updater checks the latest published release from:

```text
StarhunterUC/Stories-OSC-Unity-Tool
```

It never updates silently. It asks first, downloads the canonical `.cs` release asset or Unity-tool ZIP, creates a backup under:

```text
Assets/Stories Of Yggdrasil/Backups/Unity Tool/
```

and then refreshes Unity.
